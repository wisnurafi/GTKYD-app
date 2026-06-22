using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;
using Microsoft.UI.Dispatching;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>
/// Base for page view models that project one section of the current canonical report. Subscribes
/// to <see cref="AppScanService.CurrentReportChanged"/> and re-projects on the UI thread whenever a
/// new scan completes or a saved report is opened.
/// </summary>
public abstract partial class ReportSectionViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    protected AppScanService ScanService { get; }

    protected ReportSectionViewModel(AppScanService scanService)
    {
        ScanService = scanService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        ScanService.CurrentReportChanged += OnReportChanged;
        if (scanService.CurrentReport is { } existing) Project(existing);
    }

    public bool HasReport => ScanService.CurrentReport is not null;

    private void OnReportChanged(object? sender, CanonicalReport? report)
    {
        void Apply()
        {
            OnPropertyChanged(nameof(HasReport));
            if (report is not null) Project(report);
            else Clear();
        }
        if (_dispatcher.HasThreadAccess) Apply();
        else _dispatcher.TryEnqueue(Apply);
    }

    /// <summary>Map the report's relevant section onto observable properties.</summary>
    protected abstract void Project(CanonicalReport report);

    /// <summary>Reset observable properties when no report is loaded.</summary>
    protected virtual void Clear() { }

    public void Dispose() => ScanService.CurrentReportChanged -= OnReportChanged;
}
