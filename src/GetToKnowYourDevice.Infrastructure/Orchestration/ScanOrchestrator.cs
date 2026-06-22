using System.Reflection;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Health;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Orchestration;

public interface IScanOrchestrator
{
    Task<CanonicalReport> RunScanAsync(ScanContext context,
        IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}

/// <summary>
/// Runs the scanners selected by scan mode/categories with bounded parallelism, per-scanner
/// timeout, and cancellation. One scanner failing never fails the whole report: each failure
/// is captured in ScanDiagnostics and the overall status becomes PartialSuccess. Finally
/// computes the health score from the assembled report.
/// </summary>
public sealed class ScanOrchestrator(
    IEnumerable<IOrchestratableScanner> scanners,
    HealthScoreEngine healthEngine,
    ILogger<ScanOrchestrator> logger) : IScanOrchestrator
{
    public async Task<CanonicalReport> RunScanAsync(ScanContext context,
        IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var report = new CanonicalReport();
        report.ReportMetadata.ReportId = Guid.NewGuid();
        report.ReportMetadata.ScanMode = context.Mode;
        report.ReportMetadata.StartedAt = DateTimeOffset.Now;
        report.ReportMetadata.ApplicationVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

        var selected = SelectScanners(context).ToList();
        logger.LogInformation("Scan started: mode={Mode}, scanners={Count}", context.Mode, selected.Count);

        var completed = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var semaphore = new SemaphoreSlim(Math.Max(1, context.MaxParallelScanners));

        // SystemScanner must run first so DeviceId/BIOS data is available to others; run it
        // alone, then the rest in parallel. Keeps dependencies correct without complex graphs.
        var ordered = selected.OrderBy(s => s.Name == "System" ? 0 : s.Name == "MotherboardBios" ? 1 : 2).ToList();

        var results = new List<ScannerRunResult>();
        var resultsLock = new object();

        async Task RunOne(IOrchestratableScanner scanner)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await scanner.RunAsync(report, context, progress, cancellationToken)
                    .ConfigureAwait(false);
                lock (resultsLock) results.Add(result);
            }
            finally
            {
                semaphore.Release();
                var done = Interlocked.Increment(ref completed);
                progress?.Report(new ScanProgress
                {
                    ScannerName = scanner.Name,
                    Status = "Completed",
                    CompletedScanners = done,
                    TotalScanners = ordered.Count,
                    Elapsed = sw.Elapsed
                });
            }
        }

        // Run priority scanners (System, BIOS) sequentially first.
        foreach (var s in ordered.Where(s => s.Name is "System" or "MotherboardBios"))
            await RunOne(s).ConfigureAwait(false);

        // Run the rest with bounded parallelism.
        var rest = ordered.Where(s => s.Name is not ("System" or "MotherboardBios"));
        await Task.WhenAll(rest.Select(RunOne)).ConfigureAwait(false);

        report.ReportMetadata.CompletedAt = DateTimeOffset.Now;
        BuildDiagnostics(report, results);
        report.Health = healthEngine.Evaluate(report);

        logger.LogInformation("Scan completed: status={Status}, warnings={W}, errors={E}, {Ms}ms",
            report.ReportMetadata.OverallStatus, report.ReportMetadata.WarningCount,
            report.ReportMetadata.ErrorCount, sw.ElapsedMilliseconds);

        return report;
    }

    private IEnumerable<IOrchestratableScanner> SelectScanners(ScanContext ctx)
    {
        foreach (var s in scanners)
        {
            var include = ctx.Mode switch
            {
                ScanMode.Quick => IsQuickScanner(s),
                ScanMode.Full => true,
                ScanMode.Custom => ctx.Includes(s.Category),
                _ => false
            };

            // Honor explicit toggles even in Full scan.
            if (include && s.Category == ScanCategory.Drivers && !ctx.IncludeDriverScan && ctx.Mode != ScanMode.Custom)
                include = false;
            if (include && s.Category == ScanCategory.Security && !ctx.IncludeSecurityScan && ctx.Mode != ScanMode.Custom)
                include = false;
            if (include && s.Category == ScanCategory.Peripherals && !ctx.IncludePeripheralScan && ctx.Mode != ScanMode.Custom)
                include = false;

            if (include) yield return s;
        }
    }

    /// <summary>Quick scan = lightweight scanners only (no driver/peripheral enumeration).</summary>
    private static bool IsQuickScanner(IOrchestratableScanner s) => s.Name is
        "System" or "OperatingSystem" or "MotherboardBios" or "Processor" or "Memory"
        or "GraphicsDisplayAudio" or "Storage" or "Battery" or "Network" or "Security";

    private static void BuildDiagnostics(CanonicalReport report, List<ScannerRunResult> results)
    {
        var warnings = 0;
        var errors = 0;
        var anyFailure = false;
        var anySuccess = false;

        foreach (var r in results.OrderBy(r => r.StartedAt))
        {
            report.ScanDiagnostics.Add(new ScanDiagnostic
            {
                ScannerName = r.ScannerName,
                Status = r.Status,
                Source = r.Source,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                RequiresElevation = r.RequiresElevation,
                IsPartial = r.IsPartial,
                Warnings = r.Warnings,
                Errors = r.Errors,
                UserMessage = r.UserMessage,
                TechnicalMessage = r.TechnicalMessage,
                PropertiesUnavailable = r.PropertiesUnavailable
            });

            warnings += r.Warnings.Count;
            errors += r.Errors.Count;
            if (r.Status is ScanStatus.Failed or ScanStatus.TimedOut or ScanStatus.PartialSuccess
                or ScanStatus.PermissionRequired) anyFailure = true;
            if (r.Status is ScanStatus.Success or ScanStatus.PartialSuccess) anySuccess = true;
        }

        report.ReportMetadata.WarningCount = warnings;
        report.ReportMetadata.ErrorCount = errors;
        report.ReportMetadata.OverallStatus =
            !anySuccess ? ScanStatus.Failed :
            anyFailure ? ScanStatus.PartialSuccess :
            ScanStatus.Success;
    }
}
