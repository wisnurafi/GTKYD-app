using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Health;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Orchestration;
using GetToKnowYourDevice.Infrastructure.Scanning;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GetToKnowYourDevice.Scanner.Tests;

public class ScanOrchestratorTests
{
    private static ScanOrchestrator Build(params IOrchestratableScanner[] scanners) =>
        new(scanners, new HealthScoreEngine(), NullLogger<ScanOrchestrator>.Instance);

    private static ScanContext FullCtx() => new()
    {
        Mode = ScanMode.Full,
        PerScannerTimeout = TimeSpan.FromSeconds(5),
        MaxParallelScanners = 4
    };

    [Fact]
    public async Task OneScannerFails_OthersStillProduceResults()
    {
        var good1 = FakeScanner.Succeeds("System");
        var bad = FakeScanner.Throws("Storage");
        var good2 = FakeScanner.Succeeds("Network");

        var report = await Build(good1, bad, good2).RunScanAsync(FullCtx(), null, CancellationToken.None);

        // All three recorded; the failure did not abort the run.
        Assert.Equal(3, report.ScanDiagnostics.Count);
        Assert.Contains(report.ScanDiagnostics, d => d.Status == ScanStatus.Failed);
        Assert.Equal(2, report.ScanDiagnostics.Count(d => d.Status == ScanStatus.Success));
    }

    [Fact]
    public async Task PartialFailure_OverallStatusIsPartialSuccess()
    {
        var report = await Build(FakeScanner.Succeeds("System"), FakeScanner.Throws("Storage"))
            .RunScanAsync(FullCtx(), null, CancellationToken.None);

        Assert.Equal(ScanStatus.PartialSuccess, report.ReportMetadata.OverallStatus);
    }

    [Fact]
    public async Task AllSucceed_OverallStatusIsSuccess()
    {
        var report = await Build(FakeScanner.Succeeds("System"), FakeScanner.Succeeds("Network"))
            .RunScanAsync(FullCtx(), null, CancellationToken.None);

        Assert.Equal(ScanStatus.Success, report.ReportMetadata.OverallStatus);
    }

    [Fact]
    public async Task Orchestrator_ComputesHealthScore()
    {
        var report = await Build(FakeScanner.Succeeds("System"))
            .RunScanAsync(FullCtx(), null, CancellationToken.None);

        Assert.NotNull(report.Health);
        Assert.InRange(report.Health.Score, 0, 100);
    }

    [Fact]
    public async Task Orchestrator_ReportsProgress()
    {
        var updates = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(p => updates.Add(p));

        await Build(FakeScanner.Succeeds("System"), FakeScanner.Succeeds("Network"))
            .RunScanAsync(FullCtx(), progress, CancellationToken.None);

        // Progress is async; give the synchronization context a moment to drain.
        await Task.Delay(50);
        Assert.NotEmpty(updates);
    }
}
