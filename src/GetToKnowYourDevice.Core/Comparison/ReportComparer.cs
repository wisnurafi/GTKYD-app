using System.Text.Json;
using System.Text.Json.Nodes;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.Core.Comparison;

/// <summary>
/// Compares two canonical reports by flattening each to a map of property-path -> value
/// (via JSON), then diffing. Volatile paths are skipped unless explicitly included.
/// Keeping a single flatten-based comparer avoids per-section comparison code drift.
/// </summary>
public sealed class ReportComparer
{
    private readonly ComparisonPolicy _policy;

    public ReportComparer(ComparisonPolicy? policy = null)
        => _policy = policy ?? new ComparisonPolicy();

    public ComparisonResult Compare(CanonicalReport baseline, CanonicalReport comparison,
        bool includeVolatile = false)
    {
        var baseMap = Flatten(baseline);
        var compMap = Flatten(comparison);
        var result = new ComparisonResult();

        var allPaths = new HashSet<string>(baseMap.Keys, StringComparer.Ordinal);
        allPaths.UnionWith(compMap.Keys);

        foreach (var path in allPaths.OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!includeVolatile && _policy.IsVolatile(path)) continue;

            var hasBase = baseMap.TryGetValue(path, out var bVal);
            var hasComp = compMap.TryGetValue(path, out var cVal);

            if (hasBase && !hasComp)
            {
                result.Differences.Add(new ReportDifference
                {
                    PropertyPath = path, Section = SectionOf(path),
                    BaselineValue = bVal, ComparisonValue = null,
                    Kind = ChangeKind.Removed, Description = $"{path} removed."
                });
            }
            else if (!hasBase && hasComp)
            {
                result.Differences.Add(new ReportDifference
                {
                    PropertyPath = path, Section = SectionOf(path),
                    BaselineValue = null, ComparisonValue = cVal,
                    Kind = ChangeKind.Added, Description = $"{path} added."
                });
            }
            else if (hasBase && hasComp && !string.Equals(bVal, cVal, StringComparison.Ordinal))
            {
                result.Differences.Add(new ReportDifference
                {
                    PropertyPath = path, Section = SectionOf(path),
                    BaselineValue = bVal, ComparisonValue = cVal,
                    Kind = ClassifyChange(path, bVal, cVal),
                    Description = $"{path} changed from '{bVal}' to '{cVal}'."
                });
            }
        }

        return result;
    }

    private static string SectionOf(string path)
    {
        var idx = path.IndexOf('.');
        return idx < 0 ? path : path[..idx];
    }

    /// <summary>
    /// Best-effort semantic classification. Numeric increases in capacity-like fields read
    /// as Improved; security regressions read as Critical. Falls back to Changed.
    /// </summary>
    private static ChangeKind ClassifyChange(string path, string? bVal, string? cVal)
    {
        if (path.Contains("SecureBootEnabled", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("RealTimeProtectionEnabled", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("IsEnabled", StringComparison.OrdinalIgnoreCase))
        {
            var wasOn = string.Equals(bVal, "true", StringComparison.OrdinalIgnoreCase);
            var nowOn = string.Equals(cVal, "true", StringComparison.OrdinalIgnoreCase);
            if (wasOn && !nowOn) return ChangeKind.Critical;
            if (!wasOn && nowOn) return ChangeKind.Improved;
        }

        if (double.TryParse(bVal, out var b) && double.TryParse(cVal, out var c))
        {
            var capacityLike = path.Contains("Capacity", StringComparison.OrdinalIgnoreCase) ||
                               path.Contains("InstalledBytes", StringComparison.OrdinalIgnoreCase) ||
                               path.Contains("TotalBytes", StringComparison.OrdinalIgnoreCase);
            if (capacityLike) return c > b ? ChangeKind.Improved : ChangeKind.Warning;

            if (path.Contains("HealthPercent", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("FreeBytes", StringComparison.OrdinalIgnoreCase))
                return c < b ? ChangeKind.Warning : ChangeKind.Improved;
        }

        return ChangeKind.Changed;
    }

    /// <summary>Flattens a report into dotted property paths. Array items use [index].</summary>
    public static Dictionary<string, string> Flatten(CanonicalReport report)
    {
        var node = JsonSerializer.SerializeToNode(report) ?? new JsonObject();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        Walk(node, "", map);
        return map;
    }

    private static void Walk(JsonNode? node, string prefix, Dictionary<string, string> map)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var kv in obj)
                {
                    var path = prefix.Length == 0 ? kv.Key : $"{prefix}.{kv.Key}";
                    Walk(kv.Value, path, map);
                }
                break;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                    Walk(arr[i], $"{prefix}[{i}]", map);
                break;
            case JsonValue val:
                map[prefix] = val.ToString();
                break;
        }
    }
}
