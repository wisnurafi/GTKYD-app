using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Persistence;

/// <summary>
/// Stored scan snapshot. The full canonical report is kept as JSON; the rest are
/// indexed columns. Snapshots are immutable except Name, Note, and IsPinned.
/// </summary>
public sealed class ScanHistoryRecord
{
    public string ScanId { get; set; } = Guid.NewGuid().ToString("N");
    public string? DeviceId { get; set; }
    public ScanMode ScanType { get; set; }
    public string? ScanName { get; set; }
    public DateTimeOffset ScanDate { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public double DurationMs { get; set; }
    public string? ApplicationVersion { get; set; }
    public string? WindowsVersion { get; set; }
    public int HealthScore { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public bool IsPinned { get; set; }
    public string? UserNote { get; set; }
    public string ReportJson { get; set; } = "{}";
    public string ReportSchemaVersion { get; set; } = "1.0";
}

/// <summary>Filter/sort criteria for querying scan history.</summary>
public sealed class HistoryQuery
{
    public string? SearchText { get; set; }
    public ScanMode? ScanType { get; set; }
    public ScanStatus? Status { get; set; }
    public bool SortByHealthScore { get; set; }
    public bool Descending { get; set; } = true;
}

/// <summary>Persistence for scan snapshots. Implemented over SQLite in Infrastructure.</summary>
public interface IScanHistoryRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<string> SaveAsync(ScanHistoryRecord record, CancellationToken ct = default);
    Task<ScanHistoryRecord?> GetAsync(string scanId, CancellationToken ct = default);
    Task<IReadOnlyList<ScanHistoryRecord>> QueryAsync(HistoryQuery query, CancellationToken ct = default);
    Task RenameAsync(string scanId, string name, CancellationToken ct = default);
    Task SetNoteAsync(string scanId, string? note, CancellationToken ct = default);
    Task SetPinnedAsync(string scanId, bool pinned, CancellationToken ct = default);
    Task DeleteAsync(string scanId, CancellationToken ct = default);
    Task DeleteManyAsync(IEnumerable<string> scanIds, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Trims oldest unpinned scans beyond maxRecords. Never removes pinned scans.</summary>
    Task PruneAsync(int maxRecords, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
