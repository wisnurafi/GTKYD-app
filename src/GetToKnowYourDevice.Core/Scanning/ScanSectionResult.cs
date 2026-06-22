using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Scanning;

/// <summary>
/// Result of one scanner section. Carries data plus full diagnostics so a single
/// scanner failure never fails the whole report.
/// </summary>
public sealed class ScanSectionResult<T>
{
    public required string ScannerName { get; init; }
    public T? Data { get; init; }
    public ScanStatus Status { get; init; } = ScanStatus.Unavailable;

    /// <summary>Data source actually used (e.g. "WMI:Win32_Processor", "Registry", "PowerShell").</summary>
    public string? Source { get; init; }

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public TimeSpan Duration => CompletedAt - StartedAt;

    public IReadOnlyList<ScanWarning> Warnings { get; init; } = [];
    public IReadOnlyList<ScanError> Errors { get; init; } = [];

    public bool RequiresElevation { get; init; }
    public bool IsPartial { get; init; }

    /// <summary>Properties the scanner tried to read.</summary>
    public IReadOnlyList<string> PropertiesAttempted { get; init; } = [];

    /// <summary>Properties that could not be read, with reason.</summary>
    public IReadOnlyList<string> PropertiesUnavailable { get; init; } = [];

    public static ScanSectionResult<T> ForSuccess(string scanner, T data, string source,
        DateTimeOffset start, DateTimeOffset end) => new()
    {
        ScannerName = scanner,
        Data = data,
        Status = ScanStatus.Success,
        Source = source,
        StartedAt = start,
        CompletedAt = end
    };

    public static ScanSectionResult<T> ForFailure(string scanner, ScanStatus status,
        ScanError error, DateTimeOffset start, DateTimeOffset end) => new()
    {
        ScannerName = scanner,
        Status = status,
        StartedAt = start,
        CompletedAt = end,
        Errors = [error],
        RequiresElevation = status == ScanStatus.PermissionRequired
    };
}

/// <summary>
/// A scanner that reads one section of device data. Implementations live in Infrastructure
/// so the platform abstraction stays out of Core. Generic over the section payload type.
/// </summary>
public interface IDeviceScanner<T>
{
    string Name { get; }

    /// <summary>Categories this scanner belongs to; orchestrator selects by scan mode.</summary>
    ScanCategory Category { get; }

    Task<ScanSectionResult<T>> ScanAsync(
        ScanContext context,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken);
}
