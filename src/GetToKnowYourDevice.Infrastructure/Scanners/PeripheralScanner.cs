using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>
/// Reads relevant peripheral PnP devices (keyboard, mouse, webcam, bluetooth, USB, printer,
/// biometric, sensors) from Win32_PnPEntity, filtered to interesting device classes.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PeripheralScanner(WmiQueryRunner wmi, ILogger<PeripheralScanner> logger)
    : ScannerBase(logger)
{
    public override string Name => "Peripherals";
    public override ScanCategory Category => ScanCategory.Peripherals;

    private static readonly HashSet<string> InterestingClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Keyboard", "Mouse", "Camera", "Image", "Bluetooth", "USB", "Printer",
        "Biometric", "Sensor", "HIDClass", "MEDIA", "Monitor", "WPD"
    };

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_PnPEntity";
        var any = false;

        foreach (var r in wmi.Query(
            "SELECT Name, PNPClass, Manufacturer, Status, PNPDeviceID, ConfigManagerErrorCode " +
            "FROM Win32_PnPEntity", ct: ct))
        {
            var pnpClass = r.GetString("PNPClass");
            if (pnpClass is null || !InterestingClasses.Contains(pnpClass)) continue;

            var name = r.GetString("Name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            report.Hardware.Peripherals.Add(new PeripheralDevice
            {
                Name = name,
                DeviceClass = pnpClass,
                Manufacturer = r.GetString("Manufacturer"),
                Status = r.GetString("Status"),
                PnpDeviceId = r.GetString("PNPDeviceID"),
                ErrorCode = r.GetInt("ConfigManagerErrorCode")
            });
            any = true;
        }

        if (!any) builder.Unavailable("Win32_PnPEntity", "no matching peripherals");
        return Task.FromResult(any);
    }
}
