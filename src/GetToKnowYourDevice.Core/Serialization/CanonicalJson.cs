using System.Text.Json;
using System.Text.Json.Serialization;

namespace GetToKnowYourDevice.Core.Serialization;

/// <summary>Shared JSON options so UI, persistence, and export all serialize identically.</summary>
public static class CanonicalJson
{
    public static JsonSerializerOptions Indented { get; } = Build(true);
    public static JsonSerializerOptions Compact { get; } = Build(false);

    private static JsonSerializerOptions Build(bool indented) => new()
    {
        WriteIndented = indented,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
