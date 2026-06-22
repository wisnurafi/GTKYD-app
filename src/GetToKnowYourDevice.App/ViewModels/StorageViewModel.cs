using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Exposes the storage section for the Storage page.</summary>
public sealed partial class StorageViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    [ObservableProperty] private StorageInfo? _storage;

    protected override void Project(CanonicalReport r) => Storage = r.Storage;
    protected override void Clear() => Storage = null;
}
