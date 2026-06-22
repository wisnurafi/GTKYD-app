namespace GetToKnowYourDevice.Core.Models;

public sealed class StorageInfo
{
    public StorageSummary Summary { get; set; } = new();
    public List<PhysicalDisk> PhysicalDisks { get; set; } = [];
    public List<DiskPartition> Partitions { get; set; } = [];
    public List<StorageVolume> Volumes { get; set; } = [];
}

public sealed class StorageSummary
{
    public long? TotalBytes { get; set; }
    public long? UsedBytes { get; set; }
    public long? FreeBytes { get; set; }
    public double? UsagePercent { get; set; }
    public int PhysicalDiskCount { get; set; }
    public int PartitionCount { get; set; }
    public int VolumeCount { get; set; }
    public string? SystemDrive { get; set; }
    public string? BootDrive { get; set; }
    public string? PageFileDrive { get; set; }
}

public sealed class PhysicalDisk
{
    public string? FriendlyName { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? MediaType { get; set; }
    public string? BusType { get; set; }
    public string? InterfaceType { get; set; }
    public long? CapacityBytes { get; set; }
    public long? AllocatedSizeBytes { get; set; }
    public string? OperationalStatus { get; set; }
    public string? HealthStatus { get; set; }
    public string? PartitionStyle { get; set; }
    public int? LogicalSectorSize { get; set; }
    public int? PhysicalSectorSize { get; set; }
    public int? SpindleSpeedRpm { get; set; }
    public bool? IsBootDisk { get; set; }
    public bool? IsSystemDisk { get; set; }
    public bool? IsReadOnly { get; set; }
    public bool? IsOffline { get; set; }
    public string? DeviceId { get; set; }
    public string? PnpDeviceId { get; set; }
    public int? DiskNumber { get; set; }
    public SmartData? Smart { get; set; }
}

public sealed class DiskPartition
{
    public int? DiskNumber { get; set; }
    public int? PartitionNumber { get; set; }
    public string? DriveLetter { get; set; }
    public string? Type { get; set; }
    public string? GptType { get; set; }
    public string? MbrType { get; set; }
    public long? OffsetBytes { get; set; }
    public long? SizeBytes { get; set; }
    public bool? IsBoot { get; set; }
    public bool? IsSystem { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsHidden { get; set; }
    public bool? IsReadOnly { get; set; }
}

public sealed class StorageVolume
{
    public string? DriveLetter { get; set; }
    public string? Label { get; set; }
    public string? FileSystem { get; set; }
    public string? FileSystemLabel { get; set; }
    public long? TotalBytes { get; set; }
    public long? UsedBytes { get; set; }
    public long? FreeBytes { get; set; }
    public double? UsagePercent { get; set; }
    public string? VolumeSerialNumber { get; set; }
    public string? DriveType { get; set; }
    public string? HealthStatus { get; set; }
    public string? OperationalStatus { get; set; }
    public bool? IsBootVolume { get; set; }
    public bool? IsSystemVolume { get; set; }
    public bool? HasPageFile { get; set; }
    public string? BitLockerProtectionStatus { get; set; }
}

/// <summary>
/// SMART / storage reliability counters. Must never be assumed available.
/// When unavailable, Status carries the reason rather than reporting 0 as a real value.
/// </summary>
public sealed class SmartData
{
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public bool RequiresElevation { get; set; }
    public string? SourceAttempted { get; set; }

    public string? DeviceHealth { get; set; }
    public int? TemperatureCelsius { get; set; }
    public long? PowerOnHours { get; set; }
    public double? WearPercent { get; set; }
    public double? RemainingLifePercent { get; set; }
    public long? ReadErrors { get; set; }
    public long? WriteErrors { get; set; }
    public double? ReadLatencyMs { get; set; }
    public double? WriteLatencyMs { get; set; }
    public double? FlushLatencyMs { get; set; }
    public long? ReallocatedSectorCount { get; set; }
    public long? PendingSectorCount { get; set; }
    public long? UncorrectableErrorCount { get; set; }
    public long? UnsafeShutdownCount { get; set; }
    public long? PowerCycleCount { get; set; }
    public long? TotalHostReadsBytes { get; set; }
    public long? TotalHostWritesBytes { get; set; }
}
