using GetToKnowYourDevice.Core.Comparison;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using Xunit;

namespace GetToKnowYourDevice.Core.Tests;

public class ReportComparerTests
{
    private static CanonicalReport Make(long ram, double? cpuLoad, int build)
    {
        var r = new CanonicalReport();
        r.Hardware.MemorySummary.InstalledBytes = ram;
        r.Hardware.Processors.Add(new ProcessorInfo { Name = "CPU", CurrentLoadPercent = cpuLoad });
        r.OperatingSystem.BuildNumber = build.ToString();
        return r;
    }

    [Fact]
    public void Compare_StableFieldChanged_IsDetected()
    {
        var baseline = Make(16_000_000_000, 10, 22000);
        var comparison = Make(32_000_000_000, 10, 22000);

        var result = new ReportComparer().Compare(baseline, comparison);

        Assert.Contains(result.Differences, d =>
            d.PropertyPath.Contains("InstalledBytes"));
    }

    [Fact]
    public void Compare_VolatileFieldChanged_IsIgnoredByDefault()
    {
        var baseline = Make(16_000_000_000, 10, 22000);
        var comparison = Make(16_000_000_000, 95, 22000); // only CPU load changed

        var result = new ReportComparer().Compare(baseline, comparison);

        Assert.DoesNotContain(result.Differences, d =>
            d.PropertyPath.Contains("CurrentLoadPercent"));
    }

    [Fact]
    public void Compare_VolatileFieldChanged_IncludedWhenRequested()
    {
        var baseline = Make(16_000_000_000, 10, 22000);
        var comparison = Make(16_000_000_000, 95, 22000);

        var result = new ReportComparer().Compare(baseline, comparison, includeVolatile: true);

        Assert.Contains(result.Differences, d =>
            d.PropertyPath.Contains("CurrentLoadPercent"));
    }

    [Fact]
    public void Compare_RamIncrease_ClassifiedImproved()
    {
        var baseline = Make(16_000_000_000, 10, 22000);
        var comparison = Make(32_000_000_000, 10, 22000);

        var result = new ReportComparer().Compare(baseline, comparison);
        var ramDiff = result.Differences.First(d => d.PropertyPath.Contains("InstalledBytes"));

        Assert.Equal(ChangeKind.Improved, ramDiff.Kind);
    }

    [Fact]
    public void Compare_SecureBootDisabled_ClassifiedCritical()
    {
        var baseline = new CanonicalReport();
        baseline.Security.Device.SecureBootEnabled = true;
        var comparison = new CanonicalReport();
        comparison.Security.Device.SecureBootEnabled = false;

        var result = new ReportComparer().Compare(baseline, comparison);
        var diff = result.Differences.First(d => d.PropertyPath.Contains("SecureBootEnabled"));

        Assert.Equal(ChangeKind.Critical, diff.Kind);
    }

    [Fact]
    public void Compare_IdenticalReports_NoDifferences()
    {
        var a = Make(16_000_000_000, 10, 22000);
        var b = Make(16_000_000_000, 10, 22000);

        var result = new ReportComparer().Compare(a, b);

        Assert.Empty(result.Differences);
    }
}
