using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Calculations;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>
/// Reads battery info. Combines Win32_Battery (status) with root\wmi BatteryFullChargedCapacity
/// and BatteryStaticData (design capacity) to compute health/wear. A device with no battery is a
/// normal condition reported as an empty battery list (not a failure).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class BatteryScanner(WmiQueryRunner wmi, ILogger<BatteryScanner> logger) : ScannerBase(logger)
{
    public override string Name => "Battery";
    public override ScanCategory Category => ScanCategory.Battery;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_Battery,root\\wmi:BatteryFullChargedCapacity,BatteryStaticData";

        var batteries = wmi.Query(
            "SELECT Name, DeviceID, Chemistry, EstimatedChargeRemaining, EstimatedRunTime, " +
            "DesignCapacity, FullChargeCapacity, DesignVoltage, BatteryStatus, Status FROM Win32_Battery",
            ct: ct).ToList();

        if (batteries.Count == 0)
        {
            // No battery is normal for desktops; report empty, not failure.
            builder.Warn("No battery detected on this device.");
            return Task.FromResult(true);
        }

        // root\wmi capacity tables, keyed by instance order (best-effort pairing).
        var fullCaps = SafeQuery("SELECT FullChargedCapacity FROM BatteryFullChargedCapacity", @"root\wmi", ct)
            .Select(r => r.GetUInt("FullChargedCapacity")).ToList();
        var designCaps = SafeQuery("SELECT DesignedCapacity FROM BatteryStaticData", @"root\wmi", ct)
            .Select(r => r.GetUInt("DesignedCapacity")).ToList();
        var cycleCounts = SafeQuery("SELECT CycleCount FROM BatteryCycleCount", @"root\wmi", ct)
            .Select(r => r.GetUInt("CycleCount")).ToList();

        for (var i = 0; i < batteries.Count; i++)
        {
            var r = batteries[i];
            var designMwh = r.GetUInt("DesignCapacity");
            var fullMwh = r.GetUInt("FullChargeCapacity");

            // Prefer the more accurate root\wmi values when present.
            if (i < designCaps.Count && designCaps[i] is > 0) designMwh = designCaps[i];
            if (i < fullCaps.Count && fullCaps[i] is > 0) fullMwh = fullCaps[i];

            var health = DeviceCalculations.BatteryHealthPercent((long?)fullMwh, (long?)designMwh);
            var bat = new BatteryInfo
            {
                DeviceName = r.GetString("Name"),
                BatteryId = r.GetString("DeviceID"),
                Chemistry = MapChemistry(r.GetInt("Chemistry")),
                ChargePercent = r.GetInt("EstimatedChargeRemaining"),
                EstimatedChargeRemainingMwh = (long?)r.GetUInt("EstimatedChargeRemaining"),
                DesignCapacityMwh = (long?)designMwh,
                FullChargeCapacityMwh = (long?)fullMwh,
                DesignVoltageMv = r.GetInt("DesignVoltage"),
                BatteryStatus = MapStatus(r.GetInt("BatteryStatus")),
                ChargingStatus = MapStatus(r.GetInt("BatteryStatus")),
                HealthPercent = health,
                WearPercent = DeviceCalculations.BatteryWearPercent(health),
                CycleCount = i < cycleCounts.Count ? (int?)cycleCounts[i] : null
            };

            var runtime = r.GetUInt("EstimatedRunTime");
            if (runtime is > 0 and < 71582788) bat.EstimatedRuntime = TimeSpan.FromMinutes(runtime.Value);

            if (designMwh is null or 0) builder.Unavailable("Battery.DesignCapacity",
                "design capacity unavailable; health cannot be computed");
            if (fullMwh is null) builder.Unavailable("Battery.FullChargeCapacity");

            report.Batteries.Add(bat);
        }

        return Task.FromResult(true);
    }

    private IEnumerable<WmiRow> SafeQuery(string wql, string scope, CancellationToken ct)
    {
        try { return wmi.Query(wql, scope, ct).ToList(); }
        catch { return []; }
    }

    private static string? MapChemistry(int? c) => c switch
    {
        1 => "Other", 2 => "Unknown", 3 => "Lead Acid", 4 => "Nickel Cadmium",
        5 => "Nickel Metal Hydride", 6 => "Lithium-ion", 7 => "Zinc Air",
        8 => "Lithium Polymer", null => null, _ => $"Chem {c}"
    };

    private static string? MapStatus(int? c) => c switch
    {
        1 => "Discharging", 2 => "AC Power", 3 => "Fully Charged", 4 => "Low",
        5 => "Critical", 6 => "Charging", 7 => "Charging and High",
        8 => "Charging and Low", 9 => "Charging and Critical", 10 => "Undefined",
        11 => "Partially Charged", null => null, _ => $"Status {c}"
    };
}
