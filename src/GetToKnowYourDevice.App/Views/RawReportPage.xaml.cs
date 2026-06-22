using System.Runtime.Versioning;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.App.ViewModels;
using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Settings;
using GetToKnowYourDevice.Export;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace GetToKnowYourDevice.App.Views;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class RawReportPage : Page
{
    public RawReportViewModel ViewModel { get; }
    private readonly AppScanService _scanService;
    private readonly IReportExportService _exportService;
    private readonly ISettingsService _settings;

    public RawReportPage()
    {
        ViewModel = AppHost.Get<RawReportViewModel>();
        _scanService = AppHost.Get<AppScanService>();
        _exportService = AppHost.Get<IReportExportService>();
        _settings = AppHost.Get<ISettingsService>();
        InitializeComponent();
    }

    private void ViewCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ViewModel.SelectedView = ViewCombo.SelectedIndex switch
        {
            1 => RawReportView.PlainText,
            2 => RawReportView.Diagnostics,
            _ => RawReportView.Json
        };

    private async void ExportJson_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => await ExportAsync(ExportFormat.Json);
    private async void ExportCsv_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => await ExportAsync(ExportFormat.Csv);
    private async void ExportPdf_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => await ExportAsync(ExportFormat.Pdf);

    private async Task ExportAsync(ExportFormat format)
    {
        var report = _scanService.CurrentReport;
        if (report is null || App.MainWindowInstance is null) return;

        var s = _settings.Current;
        var options = new ExportOptions
        {
            Masking = s.ToMaskingOptions(),
            IncludeSerialNumbers = s.IncludeSerialNumbersInExport,
            IncludeRawReport = s.IncludeRawReportInExport,
            IncludeFullDriverList = s.IncludeFullDriverListInExport,
            CsvDelimiter = s.CsvDelimiter,
            PdfPageSize = s.PdfPageSize,
            PdfOrientation = s.PdfOrientation
        };

        try
        {
            var artifact = _exportService.Export(report, format, options);
            var saver = new FileSaveService(App.MainWindowInstance);
            var path = await saver.SaveArtifactAsync(artifact);
            await ShowDialogAsync(path is null ? "Export cancelled." : $"Exported to:\n{path}");
        }
        catch (Exception ex)
        {
            await ShowDialogAsync($"Export failed: {ex.Message}");
        }
    }

    private void CopyAll_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var pkg = new DataPackage();
        pkg.SetText(ViewModel.RenderedText);
        Clipboard.SetContent(pkg);
    }

    private async Task ShowDialogAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Export",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }
}
