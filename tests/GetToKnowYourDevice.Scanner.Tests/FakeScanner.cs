using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;

namespace GetToKnowYourDevice.Scanner.Tests;

/// <summary>Test double: a scanner whose behavior is controlled by the constructor delegate.</summary>
internal sealed class FakeScanner : ScannerBase
{
    private readonly Func<CanonicalReport, ScanResultBuilder, CancellationToken, Task<bool>> _collect;

    public FakeScanner(string name, ScanCategory category,
        Func<CanonicalReport, ScanResultBuilder, CancellationToken, Task<bool>> collect)
        : base(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance)
    {
        Name = name;
        Category = category;
        _collect = collect;
    }

    public override string Name { get; }
    public override ScanCategory Category { get; }

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct) => _collect(report, builder, ct);

    // Convenience factories
    public static FakeScanner Succeeds(string name) =>
        new(name, ScanCategory.System, (_, b, _) => { b.Source = "fake"; return Task.FromResult(true); });

    public static FakeScanner Throws(string name) =>
        new(name, ScanCategory.System, (_, _, _) => throw new InvalidOperationException("boom"));

    public static FakeScanner Hangs(string name) =>
        new(name, ScanCategory.System, async (_, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return true;
        });
}
