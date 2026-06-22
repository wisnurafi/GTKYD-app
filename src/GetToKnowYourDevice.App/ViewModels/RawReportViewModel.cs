using CommunityToolkit.Mvvm.ComponentModel;
using GetToKnowYourDevice.App.Services;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Serialization;

namespace GetToKnowYourDevice.App.ViewModels;

public enum RawReportView { Json, PlainText, Diagnostics }

/// <summary>
/// Backs the Raw Report page. Renders the single canonical report as pretty JSON, plain text, or a
/// diagnostics list. Search filters the rendered text. The report itself is the same canonical model
/// used by history, comparison, and export (no per-view data model).
/// </summary>
public sealed partial class RawReportViewModel(AppScanService scanService) : ReportSectionViewModel(scanService)
{
    private CanonicalReport? _report;

    [ObservableProperty] private RawReportView _selectedView = RawReportView.Json;
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private string _renderedText = "";
    [ObservableProperty] private bool _showUnavailable = true;

    partial void OnSelectedViewChanged(RawReportView value) => Render();
    partial void OnSearchTextChanged(string? value) => Render();

    protected override void Project(CanonicalReport r)
    {
        _report = r;
        Render();
    }

    protected override void Clear()
    {
        _report = null;
        RenderedText = "";
    }

    private void Render()
    {
        if (_report is null) { RenderedText = ""; return; }

        var text = SelectedView switch
        {
            RawReportView.Json => System.Text.Json.JsonSerializer.Serialize(_report, CanonicalJson.Indented),
            RawReportView.PlainText => RenderPlainText(_report),
            RawReportView.Diagnostics => RenderDiagnostics(_report),
            _ => ""
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            var lines = text.Split('\n')
                .Where(l => l.Contains(s, StringComparison.OrdinalIgnoreCase));
            text = string.Join('\n', lines);
        }

        RenderedText = text;
    }

    private static string RenderPlainText(CanonicalReport r)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Device: {Format.OrUnavailable(r.DeviceIdentity.DeviceName)}");
        sb.AppendLine($"OS: {Format.OrUnavailable(r.OperatingSystem.Edition)} {r.OperatingSystem.Version}");
        sb.AppendLine($"Health: {r.Health.Score}/100");
        sb.AppendLine($"Scan: {r.ReportMetadata.ScanMode} ({r.ReportMetadata.OverallStatus})");
        sb.AppendLine($"Started: {r.ReportMetadata.StartedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Warnings: {r.ReportMetadata.WarningCount}, Errors: {r.ReportMetadata.ErrorCount}");
        return sb.ToString();
    }

    private static string RenderDiagnostics(CanonicalReport r)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var d in r.ScanDiagnostics)
        {
            sb.AppendLine($"[{d.Status}] {d.ScannerName} ({d.DurationMs:0}ms) source={d.Source}");
            foreach (var w in d.Warnings) sb.AppendLine($"    warning: {w}");
            foreach (var e in d.Errors) sb.AppendLine($"    error: {e}");
            foreach (var u in d.PropertiesUnavailable) sb.AppendLine($"    unavailable: {u}");
        }
        return sb.ToString();
    }
}
