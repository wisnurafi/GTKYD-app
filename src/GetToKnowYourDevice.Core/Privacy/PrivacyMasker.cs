using System.Text.Json;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.Core.Privacy;

/// <summary>
/// Default masker. Masks identifiers on a deep copy of the report so the live report and
/// the immutable stored snapshot keep their true values. Masking is applied before any
/// export bytes are written.
/// </summary>
public sealed class PrivacyMasker : IPrivacyMasker
{
    public CanonicalReport Mask(CanonicalReport report, MaskingOptions options)
    {
        var copy = DeepCopy(report);
        if (!options.AnyEnabled)
        {
            copy.ReportMetadata.MaskingApplied = false;
            return copy;
        }

        if (options.MaskDeviceName)
        {
            copy.DeviceIdentity.DeviceName = MaskValue(copy.DeviceIdentity.DeviceName);
            copy.ReportMetadata.ScanName = copy.ReportMetadata.ScanName; // unchanged
        }
        if (options.MaskUsername)
            copy.DeviceIdentity.CurrentUsername = MaskValue(copy.DeviceIdentity.CurrentUsername);
        if (options.MaskUuid)
        {
            copy.DeviceIdentity.Uuid = MaskValue(copy.DeviceIdentity.Uuid);
            copy.Hardware.System.Uuid = MaskValue(copy.Hardware.System.Uuid);
        }
        if (options.MaskSerialNumbers)
        {
            copy.DeviceIdentity.SerialNumber = MaskValue(copy.DeviceIdentity.SerialNumber);
            copy.Hardware.System.SerialNumber = MaskValue(copy.Hardware.System.SerialNumber);
            copy.Hardware.Motherboard.SerialNumber = MaskValue(copy.Hardware.Motherboard.SerialNumber);
            copy.Hardware.Bios.SerialNumber = MaskValue(copy.Hardware.Bios.SerialNumber);
            foreach (var m in copy.Hardware.MemoryModules) m.SerialNumber = MaskValue(m.SerialNumber);
            foreach (var d in copy.Storage.PhysicalDisks) d.SerialNumber = MaskValue(d.SerialNumber);
            foreach (var b in copy.Batteries) b.SerialNumber = MaskValue(b.SerialNumber);
        }
        if (options.MaskMacAddress)
            foreach (var a in copy.Network.Adapters) a.MacAddress = MaskValue(a.MacAddress);
        if (options.MaskBssid && copy.Network.Wifi is not null)
            copy.Network.Wifi.Bssid = MaskValue(copy.Network.Wifi.Bssid);
        if (options.MaskIpAddress)
        {
            copy.Network.Summary.LocalIPv4 = MaskValue(copy.Network.Summary.LocalIPv4);
            copy.Network.Summary.LocalIPv6 = MaskValue(copy.Network.Summary.LocalIPv6);
            foreach (var a in copy.Network.Adapters)
            {
                a.IPv4Addresses = a.IPv4Addresses.Select(MaskValue).Where(s => s != null).Select(s => s!).ToList();
                a.IPv6Addresses = a.IPv6Addresses.Select(MaskValue).Where(s => s != null).Select(s => s!).ToList();
            }
        }

        copy.ReportMetadata.MaskingApplied = true;
        return copy;
    }

    /// <summary>
    /// Masks a value by keeping a short visible prefix and replacing the rest with asterisks.
    /// Empty/null in, null out. Short values are fully masked.
    /// </summary>
    public string? MaskValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var trimmed = value.Trim();
        if (trimmed.Length <= 2) return new string('*', trimmed.Length);
        var visible = Math.Min(2, trimmed.Length - 1);
        return string.Concat(trimmed.AsSpan(0, visible), new string('*', trimmed.Length - visible));
    }

    private static CanonicalReport DeepCopy(CanonicalReport report)
    {
        var json = JsonSerializer.Serialize(report);
        return JsonSerializer.Deserialize<CanonicalReport>(json)
            ?? throw new InvalidOperationException("Failed to deep-copy report for masking.");
    }
}
