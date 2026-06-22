using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Persistence;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Core.Serialization;
using GetToKnowYourDevice.Core.Settings;
using GetToKnowYourDevice.Infrastructure.Orchestration;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.App.Services;

/// <summary>
/// App-facing scan service: builds a ScanContext from settings, runs the orchestrator, and
/// (optionally) persists the result to history. Holds the most recent report so pages can bind
/// to it. UI never calls the orchestrator directly.
/// </summary>
public sealed class AppScanService(
    IScanOrchestrator orchestrator,
    IScanHistoryRepository history,
    ISettingsService settings,
    ILogger<AppScanService> logger)
{
    private CanonicalReport? _current;

    public CanonicalReport? CurrentReport
    {
        get => _current;
        private set { _current = value; CurrentReportChanged?.Invoke(this, value); }
    }

    public event EventHandler<CanonicalReport?>? CurrentReportChanged;

    public void SetCurrentReport(CanonicalReport report) => CurrentReport = report;

    public async Task<CanonicalReport> RunScanAsync(ScanMode mode, ScanCategory categories,
        IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        var s = settings.Current;
        var context = new ScanContext
        {
            Mode = mode,
            Categories = categories,
            IncludeDriverScan = s.IncludeDriverScan,
            IncludeSecurityScan = s.IncludeSecurityScan,
            IncludeSmartData = s.IncludeSmartData,
            IncludePeripheralScan = s.IncludePeripheralScan,
            IncludeExternalNetworkDiagnostics = s.IncludeExternalNetworkDiagnostics,
            AllowElevation = s.RequestElevationWhenRequired,
            SavePartialResult = s.SavePartialResult,
            PerScannerTimeout = TimeSpan.FromSeconds(s.PerScannerTimeoutSeconds),
            OverallTimeout = TimeSpan.FromSeconds(s.ScanTimeoutSeconds),
            MaxParallelScanners = s.MaxParallelScanners
        };

        logger.LogInformation("Running {Mode} scan", mode);
        var report = await orchestrator.RunScanAsync(context, progress, ct).ConfigureAwait(false);
        CurrentReport = report;

        if (s.AutomaticallySaveCompletedScans)
            await SaveToHistoryAsync(report, ct).ConfigureAwait(false);

        return report;
    }

    public async Task SaveToHistoryAsync(CanonicalReport report, CancellationToken ct)
    {
        try
        {
            var record = new ScanHistoryRecord
            {
                ScanId = report.ReportMetadata.ReportId.ToString("N"),
                DeviceId = report.DeviceIdentity.Uuid ?? report.DeviceIdentity.SerialNumber,
                ScanType = report.ReportMetadata.ScanMode,
                ScanName = report.ReportMetadata.ScanName,
                ScanDate = report.ReportMetadata.StartedAt,
                StartedAt = report.ReportMetadata.StartedAt,
                CompletedAt = report.ReportMetadata.CompletedAt,
                DurationMs = report.ReportMetadata.Duration.TotalMilliseconds,
                ApplicationVersion = report.ReportMetadata.ApplicationVersion,
                WindowsVersion = report.ReportMetadata.WindowsVersion,
                HealthScore = report.Health.Score,
                ScanStatus = report.ReportMetadata.OverallStatus,
                WarningCount = report.ReportMetadata.WarningCount,
                ErrorCount = report.ReportMetadata.ErrorCount,
                ReportJson = System.Text.Json.JsonSerializer.Serialize(report, CanonicalJson.Compact),
                ReportSchemaVersion = report.ReportMetadata.SchemaVersion
            };
            await history.SaveAsync(record, ct).ConfigureAwait(false);

            if (settings.Current.AutomaticallyRemoveOldUnpinnedScans)
                await history.PruneAsync(settings.Current.MaximumHistoryRecords, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save scan to history");
        }
    }

    public static CanonicalReport? DeserializeReport(string json)
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<CanonicalReport>(json, CanonicalJson.Compact); }
        catch { return null; }
    }
}
