using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Scanning;

/// <summary>Input passed to every scanner for a single scan run.</summary>
public sealed class ScanContext
{
    public ScanMode Mode { get; init; } = ScanMode.Quick;
    public ScanCategory Categories { get; init; } = ScanCategory.None;
    public bool IncludeSmartData { get; init; }
    public bool IncludeDriverScan { get; init; }
    public bool IncludeSecurityScan { get; init; }
    public bool IncludePeripheralScan { get; init; }
    public bool IncludeExternalNetworkDiagnostics { get; init; }
    public bool AllowElevation { get; init; }
    public bool SavePartialResult { get; init; } = true;

    /// <summary>Per-scanner timeout. Orchestrator enforces this.</summary>
    public TimeSpan PerScannerTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Overall scan timeout across all scanners.</summary>
    public TimeSpan OverallTimeout { get; init; } = TimeSpan.FromMinutes(5);

    public int MaxParallelScanners { get; init; } = 4;

    /// <summary>Stable identifier for the device being scanned (set after identity scan).</summary>
    public string? DeviceId { get; set; }

    public bool Includes(ScanCategory category) => (Categories & category) == category;
}

/// <summary>Progress update emitted while scanning.</summary>
public sealed record ScanProgress
{
    public required string ScannerName { get; init; }
    public required string Status { get; init; }
    public int CompletedScanners { get; init; }
    public int TotalScanners { get; init; }
    public TimeSpan Elapsed { get; init; }

    public double Percentage =>
        TotalScanners <= 0 ? 0 : Math.Clamp((double)CompletedScanners / TotalScanners * 100.0, 0, 100);
}

/// <summary>A warning attached to a scanner result. Non-fatal.</summary>
public sealed record ScanWarning(string Property, string Message);

/// <summary>An error attached to a scanner result. Keeps user + technical messages separate.</summary>
public sealed record ScanError
{
    public required string UserMessage { get; init; }
    public string? TechnicalMessage { get; init; }
    public string? ExceptionType { get; init; }
    public string? Property { get; init; }
}
