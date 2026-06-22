using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Health;
using GetToKnowYourDevice.Core.Privacy;

namespace GetToKnowYourDevice.Core.Settings;

public enum AppTheme { System, Light, Dark }
public enum UiDensity { Comfortable, Compact }
public enum StorageUnit { GB, GiB }
public enum TemperatureUnit { Celsius, Fahrenheit }

/// <summary>All persisted application settings. Saved locally; survives restart.</summary>
public sealed class AppSettings
{
    // General
    public ScanMode DefaultScanType { get; set; } = ScanMode.Quick;
    public bool RunQuickScanOnStartup { get; set; }            // default off per spec
    public bool AutomaticallySaveCompletedScans { get; set; } = true;
    public int MaximumHistoryRecords { get; set; } = 50;
    public bool ConfirmBeforeDeleting { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public StorageUnit StorageUnit { get; set; } = StorageUnit.GB;
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;
    public string StartPage { get; set; } = "Summary";

    // Appearance
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string AccentColor { get; set; } = "#0078D4";
    public UiDensity Density { get; set; } = UiDensity.Comfortable;
    public bool EnableAnimations { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public bool SidebarCollapsedByDefault { get; set; }

    // Scan
    public int ScanTimeoutSeconds { get; set; } = 300;
    public int PerScannerTimeoutSeconds { get; set; } = 30;
    public int MaxParallelScanners { get; set; } = 4;
    public bool IncludeDriverScan { get; set; } = true;
    public bool IncludeSecurityScan { get; set; } = true;
    public bool IncludeSmartData { get; set; } = true;
    public bool IncludePeripheralScan { get; set; } = true;
    public bool IncludeExternalNetworkDiagnostics { get; set; }   // default off
    public bool RequestElevationWhenRequired { get; set; }
    public bool SavePartialResult { get; set; } = true;
    public bool ShowUnavailableProperties { get; set; } = true;
    public ScanCategory LastCustomCategories { get; set; } = ScanCategory.System | ScanCategory.Hardware;

    // History
    public bool AutomaticallyRemoveOldUnpinnedScans { get; set; } = true;

    // Export
    public string? DefaultExportDirectory { get; set; }
    public ExportFormat DefaultExportFormat { get; set; } = ExportFormat.Json;
    public bool IncludeSerialNumbersInExport { get; set; } = true;
    public bool MaskSerialNumbers { get; set; }
    public bool MaskUsername { get; set; }
    public bool MaskMacAddress { get; set; }
    public bool MaskBssid { get; set; }
    public bool IncludeRawReportInExport { get; set; } = true;
    public bool IncludeFullDriverListInExport { get; set; }
    public string CsvDelimiter { get; set; } = ",";
    public PdfPageSize PdfPageSize { get; set; } = PdfPageSize.A4;
    public PdfOrientation PdfOrientation { get; set; } = PdfOrientation.Portrait;

    // Privacy
    public bool LocalOnlyMode { get; set; } = true;
    public bool DisableAllExternalRequests { get; set; } = true;
    public bool AllowPublicIpLookup { get; set; }                 // default off
    public bool AllowConnectivityTest { get; set; }               // default off
    public bool MaskDeviceIdentifiers { get; set; }
    public bool ShowDataPreviewBeforeExport { get; set; } = true;

    // Diagnostics
    public string LoggingLevel { get; set; } = "Information";
    public bool ShowDetailedScannerErrors { get; set; }

    // Health
    public HealthThresholds HealthThresholds { get; set; } = new();

    /// <summary>Builds masking options from the individual mask flags.</summary>
    public MaskingOptions ToMaskingOptions()
    {
        if (MaskDeviceIdentifiers) return MaskingOptions.All();
        return new MaskingOptions
        {
            MaskUsername = MaskUsername,
            MaskSerialNumbers = MaskSerialNumbers,
            MaskMacAddress = MaskMacAddress,
            MaskBssid = MaskBssid
        };
    }
}

/// <summary>Loads and saves <see cref="AppSettings"/> to local storage.</summary>
public interface ISettingsService
{
    AppSettings Current { get; }
    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
