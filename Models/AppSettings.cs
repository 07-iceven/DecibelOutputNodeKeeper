namespace DecibelOutputNodeKeeper.Models;

public class AppSettings
{
    public MicrophoneSettings Microphone { get; set; } = new();
    public bool AutoStartEnabled { get; set; }
}

public class MicrophoneSettings
{
    public bool LockEnabled { get; set; }
    public int VolumePercent { get; set; } = 80;
    public int CheckIntervalTicks { get; set; } = 100; // Default to 100 ticks (5s)
    public bool TimeRestrictionEnabled { get; set; }
    public string StartTime { get; set; } = "00:00";
    public string EndTime { get; set; } = "23:59";
}
