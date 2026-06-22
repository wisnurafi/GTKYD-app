using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.Export;

/// <summary>Picks the right exporter for a format and produces the artifact.</summary>
public interface IReportExportService
{
    ExportArtifact Export(CanonicalReport report, ExportFormat format, ExportOptions options);
    IReadOnlyList<ExportFormat> SupportedFormats { get; }
}

public sealed class ReportExportService : IReportExportService
{
    private readonly Dictionary<ExportFormat, IReportExporter> _exporters;

    public ReportExportService(IEnumerable<IReportExporter> exporters)
        => _exporters = exporters.ToDictionary(e => e.Format);

    public IReadOnlyList<ExportFormat> SupportedFormats => _exporters.Keys.ToList();

    public ExportArtifact Export(CanonicalReport report, ExportFormat format, ExportOptions options)
    {
        if (!_exporters.TryGetValue(format, out var exporter))
            throw new NotSupportedException($"No exporter registered for {format}.");
        return exporter.Export(report, options);
    }
}
