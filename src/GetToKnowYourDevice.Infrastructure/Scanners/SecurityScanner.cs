using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>
/// Read-only security scanner. Reads antivirus (SecurityCenter2), firewall profile state
/// (registry), TPM (Win32_Tpm), Secure Boot, UAC, and BitLocker volume status. Never changes
/// configuration. Properties needing elevation are reported as PermissionRequired, not failures.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SecurityScanner(WmiQueryRunner wmi, ILogger<SecurityScanner> logger) : ScannerBase(logger)
{
    public override string Name => "Security";
    public override ScanCategory Category => ScanCategory.Security;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:SecurityCenter2,Win32_Tpm + Registry";
        var sec = report.Security;
        var any = false;

        CollectAntivirus(sec, builder, ct, ref any);
        CollectFirewall(sec, builder);
        CollectTpm(sec, builder, ct, ref any);
        CollectDeviceSecurity(sec, report, builder);
        CollectBitLocker(sec, builder, ct);

        return Task.FromResult(any || sec.Device.UacEnabled is not null);
    }

    private void CollectAntivirus(SecurityInfo sec, ScanResultBuilder builder,
        CancellationToken ct, ref bool any)
    {
        try
        {
            foreach (var r in wmi.Query(
                "SELECT displayName, productState, pathToSignedProductExe FROM AntiVirusProduct",
                @"root\SecurityCenter2", ct))
            {
                var state = r.GetUInt("productState");
                var (enabled, rtp) = DecodeProductState(state);
                sec.AntivirusProducts.Add(new AntivirusProduct
                {
                    ProductName = r.GetString("displayName"),
                    Provider = r.GetString("displayName"),
                    ProductState = state?.ToString(),
                    IsEnabled = enabled,
                    RealTimeProtectionEnabled = rtp,
                    ProductPath = r.GetString("pathToSignedProductExe")
                });
                any = true;
            }
        }
        catch (Exception ex) { builder.Warn($"Antivirus query failed: {ex.Message}"); }
    }

    /// <summary>productState is a bitfield; decode enabled + real-time protection.</summary>
    private static (bool? enabled, bool? rtp) DecodeProductState(uint? state)
    {
        if (state is null) return (null, null);
        var s = state.Value;
        var enabledByte = (s >> 12) & 0xFF;
        var rtpByte = (s >> 8) & 0xFF;
        bool enabled = enabledByte is 0x10 or 0x11;
        bool rtp = rtpByte is 0x10 or 0x11;
        return (enabled, rtp);
    }

    private static void CollectFirewall(SecurityInfo sec, ScanResultBuilder builder)
    {
        ReadProfile(sec.Firewall.Domain, "DomainProfile", builder);
        ReadProfile(sec.Firewall.Private, "StandardProfile", builder);
        ReadProfile(sec.Firewall.Public, "PublicProfile", builder);

        static void ReadProfile(FirewallProfile profile, string keyName, ScanResultBuilder builder)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\{keyName}");
                if (key?.GetValue("EnableFirewall") is int e) profile.IsEnabled = e == 1;
                if (key?.GetValue("DefaultInboundAction") is int inb)
                    profile.DefaultInboundAction = inb == 1 ? "Block" : "Allow";
                if (key?.GetValue("DefaultOutboundAction") is int outb)
                    profile.DefaultOutboundAction = outb == 1 ? "Block" : "Allow";
            }
            catch (Exception ex) { builder.Warn($"Firewall {keyName} read failed: {ex.Message}"); }
        }
    }

    private void CollectTpm(SecurityInfo sec, ScanResultBuilder builder, CancellationToken ct, ref bool any)
    {
        try
        {
            var tpm = wmi.QuerySingle(
                "SELECT SpecVersion, ManufacturerIdTxt, IsEnabled_InitialValue, IsActivated_InitialValue, " +
                "IsOwned_InitialValue FROM Win32_Tpm", @"root\CIMV2\Security\MicrosoftTpm", ct);
            if (tpm is { } t)
            {
                sec.Device.TpmDetected = true;
                sec.Device.TpmVersion = t.GetString("SpecVersion");
                sec.Device.TpmManufacturer = t.GetString("ManufacturerIdTxt");
                sec.Device.TpmEnabled = t.GetBool("IsEnabled_InitialValue");
                sec.Device.TpmActivated = t.GetBool("IsActivated_InitialValue");
                sec.Device.TpmOwned = t.GetBool("IsOwned_InitialValue");
                sec.Device.TpmReady = sec.Device.TpmEnabled == true && sec.Device.TpmActivated == true;
                any = true;
            }
            else
            {
                sec.Device.TpmDetected = false;
            }
        }
        catch (UnauthorizedAccessException)
        {
            builder.RequiresElevation = true;
            builder.Unavailable("Win32_Tpm", "requires administrator permission");
        }
        catch (Exception ex) { builder.Warn($"TPM query failed: {ex.Message}"); }
    }

    private void CollectDeviceSecurity(SecurityInfo sec, CanonicalReport report, ScanResultBuilder builder)
    {
        sec.Device.SecureBootSupported = report.Hardware.Bios.SecureBootSupported;
        sec.Device.SecureBootEnabled = report.Hardware.Bios.SecureBootEnabled;

        // UAC
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            if (key?.GetValue("EnableLUA") is int lua) sec.Device.UacEnabled = lua == 1;
            if (key?.GetValue("ConsentPromptBehaviorAdmin") is int c)
                sec.Device.UacLevel = c switch
                {
                    0 => "Never notify", 1 => "Prompt for credentials (secure desktop)",
                    2 => "Prompt for consent (secure desktop)", 5 => "Prompt for consent",
                    _ => $"Level {c}"
                };
        }
        catch (Exception ex) { builder.Warn($"UAC read failed: {ex.Message}"); }

        // Device Guard / VBS / Credential Guard
        try
        {
            var dg = wmi.QuerySingle(
                "SELECT SecurityServicesRunning, VirtualizationBasedSecurityStatus FROM Win32_DeviceGuard",
                @"root\Microsoft\Windows\DeviceGuard");
            if (dg is { } d)
            {
                var vbs = d.GetInt("VirtualizationBasedSecurityStatus");
                sec.Device.VirtualizationBasedSecurityStatus = vbs switch
                { 0 => "Off", 1 => "Configured", 2 => "Running", _ => null };
                var running = d.GetStringArray("SecurityServicesRunning") ?? [];
                if (running.Contains("1")) sec.Device.CredentialGuardStatus = "Running";
                if (running.Contains("2"))
                {
                    sec.Device.HvciStatus = "Running";
                    sec.Device.MemoryIntegrityStatus = "On";
                }
            }
        }
        catch { /* DeviceGuard optional */ }

        // Pending reboot
        try
        {
            using var cbs = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            using var wu = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            sec.Device.PendingReboot = cbs is not null || wu is not null;
        }
        catch { /* best effort */ }
    }

    private void CollectBitLocker(SecurityInfo sec, ScanResultBuilder builder, CancellationToken ct)
    {
        try
        {
            foreach (var r in wmi.Query(
                "SELECT DriveLetter, ProtectionStatus, EncryptionPercentage FROM Win32_EncryptableVolume",
                @"root\CIMV2\Security\MicrosoftVolumeEncryption", ct))
            {
                var status = r.GetUInt("ProtectionStatus");
                sec.Device.BitLockerVolumes.Add(new BitLockerVolume
                {
                    DriveLetter = r.GetString("DriveLetter"),
                    ProtectionStatus = status switch
                    { 0 => "Off", 1 => "On", 2 => "Unknown", _ => null },
                    EncryptionPercent = r.GetInt("EncryptionPercentage")
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            builder.RequiresElevation = true;
            builder.Unavailable("BitLocker", "requires administrator permission");
        }
        catch (Exception ex) { builder.Warn($"BitLocker query failed: {ex.Message}"); }
    }
}
