using System.IO.Compression;
using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;
using GetToKnowYourDevice.Export.Exporters;
using Xunit;

namespace GetToKnowYourDevice.Export.Tests;

public class CsvAndPdfExporterTests
{
    private static CanonicalReport Sample()
    {
        var r = new CanonicalReport();
        r.DeviceIdentity.DeviceName = "TestPC";
        r.Hardware.Processors.Add(new ProcessorInfo { Name = "CPU, with comma", PhysicalCores = 8 });
        r.Storage.Volumes.Add(new StorageVolume { DriveLetter = "C:", TotalBytes = 100, FreeBytes = 40 });
        r.Drivers.Add(new DriverInfo { DeviceName = "Driver", IsSigned = true });
        return r;
    }

    [Fact]
    public void CsvExport_ProducesZipWithExpectedEntries()
    {
        var exporter = new CsvReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(), new ExportOptions());

        using var ms = new MemoryStream(artifact.Content);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var names = zip.Entries.Select(e => e.Name).ToList();

        Assert.Contains("summary.csv", names);
        Assert.Contains("processors.csv", names);
        Assert.Contains("volumes.csv", names);
        Assert.EndsWith(".zip", artifact.FileName);
    }

    [Fact]
    public void CsvExport_IncludesDriversOnlyWhenRequested()
    {
        var exporter = new CsvReportExporter(new PrivacyMasker());

        var without = ReadEntryNames(exporter.Export(Sample(), new ExportOptions { IncludeFullDriverList = false }));
        Assert.DoesNotContain("drivers.csv", without);

        var with = ReadEntryNames(exporter.Export(Sample(), new ExportOptions { IncludeFullDriverList = true }));
        Assert.Contains("drivers.csv", with);
    }

    [Fact]
    public void CsvExport_EscapesCommaInProcessorName()
    {
        var exporter = new CsvReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(), new ExportOptions());

        using var ms = new MemoryStream(artifact.Content);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var entry = zip.GetEntry("processors.csv")!;
        using var reader = new StreamReader(entry.Open());
        var content = reader.ReadToEnd();

        // The comma-containing name must be quoted.
        Assert.Contains("\"CPU, with comma\"", content);
    }

    [Fact]
    public void PdfExport_ProducesNonEmptyPdf()
    {
        var exporter = new PdfReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(), new ExportOptions());

        Assert.EndsWith(".pdf", artifact.FileName);
        Assert.True(artifact.Content.Length > 0);
        // PDF files start with "%PDF".
        Assert.Equal((byte)'%', artifact.Content[0]);
        Assert.Equal((byte)'P', artifact.Content[1]);
    }

    [Fact]
    public void PdfExport_DoesNotThrowOnEmptyReport()
    {
        var exporter = new PdfReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(new CanonicalReport(), new ExportOptions());
        Assert.True(artifact.Content.Length > 0);
    }

    private static List<string> ReadEntryNames(ExportArtifact artifact)
    {
        using var ms = new MemoryStream(artifact.Content);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        return zip.Entries.Select(e => e.Name).ToList();
    }
}
