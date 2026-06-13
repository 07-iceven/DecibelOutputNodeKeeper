using System.Timers;
using System.Diagnostics;
using System.Threading;
using NAudio.CoreAudioApi;
using DecibelOutputNodeKeeper.Models;

namespace DecibelOutputNodeKeeper.Services;

public class AudioLockService : IDisposable
{
    private const double TickDurationSeconds = 0.05;
    private readonly System.Timers.Timer _timer;
    private AppSettings? _settings;
    private readonly MMDeviceEnumerator _deviceEnumerator;

    private Thread? _highFrequencyThread;
    private CancellationTokenSource? _highFrequencyCts;

    public AudioLockService()
    {
        _deviceEnumerator = new MMDeviceEnumerator();
        _timer = new System.Timers.Timer();
        _timer.Elapsed += OnTimerElapsed;
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        
        if (settings.Microphone.LockEnabled)
        {
            if (settings.Microphone.CheckIntervalTicks <= 0)
            {
                _timer.Stop();
                StartHighFrequencyLoop();
            }
            else
            {
                StopHighFrequencyLoop();
                _timer.Interval = NormalizeCheckIntervalTicks(settings.Microphone.CheckIntervalTicks) * TickDurationSeconds * 1000;
                if (!_timer.Enabled)
                {
                    _timer.Start();
                }
            }
            // Initial check
            LockVolume();
        }
        else
        {
            _timer.Stop();
            StopHighFrequencyLoop();
        }
    }

    private void StartHighFrequencyLoop()
    {
        if (_highFrequencyThread != null && _highFrequencyThread.IsAlive) return;

        _highFrequencyCts = new CancellationTokenSource();
        var token = _highFrequencyCts.Token;

        _highFrequencyThread = new Thread(() =>
        {
            // 绑定线程到特定的 CPU 核心 (例如核心 0) 以确保吃满单核
            try
            {
                Thread.BeginThreadAffinity();
                // 获取当前进程
                using var process = Process.GetCurrentProcess();
                // 遍历当前进程的线程，找到当前执行的线程并设置其处理器亲和性
                foreach (ProcessThread pt in process.Threads)
                {
                    if (pt.Id == AppDomain.GetCurrentThreadId())
                    {
                        pt.ProcessorAffinity = (IntPtr)1; // 1 表示第 0 个核心 (二进制 0001)
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioLock] Failed to set thread affinity: {ex.Message}");
            }

            while (!token.IsCancellationRequested)
            {
                if (_settings != null && _settings.Microphone.TimeRestrictionEnabled && 
                    !IsCurrentTimeInRestriction(_settings.Microphone.StartTime, _settings.Microphone.EndTime))
                {
                    // 如果启用了时间限制且当前不在限制时间内，休眠 1 秒以降低 CPU 占用
                    Thread.Sleep(1000);
                    continue;
                }

                LockVolume();
            }

            try
            {
                Thread.EndThreadAffinity();
            }
            catch { }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest,
            Name = "AudioLockHighFrequencyThread"
        };
        _highFrequencyThread.Start();
    }

    private void StopHighFrequencyLoop()
    {
        if (_highFrequencyCts != null)
        {
            _highFrequencyCts.Cancel();
            _highFrequencyCts.Dispose();
            _highFrequencyCts = null;
        }
        if (_highFrequencyThread != null)
        {
            if (_highFrequencyThread.IsAlive)
            {
                _highFrequencyThread.Join(100);
            }
            _highFrequencyThread = null;
        }
    }

    public static int NormalizeCheckIntervalTicks(int ticks)
    {
        return ticks;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        LockVolume();
    }

    private void LockVolume()
    {
        if (_settings == null || !_settings.Microphone.LockEnabled) return;

        // Time restriction check is now handled in the loop for high frequency mode
        // For normal timer mode, we still need it here
        if (_settings.Microphone.CheckIntervalTicks > 0 && _settings.Microphone.TimeRestrictionEnabled)
        {
            if (!IsCurrentTimeInRestriction(_settings.Microphone.StartTime, _settings.Microphone.EndTime))
            {
                return;
            }
        }

        try
        {
            // Get default recording device
            using var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            if (device != null)
            {
                // Obfuscation strategy: The system's actual master volume scalar is set to (1.0 - display_volume).
                // This makes the volume slider in Windows appear inverted compared to our UI's target.
                float displayVolume = _settings.Microphone.VolumePercent / 100f;
                float actualTargetVolume = 1.0f - displayVolume;

                // Remove the deviation check to force the COM call every single time
                // This ensures maximum CPU utilization and absolute lock
                device.AudioEndpointVolume.MasterVolumeLevelScalar = actualTargetVolume;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioLock] Error accessing audio device: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if current system time falls within the restricted range.
    /// Handles cross-midnight ranges (e.g., 22:00 to 06:00).
    /// </summary>
    private bool IsCurrentTimeInRestriction(string startStr, string endStr)
    {
        if (!TimeSpan.TryParse(startStr, out var start) || !TimeSpan.TryParse(endStr, out var end))
        {
            Debug.WriteLine("[AudioLock] Invalid time format in settings.");
            return true; // Default to active if config is broken
        }

        var now = DateTime.Now.TimeOfDay;
        
        if (start <= end)
        {
            // Normal range: e.g., 09:00 - 17:00
            return now >= start && now <= end;
        }
        else
        {
            // Cross-midnight range: e.g., 22:00 - 06:00
            return now >= start || now <= end;
        }
    }

    public void Dispose()
    {
        StopHighFrequencyLoop();
        _timer.Stop();
        _timer.Dispose();
        _deviceEnumerator.Dispose();
    }
}
