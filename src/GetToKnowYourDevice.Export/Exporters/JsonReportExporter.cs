using System.Text;
using GetToKnowYourDevice.Core.Export;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;
using GetToKnowYourDevice.Core.Serialization;

namespace GetToKnowYourDevice.Export.Exporters;

/// <summary>
/// Exports the canonical report as indented JSON. Masking is applied to a copy before
/// serialization so the bytes written already respect privacy settings. Preserves the nested
/// structure plus schema and application version from the report metadata.
/// </summary>
public sealed class JsonReportExporter(IPrivacyMasker masker) : IReportExporter
{
    public ExportFormat Format => ExportFormat.Json;

    public ExportArtifact Export(CanonicalReport report, ExportOptions options)
    {
        var toWrite = masker.Mask(report, options.Masking);
        var json = System.Text.Json.JsonSerializer.Serialize(toWrite, CanonicalJson.Indented);

        return new ExportArtifact
        {
            FileName = ExportFileNaming.BuildFileName(
                toWrite.DeviceIdentity.DeviceName, DateTimeOffset.Now, "json"),
            Content = Encoding.UTF8.GetBytes(json),
            ContentType = "application/json"
        };
    }
}
