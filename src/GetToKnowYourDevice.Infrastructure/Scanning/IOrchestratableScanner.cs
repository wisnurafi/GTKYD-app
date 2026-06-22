using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;

namespace GetToKnowYourDevice.Infrastructure.Scanning;

/// <summary>Flat outcome of one scanner run, used by the orchestrator to build diagnostics.</summary>
public sealed class ScannerRunResult
{
    public required string ScannerName { get; init; }
    public ScanStatus Status { get; init; }
    public string? Source { get; init; }
    public bool RequiresElevation { get; init; }
    public bool IsPartial { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public List<string> Warnings { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public List<string> PropertiesUnavailable { get; init; } = [];
    public string? UserMessage { get; init; }
    public string? TechnicalMessage { get; init; }
}

/// <summary>Non-generic scanner contract the orchestrator drives uniformly.</summary>
public interface IOrchestratableScanner
{
    string Name { get; }
    ScanCategory Category { get; }

    /// <summary>Runs the scan and writes its section directly into the report.</summary>
    Task<ScannerRunResult> RunAsync(CanonicalReport report, ScanContext ctx,
        IProgress<ScanProgress>? progress, CancellationToken ct);
}

/// <summary>Accumulates diagnostics while a scanner collects data.</summary>
public sealed class ScanResultBuilder
{
    public string? Source { get; set; }
    public bool RequiresElevation { get; set; }
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
    public List<string> PropertiesUnavailable { get; } = [];

    public void Warn(string message) => Warnings.Add(message);
    public void Error(string message) => Errors.Add(message);
    public void Unavailable(string property, string? reason = null)
        => PropertiesUnavailable.Add(reason is null ? property : $"{property}: {reason}");
}
