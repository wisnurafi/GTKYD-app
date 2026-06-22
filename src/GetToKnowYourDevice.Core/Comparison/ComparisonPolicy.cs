using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Comparison;

/// <summary>
/// One detected difference between baseline and comparison reports.
/// </summary>
public sealed class ReportDifference
{
    public required string PropertyPath { get; init; }
    public required string Section { get; init; }
    public string? BaselineValue { get; init; }
    public string? ComparisonValue { get; init; }
    public ChangeKind Kind { get; init; }
    public string? Description { get; init; }
}

/// <summary>Grouped comparison result.</summary>
public sealed class ComparisonResult
{
    public List<ReportDifference> Differences { get; init; } = [];
    public int AddedCount => Differences.Count(d => d.Kind == ChangeKind.Added);
    public int RemovedCount => Differences.Count(d => d.Kind == ChangeKind.Removed);
    public int ChangedCount => Differences.Count(d => d.Kind is ChangeKind.Changed or ChangeKind.Improved or ChangeKind.Warning or ChangeKind.Critical);
}

/// <summary>
/// Declares which canonical fields are stable, semi-stable, volatile, or sensitive.
/// Volatile fields are excluded from comparison by default so transient differences
/// (uptime, current load, current IP) don't show up as meaningful changes.
/// </summary>
public sealed class ComparisonPolicy
{
    /// <summary>Property-path substrings considered volatile and skipped by default.</summary>
    public HashSet<string> VolatilePaths { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "OperatingSystem.Uptime",
        "OperatingSystem.LastBootTime",
        "Processors.CurrentClockMhz",
        "Processors.CurrentLoadPercent",
        "MemorySummary.AvailableBytes",
        "MemorySummary.UsedBytes",
        "MemorySummary.UsagePercent",
        "MemorySummary.MemoryLoadPercent",
        "MemorySummary.PageFileAvailableBytes",
        "Batteries.ChargePercent",
        "Batteries.ChargingStatus",
        "Batteries.EstimatedRuntime",
        "Batteries.RemainingCapacityMwh",
        "Batteries.ChargeRateMw",
        "Batteries.DischargeRateMw",
        "Network.Summary.LinkSpeedBps",
        "Adapters.ReceiveSpeedBps",
        "Adapters.TransmitSpeedBps",
        "ReportMetadata.StartedAt",
        "ReportMetadata.CompletedAt",
        "ReportMetadata.ReportId",
        "Wifi.SignalQuality",
        "Wifi.ReceiveRateBps",
        "Wifi.TransmitRateBps"
    };

    public bool IsVolatile(string propertyPath)
    {
        // Flattened paths include array indices (e.g. Processors[0].CurrentLoadPercent).
        // Strip them so policy entries like "Processors.CurrentLoadPercent" still match.
        var normalized = System.Text.RegularExpressions.Regex.Replace(propertyPath, @"\[\d+\]", "");
        return VolatilePaths.Any(v => normalized.Contains(v, StringComparison.OrdinalIgnoreCase));
    }
}
