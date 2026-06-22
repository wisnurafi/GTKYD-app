using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Calculations;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>Reads memory modules (Win32_PhysicalMemory) and a usage summary (Win32_OperatingSystem).</summary>
[SupportedOSPlatform("windows")]
public sealed class MemoryScanner(WmiQueryRunner wmi, ILogger<MemoryScanner> logger) : ScannerBase(logger)
{
    public override string Name => "Memory";
    public override ScanCategory Category => ScanCategory.Hardware;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_PhysicalMemory,Win32_OperatingSystem";
        var collected = false;
        var summary = report.Hardware.MemorySummary;

        foreach (var r in wmi.Query(
            "SELECT Manufacturer, PartNumber, SerialNumber, Capacity, Speed, ConfiguredClockSpeed, " +
            "SMBIOSMemoryType, MemoryType, FormFactor, BankLabel, DeviceLocator, ConfiguredVoltage, " +
            "MinVoltage, MaxVoltage, DataWidth, TotalWidth, TypeDetail FROM Win32_PhysicalMemory", ct: ct))
        {
            var m = new MemoryModule
            {
                Manufacturer = r.GetString("Manufacturer")?.Trim(),
                PartNumber = r.GetString("PartNumber")?.Trim(),
                SerialNumber = r.GetString("SerialNumber")?.Trim(),
                CapacityBytes = (long?)r.GetULong("Capacity"),
                SpeedMhz = r.GetInt("Speed"),
                ConfiguredSpeedMhz = r.GetInt("ConfiguredClockSpeed"),
                SmbiosMemoryType = r.GetInt("SMBIOSMemoryType"),
                MemoryType = MapMemoryType(r.GetInt("SMBIOSMemoryType")),
                FormFactor = MapFormFactor(r.GetInt("FormFactor")),
                BankLabel = r.GetString("BankLabel"),
                DeviceLocator = r.GetString("DeviceLocator"),
                VoltageMv = r.GetInt("ConfiguredVoltage"),
                MinVoltageMv = r.GetInt("MinVoltage"),
                MaxVoltageMv = r.GetInt("MaxVoltage"),
                DataWidth = r.GetInt("DataWidth"),
                TotalWidth = r.GetInt("TotalWidth"),
                TypeDetail = r.GetInt("TypeDetail")?.ToString(),
                Status = "OK"
            };
            report.Hardware.MemoryModules.Add(m);
            collected = true;
        }

        summary.UsedSlots = report.Hardware.MemoryModules.Count;

        // Physical memory array gives slot count + max capacity.
        var arr = wmi.QuerySingle(
            "SELECT MemoryDevices, MaxCapacityEx, MaxCapacity FROM Win32_PhysicalMemoryArray", ct: ct);
        if (arr is { } a)
        {
            summary.TotalSlots = a.GetInt("MemoryDevices");
            var maxEx = a.GetULong("MaxCapacityEx"); // KB
            var max = a.GetULong("MaxCapacity");      // KB
            var kb = maxEx is > 0 ? maxEx : max;
            if (kb is > 0) summary.MaxCapacityBytes = (long)(kb.Value * 1024);
            if (summary.TotalSlots is { } total)
                summary.EmptySlots = Math.Max(0, total - summary.UsedSlots.GetValueOrDefault());
        }

        // Usage summary from OS counters (KB).
        var os = wmi.QuerySingle(
            "SELECT TotalVisibleMemorySize, FreePhysicalMemory, TotalVirtualMemorySize, " +
            "FreeVirtualMemory FROM Win32_OperatingSystem", ct: ct);
        if (os is { } o)
        {
            var totalKb = o.GetULong("TotalVisibleMemorySize");
            var freeKb = o.GetULong("FreePhysicalMemory");
            if (totalKb is > 0)
            {
                summary.UsableBytes = (long)(totalKb.Value * 1024);
                if (freeKb is not null)
                {
                    summary.AvailableBytes = (long)(freeKb.Value * 1024);
                    summary.UsedBytes = summary.UsableBytes - summary.AvailableBytes;
                    summary.UsagePercent = DeviceCalculations.StorageUsagePercent(
                        summary.UsedBytes, summary.UsableBytes);
                    summary.MemoryLoadPercent = summary.UsagePercent;
                }
            }
            var pageTotal = o.GetULong("TotalVirtualMemorySize");
            var pageFree = o.GetULong("FreeVirtualMemory");
            if (pageTotal is > 0) summary.PageFileTotalBytes = (long)(pageTotal.Value * 1024);
            if (pageFree is > 0) summary.PageFileAvailableBytes = (long)(pageFree.Value * 1024);
            collected = true;
        }

        // Installed bytes = sum of module capacities (more accurate than visible memory).
        var installed = report.Hardware.MemoryModules.Sum(m => m.CapacityBytes ?? 0);
        if (installed > 0) summary.InstalledBytes = installed;

        if (!collected) builder.Unavailable("Memory", "no memory data returned");
        return Task.FromResult(collected);
    }

    private static string? MapMemoryType(int? smbios) => smbios switch
    {
        20 => "DDR", 21 => "DDR2", 24 => "DDR3", 26 => "DDR4", 34 => "DDR5",
        null or 0 => null, _ => $"Type {smbios}"
    };

    private static string? MapFormFactor(int? code) => code switch
    {
        8 => "DIMM", 12 => "SODIMM", 13 => "SRIMM", null or 0 => null, _ => $"FF {code}"
    };
}
