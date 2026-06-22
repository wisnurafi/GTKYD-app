using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Models;

/// <summary>Device health score with full breakdown so the score is explainable, not a black box.</summary>
public sealed class HealthInfo
{
    public int Score { get; set; }
    public int BaseScore { get; set; } = 100;
    public List<HealthFinding> Findings { get; set; } = [];
    public Dictionary<HealthCategory, int> CategoryScores { get; set; } = [];
}

/// <summary>One evaluated health rule and its effect on the score.</summary>
public sealed class HealthFinding
{
    public required string RuleId { get; set; }
    public HealthCategory Category { get; set; }
    public HealthSeverity Severity { get; set; }
    public int Deduction { get; set; }
    public required string Reason { get; set; }
    public string? SourceProperty { get; set; }
    public string? Recommendation { get; set; }
}

/// <summary>Per-scanner diagnostic entry persisted in the report's scanDiagnostics array.</summary>
public sealed class ScanDiagnostic
{
    public required string ScannerName { get; set; }
    public ScanStatus Status { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public double DurationMs => (CompletedAt - StartedAt).TotalMilliseconds;
    public bool RequiresElevation { get; set; }
    public bool IsPartial { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public string? UserMessage { get; set; }
    public string? TechnicalMessage { get; set; }
    public List<string> PropertiesUnavailable { get; set; } = [];
}
