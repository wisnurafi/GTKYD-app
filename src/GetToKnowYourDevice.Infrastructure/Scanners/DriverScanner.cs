using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>
/// Reads installed PnP drivers from Win32_PnPSignedDriver. The "old driver candidate" flag is a
/// local, informational heuristic based on driver date only; it is NOT a check against the latest
/// version available online. This is not the full set of Windows kernel drivers.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DriverScanner(WmiQueryRunner wmi, ILogger<DriverScanner> logger) : ScannerBase(logger)
{
    public override string Name => "Drivers";
    public override ScanCategory Category => ScanCategory.Drivers;

    /// <summary>Drivers older than this (by date) are flagged as informational "old candidates".</summary>
    private static readonly TimeSpan OldDriverAge = TimeSpan.FromDays(365 * 5);

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_PnPSignedDriver";
        var any = false;
        var now = DateTimeOffset.Now;

        foreach (var r in wmi.Query(
            "SELECT DeviceName, DeviceClass, Manufacturer, DriverProviderName, DriverVersion, DriverDate, " +
            "InfName, DeviceID, IsSigned, Signer FROM Win32_PnPSignedDriver",
            ct: ct))
        {
            var name = r.GetString("DeviceName");
            if (string.IsNullOrWhiteSpace(name)) continue;

            var date = r.GetDateTime("DriverDate");
            var d = new DriverInfo
            {
                DeviceName = name,
                DeviceClass = r.GetString("DeviceClass"),
                Manufacturer = r.GetString("Manufacturer"),
                DriverProvider = r.GetString("DriverProviderName"),
                DriverVersion = r.GetString("DriverVersion"),
                DriverDate = date,
                InfName = r.GetString("InfName"),
                DeviceId = r.GetString("DeviceID"),
                PnpDeviceId = r.GetString("DeviceID"),
                IsSigned = r.GetBool("IsSigned"),
                Signer = r.GetString("Signer"),
                Status = "OK",
                IsOldDriverCandidate = date is { } dt && (now - dt) > OldDriverAge
            };
            report.Drivers.Add(d);
            any = true;
        }

        if (!any) builder.Unavailable("Win32_PnPSignedDriver", "no rows");
        else Logger.LogInformation("Collected {Count} drivers", report.Drivers.Count);
        return Task.FromResult(any);
    }
}
