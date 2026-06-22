using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Exposes the security section for the (read-only) Security page.</summary>
public sealed partial class SecurityViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    [ObservableProperty] private SecurityInfo? _security;

    protected override void Project(CanonicalReport r) => Security = r.Security;
    protected override void Clear() => Security = null;
}
