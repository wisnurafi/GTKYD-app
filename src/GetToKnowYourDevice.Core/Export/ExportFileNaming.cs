using System.Text;

namespace GetToKnowYourDevice.Core.Export;

/// <summary>Builds safe, consistent export file names and sanitizes device names.</summary>
public static class ExportFileNaming
{
    private static readonly char[] InvalidExtra = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>
    /// Removes characters illegal in Windows file names plus a few extras, collapses
    /// whitespace to underscores, and trims. Empty input yields "Device".
    /// </summary>
    public static string SanitizeDeviceName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Device";

        var invalid = Path.GetInvalidFileNameChars().Concat(InvalidExtra).ToHashSet();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name.Trim())
        {
            if (invalid.Contains(ch) || char.IsControl(ch)) sb.Append('_');
            else if (char.IsWhiteSpace(ch)) sb.Append('_');
            else sb.Append(ch);
        }

        var result = sb.ToString().Trim('_', '.', ' ');
        while (result.Contains("__")) result = result.Replace("__", "_");
        return result.Length == 0 ? "Device" : result;
    }

    /// <summary>
    /// GetToKnowYourDevice_DEVICE_YYYY-MM-DD_HH-mm-ss.ext
    /// </summary>
    public static string BuildFileName(string? deviceName, DateTimeOffset timestamp, string extension)
    {
        var device = SanitizeDeviceName(deviceName);
        var stamp = timestamp.ToString("yyyy-MM-dd_HH-mm-ss");
        var ext = extension.TrimStart('.');
        return $"GetToKnowYourDevice_{device}_{stamp}.{ext}";
    }
}
