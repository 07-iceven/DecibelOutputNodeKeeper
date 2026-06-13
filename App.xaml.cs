using System.Windows;
using DecibelOutputNodeKeeper.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace DecibelOutputNodeKeeper;

public partial class App : System.Windows.Application
{
    private AudioLockService? _audioLockService;
    private StartupService? _startupService;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handling
        DispatcherUnhandledException += (s, args) =>
        {
            WpfMessageBox.Show($"程序发生未处理的错误：{args.Exception.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);

        var loadResult = SettingsService.Load();
        if (loadResult.Status is SettingsLoadStatus.Corrupted or SettingsLoadStatus.Error)
        {
            WpfMessageBox.Show(
                $"无法加载配置，程序已停止启动。{Environment.NewLine}{loadResult.ErrorMessage}",
                "配置错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var settings = loadResult.Settings;

        _audioLockService = new AudioLockService();
        _startupService = new StartupService();

        ApplyManagedSettings(settings);

        var mainWindow = new MainWindow(
            settings,
            _audioLockService,
            _startupService);

        MainWindow = mainWindow;
        mainWindow.Show();

        if (e.Args.Any(arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase)))
        {
            mainWindow.HideToTrayOnStartup();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _audioLockService?.Dispose();
        base.OnExit(e);
    }

    private void ApplyManagedSettings(Models.AppSettings settings)
    {
        _audioLockService?.UpdateSettings(settings);
        var startupResult = _startupService?.Apply(settings);
        if (startupResult is { Succeeded: false })
        {
            WpfMessageBox.Show(
                $"已加载配置，但开机自启动设置未能同步到系统。{Environment.NewLine}{startupResult.ErrorMessage}",
                "启动提示",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
