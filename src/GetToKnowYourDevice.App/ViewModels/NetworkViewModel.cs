using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>
/// Exposes the network section for the Network page. External diagnostics and public IP lookup
/// are deliberately not run here; those are opt-in user actions gated by privacy settings.
/// </summary>
public sealed partial class NetworkViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    [ObservableProperty] private NetworkInfo? _network;

    protected override void Project(CanonicalReport r) => Network = r.Network;
    protected override void Clear() => Network = null;
}
