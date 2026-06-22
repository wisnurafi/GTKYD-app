using System.Runtime.Versioning;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.App.ViewModels;
using GetToKnowYourDevice.Export;
using GetToKnowYourDevice.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetToKnowYourDevice.App;

/// <summary>
/// Builds and holds the application's dependency injection container. Composes Core, Infrastructure,
/// and Export registrations and adds App-layer services and view models.
/// </summary>
[SupportedOSPlatform("windows10.0.17763.0")]
public static class AppHost
{
    private static IServiceProvider? _services;

    public static IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("AppHost not initialized.");

    public static void Initialize()
    {
        var appData = AppPaths.GetAppDataDirectory();
        var services = new ServiceCollection();

        services.AddDeviceInspectionInfrastructure(appData);
        services.AddReportExport();

        // App-layer services
        services.AddSingleton<AppScanService>();

        // View models (transient pages get fresh VMs; shell is shared)
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<SummaryViewModel>();
        services.AddTransient<HardwareViewModel>();
        services.AddTransient<StorageViewModel>();
        services.AddTransient<BatteryViewModel>();
        services.AddTransient<DriversViewModel>();
        services.AddTransient<SecurityViewModel>();
        services.AddTransient<NetworkViewModel>();
        services.AddTransient<RawReportViewModel>();
        services.AddTransient<ScanHistoryViewModel>();
        services.AddTransient<CompareViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();

        _services = services.BuildServiceProvider();
    }

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();
}
