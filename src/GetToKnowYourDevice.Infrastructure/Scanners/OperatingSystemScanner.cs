using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>Reads OS facts from Win32_OperatingSystem plus the CurrentVersion registry key.</summary>
[SupportedOSPlatform("windows")]
public sealed class OperatingSystemScanner(WmiQueryRunner wmi, ILogger<OperatingSystemScanner> logger)
    : ScannerBase(logger)
{
    public override string Name => "OperatingSystem";
    public override ScanCategory Category => ScanCategory.System;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_OperatingSystem + Registry";
        var os = report.OperatingSystem;
        var collected = false;

        var row = wmi.QuerySingle(
            "SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate, LastBootUpTime, " +
            "WindowsDirectory, SystemDirectory, OSLanguage, CountryCode, MUILanguages " +
            "FROM Win32_OperatingSystem", ct: ct);

        if (row is { } r)
        {
            os.Edition = r.GetString("Caption");
            os.Version = r.GetString("Version");
            os.BuildNumber = r.GetString("BuildNumber");
            os.Architecture = r.GetString("OSArchitecture");
            os.InstallDate = r.GetDateTime("InstallDate");
            os.LastBootTime = r.GetDateTime("LastBootUpTime");
            os.WindowsDirectory = r.GetString("WindowsDirectory");
            os.SystemDirectory = r.GetString("SystemDirectory");
            if (os.LastBootTime is { } boot) os.Uptime = DateTimeOffset.Now - boot;
            collected = true;
        }
        else
        {
            builder.Unavailable("Win32_OperatingSystem", "query returned no rows");
        }

        // DisplayVersion (e.g. 23H2) and firmware mode come from the registry.
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key is not null)
            {
                os.DisplayVersion = key.GetValue("DisplayVersion") as string
                                    ?? key.GetValue("ReleaseId") as string;
                collected = true;
            }
            else builder.Unavailable("Registry:CurrentVersion", "key not found");
        }
        catch (Exception ex)
        {
            builder.Warn($"Registry read failed: {ex.Message}");
        }

        os.TimeZone = TimeZoneInfo.Local.DisplayName;
        os.Region = System.Globalization.RegionInfo.CurrentRegion.EnglishName;
        os.SystemLanguage = System.Globalization.CultureInfo.InstalledUICulture.DisplayName;
        os.DisplayLanguage = System.Globalization.CultureInfo.CurrentUICulture.DisplayName;
        os.FirmwareMode = report.Hardware.Bios.FirmwareType; // filled by BIOS scanner if it ran

        report.ReportMetadata.WindowsVersion = $"{os.Version} (Build {os.BuildNumber})";
        return Task.FromResult(collected);
    }
}
