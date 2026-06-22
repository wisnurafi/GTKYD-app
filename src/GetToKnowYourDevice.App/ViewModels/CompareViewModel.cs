using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Comparison;
using GetToKnowYourDevice.Core.Persistence;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>
/// Backs the Compare Reports page. Picks a baseline and a comparison snapshot from history and
/// diffs them with <see cref="ReportComparer"/>, which skips volatile fields by default.
/// </summary>
public sealed partial class CompareViewModel : ViewModelBase
{
    private readonly IScanHistoryRepository _repo;
    private readonly ReportComparer _comparer = new();
    private readonly ILogger<CompareViewModel> _logger;

    public CompareViewModel(IScanHistoryRepository repo, ILogger<CompareViewModel> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public ObservableCollection<ScanHistoryRecord> AvailableScans { get; } = [];
    public ObservableCollection<ReportDifference> Differences { get; } = [];

    [ObservableProperty] private ScanHistoryRecord? _baseline;
    [ObservableProperty] private ScanHistoryRecord? _comparison;
    [ObservableProperty] private bool _includeVolatile;
    [ObservableProperty] private string? _sectionFilter;
    [ObservableProperty] private int _addedCount;
    [ObservableProperty] private int _removedCount;
    [ObservableProperty] private int _changedCount;
    [ObservableProperty] private bool _hasResult;

    [RelayCommand]
    public async Task LoadScansAsync()
    {
        await _repo.InitializeAsync();
        var items = await _repo.QueryAsync(new HistoryQuery { Descending = true });
        AvailableScans.Clear();
        foreach (var r in items) AvailableScans.Add(r);
    }

    [RelayCommand]
    public async Task CompareAsync()
    {
        ClearError();
        HasResult = false;
        if (Baseline is null || Comparison is null)
        {
            SetError("Select both a baseline and a comparison scan.");
            return;
        }

        IsBusy = true;
        try
        {
            var baseFull = await _repo.GetAsync(Baseline.ScanId);
            var compFull = await _repo.GetAsync(Comparison.ScanId);
            var baseReport = baseFull is null ? null : AppScanService.DeserializeReport(baseFull.ReportJson);
            var compReport = compFull is null ? null : AppScanService.DeserializeReport(compFull.ReportJson);
            if (baseReport is null || compReport is null)
            {
                SetError("One of the snapshots could not be read.");
                return;
            }

            var result = _comparer.Compare(baseReport, compReport, IncludeVolatile);
            var diffs = result.Differences.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SectionFilter) && SectionFilter != "All")
                diffs = diffs.Where(d => d.Section.Equals(SectionFilter, StringComparison.OrdinalIgnoreCase));

            Differences.Clear();
            foreach (var d in diffs) Differences.Add(d);

            AddedCount = result.AddedCount;
            RemovedCount = result.RemovedCount;
            ChangedCount = result.ChangedCount;
            HasResult = true;
        }
        catch (Exception ex)
        {
            SetError($"Comparison failed: {ex.Message}");
            _logger.LogError(ex, "Comparison failed");
        }
        finally { IsBusy = false; }
    }
}
