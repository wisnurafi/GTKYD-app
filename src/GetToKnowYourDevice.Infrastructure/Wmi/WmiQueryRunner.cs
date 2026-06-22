using System.Globalization;
using System.Management;
using System.Runtime.Versioning;

namespace GetToKnowYourDevice.Infrastructure.Wmi;

/// <summary>
/// Thin wrapper over System.Management for safe, cancellable WMI/CIM queries with typed
/// property extraction. Kept tiny so scanners stay focused on mapping, not boilerplate.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WmiQueryRunner
{
    /// <summary>
    /// Runs a WQL query against a namespace and yields each ManagementObject's property bag.
    /// Honors cancellation between rows. Disposes objects as it goes.
    /// </summary>
    public IEnumerable<WmiRow> Query(string wql, string scope = @"root\cimv2",
        CancellationToken ct = default)
    {
        using var searcher = new ManagementObjectSearcher(
            new ManagementScope(scope),
            new ObjectQuery(wql),
            new System.Management.EnumerationOptions { ReturnImmediately = true, Rewindable = false });

        using var collection = searcher.Get();
        foreach (ManagementBaseObject mo in collection)
        {
            ct.ThrowIfCancellationRequested();
            using (mo)
            {
                yield return new WmiRow(mo);
            }
        }
    }

    public WmiRow? QuerySingle(string wql, string scope = @"root\cimv2", CancellationToken ct = default)
    {
        foreach (var row in Query(wql, scope, ct))
            return row;
        return null;
    }
}

/// <summary>Wraps a single WMI result row with null-safe typed getters.</summary>
[SupportedOSPlatform("windows")]
public readonly struct WmiRow(ManagementBaseObject obj)
{
    private object? Get(string name)
    {
        try { return obj[name]; }
        catch { return null; }
    }

    public string? GetString(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        var s = v is string[] arr ? string.Join(", ", arr) : v.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    public string[]? GetStringArray(string name) => Get(name) as string[];

    public int? GetInt(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    public long? GetLong(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        try { return Convert.ToInt64(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    public uint? GetUInt(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        try { return Convert.ToUInt32(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    public ulong? GetULong(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        try { return Convert.ToUInt64(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    public bool? GetBool(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        try { return Convert.ToBoolean(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    public ushort? GetUShort(string name)
    {
        var v = Get(name);
        if (v is null) return null;
        try { return Convert.ToUInt16(v, CultureInfo.InvariantCulture); }
        catch { return null; }
    }

    /// <summary>Parses a WMI CIM_DATETIME string (yyyyMMddHHmmss.ffffff+UTC) to DateTimeOffset.</summary>
    public DateTimeOffset? GetDateTime(string name)
    {
        var raw = GetString(name);
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 14) return null;
        try
        {
            return ManagementDateTimeConverter.ToDateTime(raw);
        }
        catch { return null; }
    }
}
