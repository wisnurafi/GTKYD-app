using System.Globalization;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed scan history. Snapshots are immutable except Name, Note, and IsPinned.
/// Uses a schema_version table so the database can be migrated on future app versions.
/// Pinned scans are never auto-removed by PruneAsync.
/// </summary>
public sealed class SqliteScanHistoryRepository : IScanHistoryRepository
{
    private const int CurrentSchemaVersion = 1;
    private readonly string _connectionString;
    private readonly ILogger<SqliteScanHistoryRepository> _logger;

    public SqliteScanHistoryRepository(string databasePath, ILogger<SqliteScanHistoryRepository> logger)
    {
        _logger = logger;
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_version (version INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS scans (
                ScanId TEXT PRIMARY KEY,
                DeviceId TEXT,
                ScanType INTEGER NOT NULL,
                ScanName TEXT,
                ScanDate TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                CompletedAt TEXT NOT NULL,
                DurationMs REAL NOT NULL,
                ApplicationVersion TEXT,
                WindowsVersion TEXT,
                HealthScore INTEGER NOT NULL,
                ScanStatus INTEGER NOT NULL,
                WarningCount INTEGER NOT NULL,
                ErrorCount INTEGER NOT NULL,
                IsPinned INTEGER NOT NULL DEFAULT 0,
                UserNote TEXT,
                ReportJson TEXT NOT NULL,
                ReportSchemaVersion TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_scans_date ON scans(ScanDate);
            CREATE INDEX IF NOT EXISTS idx_scans_health ON scans(HealthScore);
            """;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await ApplyMigrationsAsync(conn, ct).ConfigureAwait(false);
    }

    private async Task ApplyMigrationsAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using var read = conn.CreateCommand();
        read.CommandText = "SELECT version FROM schema_version LIMIT 1";
        var current = Convert.ToInt32(await read.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0);

        if (current == 0)
        {
            await using var ins = conn.CreateCommand();
            ins.CommandText = "INSERT INTO schema_version (version) VALUES ($v)";
            ins.Parameters.AddWithValue("$v", CurrentSchemaVersion);
            await ins.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Initialized scan history schema v{Version}", CurrentSchemaVersion);
        }
        else if (current < CurrentSchemaVersion)
        {
            // Future migrations applied stepwise here.
            await using var upd = conn.CreateCommand();
            upd.CommandText = "UPDATE schema_version SET version = $v";
            upd.Parameters.AddWithValue("$v", CurrentSchemaVersion);
            await upd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Migrated scan history schema {From} -> {To}", current, CurrentSchemaVersion);
        }
    }

    public async Task<string> SaveAsync(ScanHistoryRecord r, CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO scans (ScanId, DeviceId, ScanType, ScanName, ScanDate, StartedAt, CompletedAt,
                DurationMs, ApplicationVersion, WindowsVersion, HealthScore, ScanStatus, WarningCount,
                ErrorCount, IsPinned, UserNote, ReportJson, ReportSchemaVersion)
            VALUES ($id, $dev, $type, $name, $date, $start, $end, $dur, $appv, $winv, $health, $status,
                $warn, $err, $pin, $note, $json, $schema);
            """;
        cmd.Parameters.AddWithValue("$id", r.ScanId);
        cmd.Parameters.AddWithValue("$dev", (object?)r.DeviceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", (int)r.ScanType);
        cmd.Parameters.AddWithValue("$name", (object?)r.ScanName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$date", Iso(r.ScanDate));
        cmd.Parameters.AddWithValue("$start", Iso(r.StartedAt));
        cmd.Parameters.AddWithValue("$end", Iso(r.CompletedAt));
        cmd.Parameters.AddWithValue("$dur", r.DurationMs);
        cmd.Parameters.AddWithValue("$appv", (object?)r.ApplicationVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$winv", (object?)r.WindowsVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$health", r.HealthScore);
        cmd.Parameters.AddWithValue("$status", (int)r.ScanStatus);
        cmd.Parameters.AddWithValue("$warn", r.WarningCount);
        cmd.Parameters.AddWithValue("$err", r.ErrorCount);
        cmd.Parameters.AddWithValue("$pin", r.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$note", (object?)r.UserNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$json", r.ReportJson);
        cmd.Parameters.AddWithValue("$schema", r.ReportSchemaVersion);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return r.ScanId;
    }

    public async Task<ScanHistoryRecord?> GetAsync(string scanId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM scans WHERE ScanId = $id";
        cmd.Parameters.AddWithValue("$id", scanId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<ScanHistoryRecord>> QueryAsync(HistoryQuery q, CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.SearchText))
        {
            where.Add("(ScanName LIKE $s OR UserNote LIKE $s OR WindowsVersion LIKE $s)");
            cmd.Parameters.AddWithValue("$s", $"%{q.SearchText}%");
        }
        if (q.ScanType is { } t) { where.Add("ScanType = $t"); cmd.Parameters.AddWithValue("$t", (int)t); }
        if (q.Status is { } st) { where.Add("ScanStatus = $st"); cmd.Parameters.AddWithValue("$st", (int)st); }

        var orderCol = q.SortByHealthScore ? "HealthScore" : "ScanDate";
        var dir = q.Descending ? "DESC" : "ASC";
        cmd.CommandText = "SELECT * FROM scans" +
            (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") +
            $" ORDER BY IsPinned DESC, {orderCol} {dir}";

        var list = new List<ScanHistoryRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false)) list.Add(Map(reader));
        return list;
    }

    public Task RenameAsync(string scanId, string name, CancellationToken ct = default)
        => UpdateFieldAsync(scanId, "ScanName", name, ct);

    public Task SetNoteAsync(string scanId, string? note, CancellationToken ct = default)
        => UpdateFieldAsync(scanId, "UserNote", (object?)note ?? DBNull.Value, ct);

    public Task SetPinnedAsync(string scanId, bool pinned, CancellationToken ct = default)
        => UpdateFieldAsync(scanId, "IsPinned", pinned ? 1 : 0, ct);

    private async Task UpdateFieldAsync(string scanId, string column, object value, CancellationToken ct)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE scans SET {column} = $v WHERE ScanId = $id";
        cmd.Parameters.AddWithValue("$v", value);
        cmd.Parameters.AddWithValue("$id", scanId);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string scanId, CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM scans WHERE ScanId = $id";
        cmd.Parameters.AddWithValue("$id", scanId);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteManyAsync(IEnumerable<string> scanIds, CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var tx = conn.BeginTransaction();
        foreach (var id in scanIds)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM scans WHERE ScanId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        tx.Commit();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM scans";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0);
    }

    public async Task PruneAsync(int maxRecords, CancellationToken ct = default)
    {
        if (maxRecords <= 0) return;
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        // Delete oldest UNPINNED scans beyond the limit. Pinned scans are never removed.
        cmd.CommandText = """
            DELETE FROM scans WHERE ScanId IN (
                SELECT ScanId FROM scans WHERE IsPinned = 0
                ORDER BY ScanDate DESC
                LIMIT -1 OFFSET $max
            );
            """;
        cmd.Parameters.AddWithValue("$max", maxRecords);
        var removed = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (removed > 0) _logger.LogInformation("Pruned {Count} old unpinned scans", removed);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM scans";
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static string Iso(DateTimeOffset dt) => dt.ToString("o", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseIso(string s)
        => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static ScanHistoryRecord Map(SqliteDataReader r) => new()
    {
        ScanId = r.GetString(r.GetOrdinal("ScanId")),
        DeviceId = r.IsDBNull(r.GetOrdinal("DeviceId")) ? null : r.GetString(r.GetOrdinal("DeviceId")),
        ScanType = (ScanMode)r.GetInt32(r.GetOrdinal("ScanType")),
        ScanName = r.IsDBNull(r.GetOrdinal("ScanName")) ? null : r.GetString(r.GetOrdinal("ScanName")),
        ScanDate = ParseIso(r.GetString(r.GetOrdinal("ScanDate"))),
        StartedAt = ParseIso(r.GetString(r.GetOrdinal("StartedAt"))),
        CompletedAt = ParseIso(r.GetString(r.GetOrdinal("CompletedAt"))),
        DurationMs = r.GetDouble(r.GetOrdinal("DurationMs")),
        ApplicationVersion = r.IsDBNull(r.GetOrdinal("ApplicationVersion")) ? null : r.GetString(r.GetOrdinal("ApplicationVersion")),
        WindowsVersion = r.IsDBNull(r.GetOrdinal("WindowsVersion")) ? null : r.GetString(r.GetOrdinal("WindowsVersion")),
        HealthScore = r.GetInt32(r.GetOrdinal("HealthScore")),
        ScanStatus = (ScanStatus)r.GetInt32(r.GetOrdinal("ScanStatus")),
        WarningCount = r.GetInt32(r.GetOrdinal("WarningCount")),
        ErrorCount = r.GetInt32(r.GetOrdinal("ErrorCount")),
        IsPinned = r.GetInt32(r.GetOrdinal("IsPinned")) == 1,
        UserNote = r.IsDBNull(r.GetOrdinal("UserNote")) ? null : r.GetString(r.GetOrdinal("UserNote")),
        ReportJson = r.GetString(r.GetOrdinal("ReportJson")),
        ReportSchemaVersion = r.GetString(r.GetOrdinal("ReportSchemaVersion"))
    };
}
