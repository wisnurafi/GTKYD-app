namespace GetToKnowYourDevice.Core.Calculations;

/// <summary>Pure, testable calculations shared across scanners, health rules, and UI.</summary>
public static class DeviceCalculations
{
    /// <summary>
    /// Battery health = FullChargeCapacity / DesignCapacity * 100.
    /// Returns null when design capacity is missing or zero (avoids divide-by-zero
    /// and never reports a fake value).
    /// </summary>
    public static double? BatteryHealthPercent(long? fullChargeCapacity, long? designCapacity)
    {
        if (designCapacity is null or 0) return null;
        if (fullChargeCapacity is null) return null;
        var pct = (double)fullChargeCapacity.Value / designCapacity.Value * 100.0;
        return Math.Round(pct, 1);
    }

    /// <summary>Battery wear = 100 - health. Null when health is not computable.</summary>
    public static double? BatteryWearPercent(double? healthPercent)
        => healthPercent is null ? null : Math.Round(100.0 - healthPercent.Value, 1);

    /// <summary>
    /// Storage usage percentage. Returns null when total is missing or zero so the UI
    /// shows "Unavailable" rather than 0% or NaN.
    /// </summary>
    public static double? StorageUsagePercent(long? usedBytes, long? totalBytes)
    {
        if (totalBytes is null or 0) return null;
        if (usedBytes is null) return null;
        var pct = (double)usedBytes.Value / totalBytes.Value * 100.0;
        return Math.Round(Math.Clamp(pct, 0, 100), 1);
    }

    /// <summary>Free percentage from total and free. Null when total missing/zero.</summary>
    public static double? StorageFreePercent(long? freeBytes, long? totalBytes)
    {
        if (totalBytes is null or 0) return null;
        if (freeBytes is null) return null;
        var pct = (double)freeBytes.Value / totalBytes.Value * 100.0;
        return Math.Round(Math.Clamp(pct, 0, 100), 1);
    }
}
