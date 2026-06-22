using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>Reads one or more processors from Win32_Processor.</summary>
[SupportedOSPlatform("windows")]
public sealed class ProcessorScanner(WmiQueryRunner wmi, ILogger<ProcessorScanner> logger)
    : ScannerBase(logger)
{
    public override string Name => "Processor";
    public override ScanCategory Category => ScanCategory.Hardware;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_Processor";
        var any = false;

        foreach (var r in wmi.Query(
            "SELECT Name, Manufacturer, Description, Architecture, SocketDesignation, ProcessorId, " +
            "NumberOfCores, NumberOfLogicalProcessors, CurrentClockSpeed, MaxClockSpeed, " +
            "L2CacheSize, L3CacheSize, AddressWidth, DataWidth, VirtualizationFirmwareEnabled, " +
            "SecondLevelAddressTranslationExtensions, VMMonitorModeExtensions, LoadPercentage, " +
            "Status, Availability FROM Win32_Processor", ct: ct))
        {
            var p = new ProcessorInfo
            {
                Name = r.GetString("Name"),
                Manufacturer = r.GetString("Manufacturer"),
                Description = r.GetString("Description"),
                Architecture = MapArch(r.GetUShort("Architecture")),
                Socket = r.GetString("SocketDesignation"),
                ProcessorId = r.GetString("ProcessorId"),
                PhysicalCores = r.GetInt("NumberOfCores"),
                LogicalProcessors = r.GetInt("NumberOfLogicalProcessors"),
                CurrentClockMhz = r.GetInt("CurrentClockSpeed"),
                MaxClockMhz = r.GetInt("MaxClockSpeed"),
                BaseClockMhz = r.GetInt("MaxClockSpeed"),
                L2CacheKb = r.GetLong("L2CacheSize"),
                L3CacheKb = r.GetLong("L3CacheSize"),
                AddressWidth = r.GetInt("AddressWidth"),
                DataWidth = r.GetInt("DataWidth"),
                VirtualizationEnabled = r.GetBool("VirtualizationFirmwareEnabled"),
                VirtualizationSupported = r.GetBool("VMMonitorModeExtensions"),
                SlatSupported = r.GetBool("SecondLevelAddressTranslationExtensions"),
                CurrentLoadPercent = r.GetInt("LoadPercentage"),
                Status = r.GetString("Status"),
                Availability = MapAvailability(r.GetUShort("Availability"))
            };

            if (p.CurrentClockMhz is null) builder.Unavailable("Processor.CurrentClockSpeed");
            if (p.CurrentLoadPercent is null) builder.Unavailable("Processor.LoadPercentage");

            report.Hardware.Processors.Add(p);
            any = true;
        }

        if (!any) builder.Unavailable("Win32_Processor", "no rows");
        return Task.FromResult(any);
    }

    private static string? MapArch(ushort? code) => code switch
    {
        0 => "x86", 1 => "MIPS", 2 => "Alpha", 3 => "PowerPC", 5 => "ARM",
        6 => "ia64", 9 => "x64", 12 => "ARM64", null => null, _ => $"Arch {code}"
    };

    private static string? MapAvailability(ushort? code) => code switch
    {
        3 => "Running/Full Power", 4 => "Warning", 5 => "In Test",
        6 => "Not Applicable", 7 => "Power Off", 8 => "Off Line",
        null => null, _ => $"Code {code}"
    };
}
