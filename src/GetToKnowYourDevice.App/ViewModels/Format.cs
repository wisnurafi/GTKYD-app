namespace GetToKnowYourDevice.App.ViewModels;

/// <summary>Small display formatting helpers shared across view models.</summary>
public static class Format
{
    /// <summary>Human-readable byte size. Null/zero -> "Unavailable".</summary>
    public static string Bytes(long? bytes)
    {
        if (bytes is null or 0) return "Unavailable";
        double b = bytes.Value;
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        var i = 0;
        while (b >= 1024 && i < units.Length - 1) { b /= 1024; i++; }
        return $"{b:0.##} {units[i]}";
    }

    /// <summary>bps -> readable link speed.</summary>
    public static string Bps(long? bps)
    {
        if (bps is null or 0) return "Unavailable";
        double v = bps.Value;
        string[] units = ["bps", "Kbps", "Mbps", "Gbps"];
        var i = 0;
        while (v >= 1000 && i < units.Length - 1) { v /= 1000; i++; }
        return $"{v:0.#} {units[i]}";
    }

    public static string OrUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;
}
