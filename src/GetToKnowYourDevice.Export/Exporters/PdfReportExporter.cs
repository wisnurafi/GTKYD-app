using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GetToKnowYourDevice.Export.Exporters;

/// <summary>
/// Exports the report as a structured PDF using QuestPDF (Community license). Masking is applied
/// before rendering. Handles missing properties gracefully (shows "Unavailable"), paginates long
/// tables, and stamps every page with a number and generation timestamp.
/// </summary>
public sealed class PdfReportExporter : IReportExporter
{
    private readonly IPrivacyMasker _masker;

    static PdfReportExporter()
    {
        // QuestPDF Community license: free for individuals and companies under the revenue
        // threshold. Documented in README third-party licenses.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PdfReportExporter(IPrivacyMasker masker) => _masker = masker;

    public ExportFormat Format => ExportFormat.Pdf;

    public ExportArtifact Export(CanonicalReport report, ExportOptions options)
    {
        var r = _masker.Mask(report, options.Masking);
        var generatedAt = DateTimeOffset.Now;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                var baseSize = options.PdfPageSize == PdfPageSize.Letter ? PageSizes.Letter : PageSizes.A4;
                page.Size(baseSize.Let(options.PdfOrientation));
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(h => ComposeHeader(h, r));
                page.Content().Element(c => ComposeContent(c, r, options));
                page.Footer().Element(f => ComposeFooter(f, generatedAt));
            });
        }).GeneratePdf();

        return new ExportArtifact
        {
            FileName = ExportFileNaming.BuildFileName(r.DeviceIdentity.DeviceName, generatedAt, "pdf"),
            Content = bytes,
            ContentType = "application/pdf"
        };
    }

    private static void ComposeHeader(IContainer container, CanonicalReport r)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Get To Know Your Device").Bold().FontSize(16);
                col.Item().Text(r.DeviceIdentity.DeviceName ?? "Unknown device").FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(120).AlignRight().Text($"Health: {r.Health.Score}/100")
                .Bold().FontSize(12);
        });
    }

    private static void ComposeFooter(IContainer container, DateTimeOffset generatedAt)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Generated {generatedAt:yyyy-MM-dd HH:mm:ss}")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(120).AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private static void ComposeContent(IContainer container, CanonicalReport r, ExportOptions options)
    {
        container.PaddingVertical(8).Column(col =>
        {
            col.Spacing(12);

            Section(col, "Report Metadata", t =>
            {
                Kv(t, "Scan Mode", r.ReportMetadata.ScanMode.ToString());
                Kv(t, "Scan Status", r.ReportMetadata.OverallStatus.ToString());
                Kv(t, "Started", r.ReportMetadata.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                Kv(t, "Duration", $"{r.ReportMetadata.Duration.TotalSeconds:0.0}s");
                Kv(t, "Schema Version", r.ReportMetadata.SchemaVersion);
                Kv(t, "App Version", r.ReportMetadata.ApplicationVersion);
                Kv(t, "Masking Applied", r.ReportMetadata.MaskingApplied ? "Yes" : "No");
            });

            Section(col, "Device Summary", t =>
            {
                Kv(t, "Device Name", V(r.DeviceIdentity.DeviceName));
                Kv(t, "Manufacturer", V(r.DeviceIdentity.Manufacturer));
                Kv(t, "Model", V(r.DeviceIdentity.Model));
                Kv(t, "Serial Number", V(r.DeviceIdentity.SerialNumber));
                Kv(t, "Virtual Machine", r.DeviceIdentity.IsVirtualMachine?.ToString() ?? "Unavailable");
            });

            Section(col, "Operating System", t =>
            {
                Kv(t, "Edition", V(r.OperatingSystem.Edition));
                Kv(t, "Version", V(r.OperatingSystem.Version));
                Kv(t, "Build", V(r.OperatingSystem.BuildNumber));
                Kv(t, "Display Version", V(r.OperatingSystem.DisplayVersion));
                Kv(t, "Architecture", V(r.OperatingSystem.Architecture));
            });

            ComposeHealth(col, r);
            ComposeHardware(col, r);
            ComposeStorage(col, r);
            ComposeBattery(col, r);
            ComposeSecurity(col, r);
            ComposeNetwork(col, r);

            if (options.PdfDetail == PdfDetailLevel.Full && options.IncludeFullDriverList)
                ComposeDrivers(col, r);

            ComposeDiagnostics(col, r);
        });
    }

    private static void ComposeHealth(ColumnDescriptor col, CanonicalReport r)
    {
        Section(col, $"Health Score: {r.Health.Score}/100", t =>
        {
            if (r.Health.Findings.Count == 0)
            {
                Kv(t, "Findings", "No issues detected");
                return;
            }
            foreach (var f in r.Health.Findings)
                Kv(t, $"[{f.Severity}] {f.RuleId}", $"{f.Reason} (-{f.Deduction})");
        });
    }

    private static void ComposeHardware(ColumnDescriptor col, CanonicalReport r)
    {
        Section(col, "Hardware", t =>
        {
            foreach (var p in r.Hardware.Processors)
                Kv(t, "CPU", $"{V(p.Name)} ({p.PhysicalCores}C/{p.LogicalProcessors}T)");
            Kv(t, "Installed RAM", Bytes(r.Hardware.MemorySummary.InstalledBytes));
            foreach (var g in r.Hardware.GraphicsAdapters)
                Kv(t, "GPU", V(g.Name));
        });
    }

    private static void ComposeStorage(ColumnDescriptor col, CanonicalReport r)
    {
        if (r.Storage.Volumes.Count == 0) return;
        col.Item().Column(c =>
        {
            c.Item().PaddingBottom(4).Text("Storage Volumes").Bold().FontSize(11);
            c.Item().Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(1);
                    cd.RelativeColumn(1); cd.RelativeColumn(1);
                });
                HeaderCells(table, "Drive", "Label", "FS", "Total", "Free");
                foreach (var v in r.Storage.Volumes)
                {
                    Cell(table, V(v.DriveLetter));
                    Cell(table, V(v.Label));
                    Cell(table, V(v.FileSystem));
                    Cell(table, Bytes(v.TotalBytes));
                    Cell(table, Bytes(v.FreeBytes));
                }
            });
        });
    }

    private static void ComposeBattery(ColumnDescriptor col, CanonicalReport r)
    {
        Section(col, "Battery", t =>
        {
            if (r.Batteries.Count == 0)
            {
                Kv(t, "Status", "No battery detected on this device");
                return;
            }
            foreach (var b in r.Batteries)
            {
                Kv(t, "Battery", V(b.DeviceName));
                Kv(t, "  Health", b.HealthPercent is { } h ? $"{h:0.#}%" : "Unavailable");
                Kv(t, "  Cycle Count", b.CycleCount?.ToString() ?? "Unavailable");
            }
        });
    }

    private static void ComposeSecurity(ColumnDescriptor col, CanonicalReport r)
    {
        Section(col, "Security", t =>
        {
            Kv(t, "Secure Boot", r.Security.Device.SecureBootEnabled?.ToString() ?? "Unavailable");
            Kv(t, "TPM Detected", r.Security.Device.TpmDetected?.ToString() ?? "Unavailable");
            Kv(t, "TPM Version", V(r.Security.Device.TpmVersion));
            Kv(t, "UAC Enabled", r.Security.Device.UacEnabled?.ToString() ?? "Unavailable");
            foreach (var av in r.Security.AntivirusProducts)
                Kv(t, "Antivirus", $"{V(av.ProductName)} (Enabled: {av.IsEnabled?.ToString() ?? "?"})");
        });
    }

    private static void ComposeNetwork(ColumnDescriptor col, CanonicalReport r)
    {
        Section(col, "Network", t =>
        {
            Kv(t, "Active Interface", V(r.Network.Summary.ActiveInterface));
            Kv(t, "Local IPv4", V(r.Network.Summary.LocalIPv4));
            Kv(t, "Default Gateway", V(r.Network.Summary.DefaultGateway));
            Kv(t, "Adapters", r.Network.Adapters.Count.ToString());
        });
    }

    private static void ComposeDrivers(ColumnDescriptor col, CanonicalReport r)
    {
        if (r.Drivers.Count == 0) return;
        col.Item().Column(c =>
        {
            c.Item().PaddingBottom(4).Text($"Drivers ({r.Drivers.Count})").Bold().FontSize(11);
            c.Item().Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3); cd.RelativeColumn(2); cd.RelativeColumn(1); cd.RelativeColumn(1);
                });
                HeaderCells(table, "Device", "Provider", "Version", "Signed");
                foreach (var dr in r.Drivers)
                {
                    Cell(table, V(dr.DeviceName));
                    Cell(table, V(dr.DriverProvider));
                    Cell(table, V(dr.DriverVersion));
                    Cell(table, dr.IsSigned?.ToString() ?? "?");
                }
            });
        });
    }

    private static void ComposeDiagnostics(ColumnDescriptor col, CanonicalReport r)
    {
        Section(col, "Scan Diagnostics", t =>
        {
            foreach (var diag in r.ScanDiagnostics)
                Kv(t, diag.ScannerName, $"{diag.Status} ({diag.DurationMs:0}ms)" +
                    (diag.RequiresElevation ? " [needs admin]" : ""));
        });
    }

    // --- helpers ---

    private static void Section(ColumnDescriptor col, string title, Action<ColumnDescriptor> body)
    {
        col.Item().Column(c =>
        {
            c.Item().PaddingBottom(4).Text(title).Bold().FontSize(11);
            c.Item().Column(body);
        });
    }

    private static void Kv(ColumnDescriptor col, string key, string? value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(160).Text(key).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().Text(value ?? "Unavailable");
        });
    }

    private static void HeaderCells(TableDescriptor table, params string[] headers)
    {
        foreach (var h in headers)
            table.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).Bold();
    }

    private static void Cell(TableDescriptor table, string? value)
        => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
            .Text(value ?? "Unavailable");

    private static string V(string? value) => string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;

    private static string Bytes(long? bytes)
    {
        if (bytes is null or 0) return "Unavailable";
        double b = bytes.Value;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var i = 0;
        while (b >= 1024 && i < units.Length - 1) { b /= 1024; i++; }
        return $"{b:0.##} {units[i]}";
    }
}

/// <summary>Small helper to apply page orientation fluently.</summary>
internal static class PageSizeExtensions
{
    public static PageSize Let(this PageSize size, PdfOrientation orientation)
        => orientation == PdfOrientation.Landscape ? size.Landscape() : size.Portrait();
}
