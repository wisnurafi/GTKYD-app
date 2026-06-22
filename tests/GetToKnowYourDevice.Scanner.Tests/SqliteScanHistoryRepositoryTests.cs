using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Persistence;
using GetToKnowYourDevice.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GetToKnowYourDevice.Scanner.Tests;

public class SqliteScanHistoryRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteScanHistoryRepository _repo;

    public SqliteScanHistoryRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"gtkyd-test-{Guid.NewGuid():N}.db");
        _repo = new SqliteScanHistoryRepository(_dbPath, NullLogger<SqliteScanHistoryRepository>.Instance);
    }

    private static ScanHistoryRecord Record(string? name = null, bool pinned = false, int health = 90) => new()
    {
        ScanId = Guid.NewGuid().ToString("N"),
        ScanType = ScanMode.Quick,
        ScanName = name,
        ScanDate = DateTimeOffset.Now,
        StartedAt = DateTimeOffset.Now,
        CompletedAt = DateTimeOffset.Now,
        HealthScore = health,
        ScanStatus = ScanStatus.Success,
        IsPinned = pinned,
        ReportJson = """{"reportMetadata":{}}"""
    };

    [Fact]
    public async Task InsertAndRetrieve_RoundTrips()
    {
        await _repo.InitializeAsync();
        var record = Record("My Scan");
        await _repo.SaveAsync(record);

        var loaded = await _repo.GetAsync(record.ScanId);

        Assert.NotNull(loaded);
        Assert.Equal("My Scan", loaded!.ScanName);
        Assert.Equal(90, loaded.HealthScore);
        Assert.Equal(record.ReportJson, loaded.ReportJson);
    }

    [Fact]
    public async Task Rename_UpdatesName()
    {
        await _repo.InitializeAsync();
        var record = Record("Old");
        await _repo.SaveAsync(record);

        await _repo.RenameAsync(record.ScanId, "New");

        var loaded = await _repo.GetAsync(record.ScanId);
        Assert.Equal("New", loaded!.ScanName);
    }

    [Fact]
    public async Task Pin_PersistsFlag()
    {
        await _repo.InitializeAsync();
        var record = Record();
        await _repo.SaveAsync(record);

        await _repo.SetPinnedAsync(record.ScanId, true);

        var loaded = await _repo.GetAsync(record.ScanId);
        Assert.True(loaded!.IsPinned);
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        await _repo.InitializeAsync();
        var record = Record();
        await _repo.SaveAsync(record);

        await _repo.DeleteAsync(record.ScanId);

        Assert.Null(await _repo.GetAsync(record.ScanId));
    }

    [Fact]
    public async Task Prune_KeepsPinned_RemovesOldestUnpinned()
    {
        await _repo.InitializeAsync();
        // 1 pinned + 5 unpinned; prune to max 2.
        await _repo.SaveAsync(Record("pinned", pinned: true));
        for (var i = 0; i < 5; i++) await _repo.SaveAsync(Record($"unpinned-{i}"));

        await _repo.PruneAsync(maxRecords: 2);

        var all = await _repo.QueryAsync(new HistoryQuery());
        // Pinned must survive regardless of the limit.
        Assert.Contains(all, r => r.ScanName == "pinned");
    }

    [Fact]
    public async Task Query_FilterByStatus()
    {
        await _repo.InitializeAsync();
        var ok = Record("ok");
        var failed = Record("failed");
        failed.ScanStatus = ScanStatus.Failed;
        await _repo.SaveAsync(ok);
        await _repo.SaveAsync(failed);

        var results = await _repo.QueryAsync(new HistoryQuery { Status = ScanStatus.Failed });

        Assert.Single(results);
        Assert.Equal("failed", results[0].ScanName);
    }

    [Fact]
    public async Task Count_ReflectsInserts()
    {
        await _repo.InitializeAsync();
        await _repo.SaveAsync(Record());
        await _repo.SaveAsync(Record());
        Assert.Equal(2, await _repo.CountAsync());
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* temp file */ }
    }
}
