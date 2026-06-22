using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>Reads Win32_BaseBoard (motherboard) and Win32_BIOS, plus Secure Boot state from registry.</summary>
[SupportedOSPlatform("windows")]
public sealed class MotherboardBiosScanner(WmiQueryRunner wmi, ILogger<MotherboardBiosScanner> logger)
    : ScannerBase(logger)
{
    public override string Name => "MotherboardBios";
    public override ScanCategory Category => ScanCategory.Hardware;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_BaseBoard,Win32_BIOS + Registry";
        var collected = false;

        var mb = report.Hardware.Motherboard;
        var board = wmi.QuerySingle(
            "SELECT Manufacturer, Product, Version, SerialNumber, Tag, HostingBoard, Replaceable " +
            "FROM Win32_BaseBoard", ct: ct);
        if (board is { } b)
        {
            mb.Manufacturer = b.GetString("Manufacturer");
            mb.Product = b.GetString("Product");
            mb.Version = b.GetString("Version");
            mb.SerialNumber = b.GetString("SerialNumber");
            mb.AssetTag = b.GetString("Tag");
            mb.IsHostingBoard = b.GetBool("HostingBoard");
            mb.IsReplaceable = b.GetBool("Replaceable");
            collected = true;
        }
        else builder.Unavailable("Win32_BaseBoard", "no rows");

        var bios = report.Hardware.Bios;
        var biosRow = wmi.QuerySingle(
            "SELECT Manufacturer, Name, SMBIOSBIOSVersion, ReleaseDate, SerialNumber, " +
            "SMBIOSMajorVersion, SMBIOSMinorVersion FROM Win32_BIOS", ct: ct);
        if (biosRow is { } r)
        {
            bios.Manufacturer = r.GetString("Manufacturer");
            bios.Name = r.GetString("Name");
            bios.Version = r.GetString("SMBIOSBIOSVersion");
            bios.ReleaseDate = r.GetDateTime("ReleaseDate");
            bios.SerialNumber = r.GetString("SerialNumber");
            var maj = r.GetInt("SMBIOSMajorVersion");
            var min = r.GetInt("SMBIOSMinorVersion");
            if (maj is not null) bios.SmbiosVersion = $"{maj}.{min ?? 0}";
            collected = true;
        }
        else builder.Unavailable("Win32_BIOS", "no rows");

        DetectFirmware(bios, builder);

        return Task.FromResult(collected);
    }

    /// <summary>Determines UEFI vs Legacy and Secure Boot state from the registry.</summary>
    private static void DetectFirmware(BiosInfo bios, ScanResultBuilder builder)
    {
        // PEFirmwareType: 1 = BIOS (Legacy), 2 = UEFI
        try
        {
            using var ctrl = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
            var firmwareType = ctrl?.GetValue("PEFirmwareType");
            if (firmwareType is int ft)
            {
                bios.FirmwareType = ft == 2 ? "UEFI" : "Legacy BIOS";
                bios.UefiSupported = ft == 2;
            }
        }
        catch (Exception ex) { builder.Warn($"Firmware type read failed: {ex.Message}"); }

        // Secure Boot state: HKLM\SYSTEM\CurrentControlSet\Control\SecureBoot\State\UEFISecureBootEnabled
        try
        {
            using var sb = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            var enabled = sb?.GetValue("UEFISecureBootEnabled");
            if (enabled is int v)
            {
                bios.SecureBootSupported = true;
                bios.SecureBootEnabled = v == 1;
            }
            else
            {
                bios.SecureBootSupported = bios.UefiSupported;
                builder.Unavailable("SecureBoot.State", "value not present (may require UEFI)");
            }
        }
        catch (Exception ex) { builder.Warn($"Secure Boot read failed: {ex.Message}"); }
    }
}
