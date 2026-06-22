namespace GetToKnowYourDevice.Core.Privacy;

/// <summary>Which identifier categories to mask. Mirrors export/privacy settings.</summary>
public sealed class MaskingOptions
{
    public bool MaskUsername { get; set; }
    public bool MaskDeviceName { get; set; }
    public bool MaskSerialNumbers { get; set; }
    public bool MaskMacAddress { get; set; }
    public bool MaskBssid { get; set; }
    public bool MaskUuid { get; set; }
    public bool MaskIpAddress { get; set; }

    /// <summary>Convenience: turn everything on (used by "mask device identifiers").</summary>
    public static MaskingOptions All() => new()
    {
        MaskUsername = true,
        MaskDeviceName = true,
        MaskSerialNumbers = true,
        MaskMacAddress = true,
        MaskBssid = true,
        MaskUuid = true,
        MaskIpAddress = true
    };

    public bool AnyEnabled =>
        MaskUsername || MaskDeviceName || MaskSerialNumbers || MaskMacAddress ||
        MaskBssid || MaskUuid || MaskIpAddress;
}

/// <summary>
/// Masks sensitive identifiers in a canonical report. Applied to a deep copy before export
/// so the in-memory report and stored snapshot keep their real values.
/// </summary>
public interface IPrivacyMasker
{
    /// <summary>Returns a masked deep copy of the report; original is not mutated.</summary>
    Models.CanonicalReport Mask(Models.CanonicalReport report, MaskingOptions options);

    /// <summary>Masks a single string identifier (keeps a short prefix, hides the rest).</summary>
    string? MaskValue(string? value);
}
