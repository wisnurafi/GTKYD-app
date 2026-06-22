using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Health;
using GetToKnowYourDevice.Core.Persistence;
using GetToKnowYourDevice.Core.Privacy;
using GetToKnowYourDevice.Core.Settings;
using GetToKnowYourDevice.Infrastructure.Logging;
using GetToKnowYourDevice.Infrastructure.Orchestration;
using GetToKnowYourDevice.Infrastructure.Persistence;
using GetToKnowYourDevice.Infrastructure.PowerShellSupport;
using GetToKnowYourDevice.Infrastructure.Scanners;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Settings;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers all infrastructure: scanners, orchestrator, SQLite history, settings, masking,
    /// health engine, and the file logger. <paramref name="appDataDirectory"/> is where the
    /// database, settings, and logs are stored (local-first, no external services).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddDeviceInspectionInfrastructure(
        this IServiceCollection services, string appDataDirectory)
    {
        var dbPath = Path.Combine(appDataDirectory, "history", "scans.db");
        var settingsDir = Path.Combine(appDataDirectory, "settings");
        var logDir = Path.Combine(appDataDirectory, "logs");

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new FileLoggerProvider(logDir));
        });

        // Shared helpers
        services.AddSingleton<WmiQueryRunner>();
        services.AddSingleton<PowerShellRunner>();

        // Core engines
        services.AddSingleton<HealthThresholds>();
        services.AddSingleton<HealthScoreEngine>();
        services.AddSingleton<IPrivacyMasker, PrivacyMasker>();

        // Settings (single instance shared across the app)
        services.AddSingleton<ISettingsService>(sp =>
            new JsonSettingsService(settingsDir, sp.GetRequiredService<ILogger<JsonSettingsService>>()));

        // Persistence
        services.AddSingleton<IScanHistoryRepository>(sp =>
            new SqliteScanHistoryRepository(dbPath, sp.GetRequiredService<ILogger<SqliteScanHistoryRepository>>()));

        // Scanners (all registered under the orchestratable interface)
        services.AddSingleton<IOrchestratableScanner, SystemScanner>();
        services.AddSingleton<IOrchestratableScanner, OperatingSystemScanner>();
        services.AddSingleton<IOrchestratableScanner, MotherboardBiosScanner>();
        services.AddSingleton<IOrchestratableScanner, ProcessorScanner>();
        services.AddSingleton<IOrchestratableScanner, MemoryScanner>();
        services.AddSingleton<IOrchestratableScanner, GraphicsDisplayAudioScanner>();
        services.AddSingleton<IOrchestratableScanner, StorageScanner>();
        services.AddSingleton<IOrchestratableScanner, BatteryScanner>();
        services.AddSingleton<IOrchestratableScanner, DriverScanner>();
        services.AddSingleton<IOrchestratableScanner, SecurityScanner>();
        services.AddSingleton<IOrchestratableScanner, NetworkScanner>();
        services.AddSingleton<IOrchestratableScanner, PeripheralScanner>();

        services.AddSingleton<IScanOrchestrator, ScanOrchestrator>();

        return services;
    }
}
