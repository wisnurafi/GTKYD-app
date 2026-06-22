using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;
using GetToKnowYourDevice.Export.Exporters;
using Xunit;

namespace GetToKnowYourDevice.Export.Tests;

public class JsonReportExporterTests
{
    private static CanonicalReport Sample()
    {
        var r = new CanonicalReport();
        r.DeviceIdentity.DeviceName = "TestPC";
        r.DeviceIdentity.SerialNumber = "SN-999";
        r.Hardware.Processors.Add(new ProcessorInfo { Name = "Test CPU", PhysicalCores = 4 });
        r.ReportMetadata.SchemaVersion = "1.0";
        r.ReportMetadata.ApplicationVersion = "1.2.3.4";
        return r;
    }

    [Fact]
    public void Export_ProducesValidJson()
    {
        var exporter = new JsonReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(), new ExportOptions());

        var json = System.Text.Encoding.UTF8.GetString(artifact.Content);
        // Round-trips back to a report (proves it's valid serialized canonical JSON).
        var parsed = System.Text.Json.JsonSerializer.Deserialize<CanonicalReport>(
            json, GetToKnowYourDevice.Core.Serialization.CanonicalJson.Indented);
        Assert.NotNull(parsed);
        Assert.Equal("TestPC", parsed!.DeviceIdentity.DeviceName);
    }

    [Fact]
    public void Export_PreservesSchemaAndAppVersion()
    {
        var exporter = new JsonReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(), new ExportOptions());
        var json = System.Text.Encoding.UTF8.GetString(artifact.Content);

        Assert.Contains("1.0", json);
        Assert.Contains("1.2.3.4", json);
    }

    [Fact]
    public void Export_WithMasking_HidesSerial()
    {
        var exporter = new JsonReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(),
            new ExportOptions { Masking = new MaskingOptions { MaskSerialNumbers = true } });
        var json = System.Text.Encoding.UTF8.GetString(artifact.Content);

        Assert.DoesNotContain("SN-999", json);
    }

    [Fact]
    public void Export_FileNameHasJsonExtension()
    {
        var exporter = new JsonReportExporter(new PrivacyMasker());
        var artifact = exporter.Export(Sample(), new ExportOptions());
        Assert.EndsWith(".json", artifact.FileName);
    }
}
