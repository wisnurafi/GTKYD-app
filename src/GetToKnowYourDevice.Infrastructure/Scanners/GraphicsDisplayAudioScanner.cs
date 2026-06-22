using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>Reads GPUs (Win32_VideoController), monitors (Win32_DesktopMonitor), and audio (Win32_SoundDevice).</summary>
[SupportedOSPlatform("windows")]
public sealed class GraphicsDisplayAudioScanner(WmiQueryRunner wmi, ILogger<GraphicsDisplayAudioScanner> logger)
    : ScannerBase(logger)
{
    public override string Name => "GraphicsDisplayAudio";
    public override ScanCategory Category => ScanCategory.Hardware;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_VideoController,Win32_DesktopMonitor,Win32_SoundDevice";
        var any = false;

        foreach (var r in wmi.Query(
            "SELECT Name, AdapterCompatibility, AdapterRAM, DriverVersion, DriverDate, VideoProcessor, " +
            "VideoArchitecture, VideoMemoryType, CurrentHorizontalResolution, CurrentVerticalResolution, " +
            "CurrentRefreshRate, CurrentBitsPerPixel, Status, Availability, PNPDeviceID FROM Win32_VideoController", ct: ct))
        {
            var hRes = r.GetInt("CurrentHorizontalResolution");
            var vRes = r.GetInt("CurrentVerticalResolution");
            var g = new GraphicsAdapter
            {
                Name = r.GetString("Name"),
                Vendor = r.GetString("AdapterCompatibility"),
                AdapterCompatibility = r.GetString("AdapterCompatibility"),
                AdapterRamBytes = (long?)r.GetUInt("AdapterRAM"),
                AdapterRamConfidence = DataConfidence.Low,
                DriverVersion = r.GetString("DriverVersion"),
                DriverDate = r.GetDateTime("DriverDate"),
                VideoProcessor = r.GetString("VideoProcessor"),
                VideoArchitecture = MapVideoArch(r.GetInt("VideoArchitecture")),
                VideoMemoryType = MapVideoMem(r.GetInt("VideoMemoryType")),
                CurrentResolution = hRes is not null && vRes is not null ? $"{hRes}x{vRes}" : null,
                RefreshRateHz = r.GetInt("CurrentRefreshRate"),
                CurrentBitsPerPixel = r.GetInt("CurrentBitsPerPixel"),
                ColorDepth = r.GetInt("CurrentBitsPerPixel"),
                Status = r.GetString("Status"),
                PnpDeviceId = r.GetString("PNPDeviceID")
            };
            // AdapterRAM is unreliable for modern GPUs (>4GB overflows the 32-bit WMI field).
            if (g.AdapterRamBytes is 4294967295 or 0) builder.Warn(
                $"GPU '{g.Name}' AdapterRAM from WMI is unreliable for modern adapters.");
            report.Hardware.GraphicsAdapters.Add(g);
            any = true;
        }

        foreach (var r in wmi.Query(
            "SELECT Name, MonitorManufacturer, MonitorType, ScreenHeight, ScreenWidth, " +
            "PixelsPerXLogicalInch FROM Win32_DesktopMonitor", ct: ct))
        {
            var w = r.GetInt("ScreenWidth");
            var h = r.GetInt("ScreenHeight");
            report.Hardware.Displays.Add(new DisplayInfo
            {
                MonitorName = r.GetString("Name"),
                Manufacturer = r.GetString("MonitorManufacturer"),
                Resolution = w is not null && h is not null ? $"{w}x{h}" : null,
                IsActive = true
            });
            any = true;
        }

        foreach (var r in wmi.Query(
            "SELECT Name, Manufacturer, ProductName, Status FROM Win32_SoundDevice", ct: ct))
        {
            report.Hardware.AudioDevices.Add(new AudioDevice
            {
                Name = r.GetString("Name"),
                Manufacturer = r.GetString("Manufacturer"),
                ProductName = r.GetString("ProductName"),
                Status = r.GetString("Status")
            });
            any = true;
        }

        if (!any) builder.Unavailable("Graphics/Display/Audio", "no rows");
        return Task.FromResult(any);
    }

    private static string? MapVideoArch(int? c) => c switch
    {
        2 => "Unknown", 3 => "CGA", 4 => "EGA", 5 => "VGA", 6 => "SVGA", 7 => "MDA",
        8 => "HGC", 9 => "MCGA", 10 => "8514A", 11 => "XGA", 12 => "Linear Frame Buffer",
        160 => "PC-98", null => null, _ => $"Arch {c}"
    };

    private static string? MapVideoMem(int? c) => c switch
    {
        2 => "Unknown", 3 => "VRAM", 4 => "DRAM", 5 => "SRAM", 6 => "WRAM",
        7 => "EDO RAM", 8 => "Burst Synchronous DRAM", 9 => "Pipelined Burst SRAM",
        null => null, _ => $"Mem {c}"
    };
}
