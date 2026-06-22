using System.Runtime.Versioning;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace GetToKnowYourDevice.App;

/// <summary>
/// Application entry point. Initializes dependency injection, loads settings, sets up global
/// exception handling (logged, never silently swallowed), and shows the main window.
/// </summary>
[SupportedOSPlatform("windows10.0.17763.0")]
public partial class App : Application
{
    private Window? _window;
    private ILogger<App>? _logger;

    /// <summary>The live main window. Used by services that need a window handle (file pickers).</summary>
    public static MainWindow? MainWindowInstance { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppHost.Initialize();
        _logger = AppHost.Get<ILogger<App>>();

        RegisterGlobalExceptionHandlers();
        _logger.LogInformation("Application startup");

        // Load persisted settings before showing UI.
        try { await AppHost.Get<ISettingsService>().LoadAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Settings load failed at startup"); }

        _window = new MainWindow();
        MainWindowInstance = (MainWindow)_window;
        _window.Closed += (_, _) => _logger?.LogInformation("Application shutdown");
        _window.Activate();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        UnhandledException += (_, e) =>
        {
            _logger?.LogError(e.Exception, "Unhandled UI exception: {Message}", e.Message);
            e.Handled = true; // keep the app alive; the error is logged with full context
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            _logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled domain exception");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger?.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }
}
