using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Models;

/// <summary>Metadata about the scan run itself. Stored at the top of every report.</summary>
public sealed class ReportMetadata
{
    public string SchemaVersion { get; set; } = "1.0";
    public string ApplicationVersion { get; set; } = "1.0.0.0";
    public Guid ReportId { get; set; }
    public ScanMode ScanMode { get; set; }
    public string? ScanName { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public ScanStatus OverallStatus { get; set; } = ScanStatus.Success;
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public string? WindowsVersion { get; set; }
    public bool MaskingApplied { get; set; }
}

/// <summary>Identifies the device. Several fields are maskable for privacy.</summary>
public sealed class DeviceIdentity
{
    public string? DeviceName { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? ProductName { get; set; }
    public string? DeviceType { get; set; }
    public string? SerialNumber { get; set; }
    public string? SystemSku { get; set; }
    public string? Uuid { get; set; }
    public string? CurrentUsername { get; set; }
    public string? DomainOrWorkgroup { get; set; }
    public bool? IsVirtualMachine { get; set; }
    public string? VirtualMachineVendor { get; set; }
}

/// <summary>Operating system facts.</summary>
public sealed class OperatingSystemInfo
{
    public string? Edition { get; set; }
    public string? Version { get; set; }
    public string? BuildNumber { get; set; }
    public string? DisplayVersion { get; set; }
    public string? Architecture { get; set; }
    public DateTimeOffset? InstallDate { get; set; }
    public DateTimeOffset? LastBootTime { get; set; }
    public TimeSpan? Uptime { get; set; }
    public string? SystemLanguage { get; set; }
    public string? DisplayLanguage { get; set; }
    public string? Region { get; set; }
    public string? TimeZone { get; set; }
    public string? WindowsDirectory { get; set; }
    public string? SystemDirectory { get; set; }
    public string? FirmwareMode { get; set; }
    public string? ActivationStatus { get; set; }
}
