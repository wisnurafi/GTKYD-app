using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;

namespace GetToKnowYourDevice.Core.Export;

public enum ExportFormat { Json, Csv, Pdf }

public enum PdfPageSize { A4, Letter }
public enum PdfOrientation { Portrait, Landscape }
public enum PdfDetailLevel { Summary, Full }

/// <summary>Options controlling an export operation. Masking is applied before bytes are written.</summary>
public sealed class ExportOptions
{
    public MaskingOptions Masking { get; set; } = new();
    public bool IncludeSerialNumbers { get; set; } = true;
    public bool IncludeRawReport { get; set; } = true;
    public bool IncludeFullDriverList { get; set; }

    // CSV
    public string CsvDelimiter { get; set; } = ",";

    // PDF
    public PdfDetailLevel PdfDetail { get; set; } = PdfDetailLevel.Summary;
    public PdfPageSize PdfPageSize { get; set; } = PdfPageSize.A4;
    public PdfOrientation PdfOrientation { get; set; } = PdfOrientation.Portrait;
    public bool IncludeRawIdentifiers { get; set; } = true;
}

/// <summary>Result of an export: bytes plus the suggested file name and content type.</summary>
public sealed class ExportArtifact
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
}

/// <summary>Serializes a canonical report to a single format.</summary>
public interface IReportExporter
{
    ExportFormat Format { get; }
    ExportArtifact Export(CanonicalReport report, ExportOptions options);
}
