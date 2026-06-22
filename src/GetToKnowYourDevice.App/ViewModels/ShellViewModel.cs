using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>
/// Hosts the global scan progress area: which scanner is running, percentage, elapsed time,
/// cancel command, and warning/error/completion status. Scans run off the UI thread; progress
/// is marshaled back via the dispatcher queue.
/// </summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly AppScanService _scanService;
    private readonly ISettingsService _settings;
    private readonly ILogger<ShellViewModel> _logger;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;

    public ShellViewModel(AppScanService scanService, ISettingsService settings,
        ILogger<ShellViewModel> logger)
    {
        _scanService = scanService;
        _settings = settings;
        _logger = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        SelectedScanMode = settings.Current.DefaultScanType;
    }

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string? _currentScannerName;
    [ObservableProperty] private double _scanPercentage;
    [ObservableProperty] private string _elapsedDisplay = "00:00";
    [ObservableProperty] private int _completedScanners;
    [ObservableProperty] private int _totalScanners;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private string? _completionStatus;
    [ObservableProperty] private ScanMode _selectedScanMode;

    /// <summary>Custom-scan category toggles. Bound from the scan flyout when mode = Custom.</summary>
    public ScanCategory CustomCategories { get; set; } = ScanCategory.System | ScanCategory.Hardware;

    public event EventHandler? ScanCompleted;

    [RelayCommand(CanExecute = nameof(CanRunScan))]
    private async Task RunScanAsync()
    {
        if (IsScanning) return;
        _cts = new CancellationTokenSource();
        IsScanning = true;
        ClearError();
        CompletionStatus = null;
        ScanPercentage = 0;
        CompletedScanners = 0;
        WarningCount = 0;
        ErrorCount = 0;
        RunScanCommand.NotifyCanExecuteChanged();

        var sw = Stopwatch.StartNew();
        using var timer = new Timer(_ => UpdateElapsed(sw), null, 0, 500);

        var progress = new Progress<ScanProgress>(p => _dispatcher.TryEnqueue(() =>
        {
            CurrentScannerName = p.ScannerName;
            ScanPercentage = p.Percentage;
            CompletedScanners = p.CompletedScanners;
            TotalScanners = p.TotalScanners;
        }));

        try
        {
            var categories = SelectedScanMode == ScanMode.Custom
                ? CustomCategories
                : ScanCategory.None;
            var report = await _scanService.RunScanAsync(SelectedScanMode, categories, progress, _cts.Token);

            WarningCount = report.ReportMetadata.WarningCount;
            ErrorCount = report.ReportMetadata.ErrorCount;
            CompletionStatus = report.ReportMetadata.OverallStatus switch
            {
                ScanStatus.Success => "Scan completed successfully",
                ScanStatus.PartialSuccess => "Scan completed with some sections unavailable",
                _ => "Scan finished with errors"
            };
            ScanPercentage = 100;
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            CompletionStatus = "Scan cancelled";
            _logger.LogInformation("Scan cancelled by user");
        }
        catch (Exception ex)
        {
            SetError($"Scan failed: {ex.Message}");
            _logger.LogError(ex, "Scan failed unexpectedly");
        }
        finally
        {
            sw.Stop();
            IsScanning = false;
            CurrentScannerName = null;
            _cts?.Dispose();
            _cts = null;
            RunScanCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRunScan() => !IsScanning;

    [RelayCommand]
    private void CancelScan()
    {
        _cts?.Cancel();
        _logger.LogInformation("Cancel requested");
    }

    private void UpdateElapsed(Stopwatch sw) =>
        _dispatcher.TryEnqueue(() => ElapsedDisplay = sw.Elapsed.ToString(@"mm\:ss"));
}
