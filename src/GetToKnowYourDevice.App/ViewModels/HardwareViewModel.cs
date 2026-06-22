using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Exposes the full hardware section for the Hardware page.</summary>
public sealed partial class HardwareViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    [ObservableProperty] private HardwareInfo? _hardware;

    protected override void Project(CanonicalReport r) => Hardware = r.Hardware;
    protected override void Clear() => Hardware = null;
}
