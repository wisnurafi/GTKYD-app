using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Persistence;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>
/// Backs the Scan History page: lists stored snapshots with search/filter/sort, and supports
/// open, rename, note, pin, and delete. Snapshots are immutable except name/note/pin. Delete
/// confirmation is handled by the view before calling the delete command.
/// </summary>
public sealed partial class ScanHistoryViewModel : ViewModelBase
{
    private readonly IScanHistoryRepository _repo;
    private readonly AppScanService _scanService;
    private readonly ILogger<ScanHistoryViewModel> _logger;

    public ScanHistoryViewModel(IScanHistoryRepository repo, AppScanService scanService,
        ILogger<ScanHistoryViewModel> logger)
    {
        _repo = repo;
        _scanService = scanService;
        _logger = logger;
    }

    public ObservableCollection<ScanHistoryRecord> Records { get; } = [];

    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private bool _sortByHealth;
    [ObservableProperty] private ScanHistoryRecord? _selectedRecord;
    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            await _repo.InitializeAsync();
            var query = new HistoryQuery
            {
                SearchText = SearchText,
                SortByHealthScore = SortByHealth,
                Descending = true
            };
            var items = await _repo.QueryAsync(query);
            Records.Clear();
            foreach (var r in items) Records.Add(r);
            IsEmpty = Records.Count == 0;
        }
        catch (Exception ex)
        {
            SetError($"Failed to load history: {ex.Message}");
            _logger.LogError(ex, "Load history failed");
        }
        finally { IsBusy = false; }
    }

    /// <summary>Loads a stored snapshot into the current report so all pages reflect it.</summary>
    [RelayCommand]
    public async Task OpenAsync(ScanHistoryRecord? record)
    {
        if (record is null) return;
        var full = await _repo.GetAsync(record.ScanId);
        if (full is null) { SetError("Snapshot not found."); return; }

        var report = AppScanService.DeserializeReport(full.ReportJson);
        if (report is null) { SetError("Snapshot could not be read."); return; }
        _scanService.SetCurrentReport(report);
    }

    [RelayCommand]
    public async Task RenameAsync((string id, string name) args)
    {
        await _repo.RenameAsync(args.id, args.name);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task SetNoteAsync((string id, string? note) args)
    {
        await _repo.SetNoteAsync(args.id, args.note);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task TogglePinAsync(ScanHistoryRecord? record)
    {
        if (record is null) return;
        await _repo.SetPinnedAsync(record.ScanId, !record.IsPinned);
        await LoadAsync();
    }

    /// <summary>Deletes a snapshot. The view must confirm before invoking this.</summary>
    [RelayCommand]
    public async Task DeleteAsync(ScanHistoryRecord? record)
    {
        if (record is null) return;
        await _repo.DeleteAsync(record.ScanId);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task ClearAllAsync()
    {
        await _repo.ClearAsync();
        await LoadAsync();
    }
}
