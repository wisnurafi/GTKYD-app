using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Exposes batteries for the Battery page. Empty list is a normal (desktop) condition.</summary>
public sealed partial class BatteryViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    public ObservableCollection<BatteryInfo> Batteries { get; } = [];

    [ObservableProperty] private bool _hasBatteries;

    protected override void Project(CanonicalReport r)
    {
        Batteries.Clear();
        foreach (var b in r.Batteries) Batteries.Add(b);
        HasBatteries = Batteries.Count > 0;
    }

    protected override void Clear()
    {
        Batteries.Clear();
        HasBatteries = false;
    }
}
