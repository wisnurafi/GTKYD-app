using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>Reads device identity + base system info from Win32_ComputerSystem / Win32_SystemEnclosure.</summary>
[SupportedOSPlatform("windows")]
public sealed class SystemScanner(WmiQueryRunner wmi, ILogger<SystemScanner> logger) : ScannerBase(logger)
{
    public override string Name => "System";
    public override ScanCategory Category => ScanCategory.System;

    private static readonly string[] VmManufacturerHints =
        ["vmware", "virtualbox", "innotek", "qemu", "kvm", "xen", "parallels", "bochs"];
    private static readonly string[] VmModelHints =
        ["virtual machine", "vmware", "virtualbox", "kvm", "bochs", "hvm domu"];

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:Win32_ComputerSystem";
        var collected = false;

        var cs = wmi.QuerySingle(
            "SELECT Manufacturer, Model, SystemFamily, SystemType, SystemSKUNumber, " +
            "Name, Domain, Workgroup, PartOfDomain, UserName, BootupState, Roles, " +
            "HypervisorPresent FROM Win32_ComputerSystem", ct: ct);

        var sys = report.Hardware.System;
        var id = report.DeviceIdentity;

        if (cs is { } row)
        {
            sys.Manufacturer = row.GetString("Manufacturer");
            sys.Model = row.GetString("Model");
            sys.SystemFamily = row.GetString("SystemFamily");
            sys.SystemType = row.GetString("SystemType");
            sys.SystemSku = row.GetString("SystemSKUNumber");
            sys.BootState = row.GetString("BootupState");
            sys.HypervisorDetected = row.GetBool("HypervisorPresent");
            sys.DeviceRole = row.GetStringArray("Roles") is { } roles ? string.Join(", ", roles) : null;

            id.DeviceName = row.GetString("Name");
            id.Manufacturer = sys.Manufacturer;
            id.Model = sys.Model;
            id.SystemSku = sys.SystemSku;
            id.CurrentUsername = row.GetString("UserName");
            var partOfDomain = row.GetBool("PartOfDomain") ?? false;
            id.DomainOrWorkgroup = partOfDomain ? row.GetString("Domain") : row.GetString("Workgroup");

            collected = true;
        }
        else
        {
            builder.Unavailable("Win32_ComputerSystem", "query returned no rows");
        }

        // Product / serial / UUID from Win32_ComputerSystemProduct
        var prod = wmi.QuerySingle(
            "SELECT Name, IdentifyingNumber, UUID, Vendor, Version FROM Win32_ComputerSystemProduct", ct: ct);
        if (prod is { } p)
        {
            sys.ProductName = p.GetString("Name");
            sys.SerialNumber = p.GetString("IdentifyingNumber");
            sys.Uuid = p.GetString("UUID");
            id.ProductName = sys.ProductName;
            id.SerialNumber ??= sys.SerialNumber;
            id.Uuid = sys.Uuid;
            collected = true;
        }

        // Chassis / asset tag from Win32_SystemEnclosure
        var enc = wmi.QuerySingle(
            "SELECT ChassisTypes, SerialNumber, SMBIOSAssetTag FROM Win32_SystemEnclosure", ct: ct);
        if (enc is { } e)
        {
            sys.ChassisType = MapChassis(e.GetStringArray("ChassisTypes")?.FirstOrDefault());
            sys.AssetTag = e.GetString("SMBIOSAssetTag");
            id.DeviceType = sys.ChassisType;
        }

        // VM detection
        var isVm = DetectVm(sys, out var vendor);
        sys.IsVirtualMachine = isVm;
        sys.VirtualMachineVendor = vendor;
        id.IsVirtualMachine = isVm;
        id.VirtualMachineVendor = vendor;

        if (collected) ctx.DeviceId ??= sys.Uuid ?? sys.SerialNumber ?? id.DeviceName;
        return Task.FromResult(collected);
    }

    private static bool DetectVm(SystemInfo sys, out string? vendor)
    {
        vendor = null;
        var mfg = (sys.Manufacturer ?? "").ToLowerInvariant();
        var model = (sys.Model ?? "").ToLowerInvariant();

        foreach (var hint in VmManufacturerHints)
            if (mfg.Contains(hint)) { vendor = sys.Manufacturer; return true; }
        foreach (var hint in VmModelHints)
            if (model.Contains(hint)) { vendor = sys.Model; return true; }
        if (sys.HypervisorDetected == true && (mfg.Contains("microsoft") && model.Contains("virtual")))
        { vendor = "Microsoft Hyper-V"; return true; }
        return false;
    }

    private static string? MapChassis(string? code) => code switch
    {
        "3" => "Desktop", "4" => "Low Profile Desktop", "5" => "Pizza Box",
        "6" => "Mini Tower", "7" => "Tower", "8" => "Portable", "9" => "Laptop",
        "10" => "Notebook", "11" => "Hand Held", "12" => "Docking Station",
        "13" => "All in One", "14" => "Sub Notebook", "15" => "Space-Saving",
        "16" => "Lunch Box", "17" => "Main System Chassis", "18" => "Expansion Chassis",
        "21" => "Peripheral Chassis", "23" => "Rack Mount Chassis",
        "30" => "Tablet", "31" => "Convertible", "32" => "Detachable",
        null => null, _ => $"Type {code}"
    };
}
