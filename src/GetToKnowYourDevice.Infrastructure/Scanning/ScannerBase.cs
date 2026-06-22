using System.Diagnostics;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanning;

/// <summary>
/// Base for all scanners. Centralizes timing, progress, per-scanner timeout, cancellation,
/// and exception-to-status mapping so a single scanner failure never crashes the run or
/// fails the whole report. Subclasses implement only CollectAsync.
/// </summary>
public abstract class ScannerBase : IOrchestratableScanner
{
    protected ILogger Logger { get; }

    protected ScannerBase(ILogger logger) => Logger = logger;

    public abstract string Name { get; }
    public abstract ScanCategory Category { get; }

    /// <summary>
    /// Collect data into <paramref name="report"/>. Throw <see cref="UnauthorizedAccessException"/>
    /// to signal PermissionRequired. Use <paramref name="builder"/> to record source/warnings.
    /// Returns true when at least some data was collected (partial allowed).
    /// </summary>
    protected abstract Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct);

    public async Task<ScannerRunResult> RunAsync(CanonicalReport report, ScanContext ctx,
        IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        var started = DateTimeOffset.Now;
        var sw = Stopwatch.StartNew();
        var builder = new ScanResultBuilder();

        using var timeoutCts = new CancellationTokenSource(ctx.PerScannerTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            Logger.LogInformation("Scanner {Scanner} started", Name);
            var collected = await CollectAsync(report, ctx, builder, linked.Token).ConfigureAwait(false);

            var status = builder.Errors.Count > 0 && collected ? ScanStatus.PartialSuccess
                       : builder.Errors.Count > 0 ? ScanStatus.Failed
                       : collected ? ScanStatus.Success
                       : ScanStatus.Unavailable;

            Logger.LogInformation("Scanner {Scanner} completed with {Status} in {Ms}ms",
                Name, status, sw.ElapsedMilliseconds);

            return Build(status, builder, started, collected && builder.Errors.Count > 0);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Logger.LogWarning("Scanner {Scanner} cancelled", Name);
            return Build(ScanStatus.Cancelled, builder, started, false,
                "Scan was cancelled.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Scanner {Scanner} timed out after {Ms}ms", Name, sw.ElapsedMilliseconds);
            return Build(ScanStatus.TimedOut, builder, started, false,
                "Scanner timed out before completing.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Scanner {Scanner} needs elevation", Name);
            builder.RequiresElevation = true;
            return Build(ScanStatus.PermissionRequired, builder, started, false,
                "Administrator permission is required to read this data.", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Scanner {Scanner} failed", Name);
            return Build(ScanStatus.Failed, builder, started, false,
                "This scanner failed to complete. Other results are unaffected.", ex);
        }
    }

    private ScannerRunResult Build(ScanStatus status, ScanResultBuilder b, DateTimeOffset started,
        bool isPartial, string? userMessage = null, Exception? ex = null)
    {
        if (ex is not null) b.Error(ex.Message);
        return new ScannerRunResult
        {
            ScannerName = Name,
            Status = status,
            Source = b.Source,
            RequiresElevation = b.RequiresElevation,
            IsPartial = isPartial,
            StartedAt = started,
            CompletedAt = DateTimeOffset.Now,
            Warnings = b.Warnings,
            Errors = b.Errors,
            PropertiesUnavailable = b.PropertiesUnavailable,
            UserMessage = userMessage,
            TechnicalMessage = ex is null ? null : $"{ex.GetType().Name}: {ex.Message}"
        };
    }
}
