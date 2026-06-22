using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using Xunit;

namespace GetToKnowYourDevice.Scanner.Tests;

public class ScannerBaseTests
{
    private static ScanContext Ctx(TimeSpan? perScanner = null) => new()
    {
        PerScannerTimeout = perScanner ?? TimeSpan.FromSeconds(5)
    };

    [Fact]
    public async Task SuccessfulScanner_ReturnsSuccess()
    {
        var scanner = FakeScanner.Succeeds("ok");
        var result = await scanner.RunAsync(new CanonicalReport(), Ctx(), null, CancellationToken.None);
        Assert.Equal(ScanStatus.Success, result.Status);
    }

    [Fact]
    public async Task ThrowingScanner_ReturnsFailed_AndDoesNotThrow()
    {
        var scanner = FakeScanner.Throws("bad");
        var result = await scanner.RunAsync(new CanonicalReport(), Ctx(), null, CancellationToken.None);
        Assert.Equal(ScanStatus.Failed, result.Status);
        Assert.NotEmpty(result.Errors);
        Assert.NotNull(result.UserMessage);
    }

    [Fact]
    public async Task CancelledScanner_ReturnsCancelled()
    {
        var scanner = FakeScanner.Hangs("hang");
        using var cts = new CancellationTokenSource();
        var task = scanner.RunAsync(new CanonicalReport(), Ctx(), null, cts.Token);
        cts.Cancel();
        var result = await task;
        Assert.Equal(ScanStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task TimedOutScanner_ReturnsTimedOut()
    {
        var scanner = FakeScanner.Hangs("hang");
        // Very short per-scanner timeout forces a timeout (not external cancellation).
        var result = await scanner.RunAsync(new CanonicalReport(),
            Ctx(TimeSpan.FromMilliseconds(100)), null, CancellationToken.None);
        Assert.Equal(ScanStatus.TimedOut, result.Status);
    }

    [Fact]
    public async Task PermissionRequired_WhenUnauthorizedThrown()
    {
        var scanner = new FakeScanner("sec", ScanCategory.Security,
            (_, _, _) => throw new UnauthorizedAccessException());
        var result = await scanner.RunAsync(new CanonicalReport(), Ctx(), null, CancellationToken.None);
        Assert.Equal(ScanStatus.PermissionRequired, result.Status);
        Assert.True(result.RequiresElevation);
    }
}
