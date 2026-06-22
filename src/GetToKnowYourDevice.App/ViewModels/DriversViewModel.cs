using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

public enum DriverFilter { All, Signed, Unsigned, Error, Microsoft, ThirdParty, OldCandidate }

/// <summary>
/// Drives the Drivers table: search, filter, and sort over the report's driver list. The
/// "Old driver candidate" filter is informational only (local date heuristic, not an online check).
/// </summary>
public sealed partial class DriversViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    private List<DriverInfo> _all = [];

    public ObservableCollection<DriverInfo> Drivers { get; } = [];

    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private DriverFilter _filter;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _filteredCount;
    [ObservableProperty] private DriverInfo? _selectedDriver;

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnFilterChanged(DriverFilter value) => ApplyFilter();

    protected override void Project(CanonicalReport r)
    {
        _all = r.Drivers;
        TotalCount = _all.Count;
        ApplyFilter();
    }

    protected override void Clear()
    {
        _all = [];
        Drivers.Clear();
        TotalCount = FilteredCount = 0;
    }

    private void ApplyFilter()
    {
        IEnumerable<DriverInfo> q = _all;

        q = Filter switch
        {
            DriverFilter.Signed => q.Where(d => d.IsSigned == true),
            DriverFilter.Unsigned => q.Where(d => d.IsSigned == false),
            DriverFilter.Error => q.Where(d => d.ConfigManagerErrorCode is > 0),
            DriverFilter.Microsoft => q.Where(d =>
                (d.DriverProvider ?? d.Manufacturer ?? "").Contains("Microsoft", StringComparison.OrdinalIgnoreCase)),
            DriverFilter.ThirdParty => q.Where(d =>
                !(d.DriverProvider ?? d.Manufacturer ?? "").Contains("Microsoft", StringComparison.OrdinalIgnoreCase)),
            DriverFilter.OldCandidate => q.Where(d => d.IsOldDriverCandidate),
            _ => q
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            q = q.Where(d =>
                (d.DeviceName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Manufacturer?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.DeviceClass?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.DriverProvider?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var ordered = q.OrderBy(d => d.DeviceName, StringComparer.OrdinalIgnoreCase).ToList();
        Drivers.Clear();
        foreach (var d in ordered) Drivers.Add(d);
        FilteredCount = ordered.Count;
    }
}
