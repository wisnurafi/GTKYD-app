using System.Runtime.Versioning;
using GetToKnowYourDevice.Core.Calculations;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Scanning;
using GetToKnowYourDevice.Infrastructure.Scanning;
using GetToKnowYourDevice.Infrastructure.Wmi;
using Microsoft.Extensions.Logging;

namespace GetToKnowYourDevice.Infrastructure.Scanners;

/// <summary>
/// Reads physical disks, partitions, and volumes. Uses the Storage WMI namespace
/// (root\Microsoft\Windows\Storage MSFT_* classes) with Win32_LogicalDisk for volume usage.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class StorageScanner(WmiQueryRunner wmi, ILogger<StorageScanner> logger) : ScannerBase(logger)
{
    private const string StorageScope = @"root\Microsoft\Windows\Storage";

    public override string Name => "Storage";
    public override ScanCategory Category => ScanCategory.Storage;

    protected override Task<bool> CollectAsync(CanonicalReport report, ScanContext ctx,
        ScanResultBuilder builder, CancellationToken ct)
    {
        builder.Source = "WMI:MSFT_PhysicalDisk,MSFT_Disk,MSFT_Partition,Win32_LogicalDisk";
        var storage = report.Storage;
        var any = false;

        CollectPhysicalDisks(storage, builder, ct, ref any);
        CollectPartitions(storage, builder, ct, ref any);
        CollectVolumes(storage, builder, ct, ref any);
        BuildSummary(storage);

        if (!any) builder.Unavailable("Storage", "no storage data");
        return Task.FromResult(any);
    }

    private void CollectPhysicalDisks(StorageInfo storage, ScanResultBuilder builder,
        CancellationToken ct, ref bool any)
    {
        try
        {
            foreach (var r in wmi.Query(
                "SELECT FriendlyName, Model, Manufacturer, SerialNumber, FirmwareVersion, MediaType, " +
                "BusType, Size, AllocatedSize, OperationalStatus, HealthStatus, SpindleSpeed, " +
                "DeviceId, PhysicalSectorSize, LogicalSectorSize FROM MSFT_PhysicalDisk", StorageScope, ct))
            {
                storage.PhysicalDisks.Add(new PhysicalDisk
                {
                    FriendlyName = r.GetString("FriendlyName"),
                    Model = r.GetString("Model"),
                    Manufacturer = r.GetString("Manufacturer"),
                    SerialNumber = r.GetString("SerialNumber")?.Trim(),
                    FirmwareVersion = r.GetString("FirmwareVersion"),
                    MediaType = MapMediaType(r.GetUShort("MediaType")),
                    BusType = MapBusType(r.GetUShort("BusType")),
                    CapacityBytes = (long?)r.GetULong("Size"),
                    AllocatedSizeBytes = (long?)r.GetULong("AllocatedSize"),
                    OperationalStatus = MapOpStatus(r.GetUShort("OperationalStatus")),
                    HealthStatus = MapHealth(r.GetUShort("HealthStatus")),
                    SpindleSpeedRpm = r.GetInt("SpindleSpeed"),
                    PhysicalSectorSize = r.GetInt("PhysicalSectorSize"),
                    LogicalSectorSize = r.GetInt("LogicalSectorSize"),
                    DeviceId = r.GetString("DeviceId"),
                    Smart = new SmartData
                    {
                        IsAvailable = false,
                        SourceAttempted = "MSFT_StorageReliabilityCounter",
                        UnavailableReason = "SMART/reliability counters not collected in this scan."
                    }
                });
                any = true;
            }
        }
        catch (Exception ex)
        {
            builder.Warn($"MSFT_PhysicalDisk query failed: {ex.Message}");
        }
    }

    private void CollectPartitions(StorageInfo storage, ScanResultBuilder builder,
        CancellationToken ct, ref bool any)
    {
        try
        {
            foreach (var r in wmi.Query(
                "SELECT DiskNumber, PartitionNumber, DriveLetter, GptType, MbrType, Offset, Size, " +
                "IsBoot, IsSystem, IsActive, IsHidden, IsReadOnly FROM MSFT_Partition", StorageScope, ct))
            {
                var letter = r.GetString("DriveLetter");
                storage.Partitions.Add(new DiskPartition
                {
                    DiskNumber = r.GetInt("DiskNumber"),
                    PartitionNumber = r.GetInt("PartitionNumber"),
                    DriveLetter = string.IsNullOrWhiteSpace(letter) || letter == "\0" ? null : letter,
                    Type = r.GetString("Type"),
                    GptType = r.GetString("GptType"),
                    OffsetBytes = (long?)r.GetULong("Offset"),
                    SizeBytes = (long?)r.GetULong("Size"),
                    IsBoot = r.GetBool("IsBoot"),
                    IsSystem = r.GetBool("IsSystem"),
                    IsActive = r.GetBool("IsActive"),
                    IsHidden = r.GetBool("IsHidden"),
                    IsReadOnly = r.GetBool("IsReadOnly")
                });
                any = true;
            }
            if (storage.Partitions.Count == 0)
                builder.Unavailable("MSFT_Partition", "query returned no rows");
        }
        catch (Exception ex)
        {
            builder.Warn($"MSFT_Partition query failed: {ex.Message}");
            builder.Unavailable("MSFT_Partition", ex.Message);
        }
    }

    private void CollectVolumes(StorageInfo storage, ScanResultBuilder builder,
        CancellationToken ct, ref bool any)
    {
        foreach (var r in wmi.Query(
            "SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace, VolumeSerialNumber, DriveType " +
            "FROM Win32_LogicalDisk", ct: ct))
        {
            var total = (long?)r.GetULong("Size");
            var free = (long?)r.GetULong("FreeSpace");
            var used = total is not null && free is not null ? total - free : null;
            storage.Volumes.Add(new StorageVolume
            {
                DriveLetter = r.GetString("DeviceID"),
                Label = r.GetString("VolumeName"),
                FileSystem = r.GetString("FileSystem"),
                FileSystemLabel = r.GetString("VolumeName"),
                TotalBytes = total,
                FreeBytes = free,
                UsedBytes = used,
                UsagePercent = DeviceCalculations.StorageUsagePercent(used, total),
                VolumeSerialNumber = r.GetString("VolumeSerialNumber"),
                DriveType = MapDriveType(r.GetUInt("DriveType")),
                BitLockerProtectionStatus = "Unknown"
            });
            any = true;
        }
        if (storage.Volumes.Count == 0) builder.Unavailable("Win32_LogicalDisk", "no volumes");
    }

    private static void BuildSummary(StorageInfo storage)
    {
        var s = storage.Summary;
        s.PhysicalDiskCount = storage.PhysicalDisks.Count;
        s.PartitionCount = storage.Partitions.Count;
        s.VolumeCount = storage.Volumes.Count;

        var fixedVols = storage.Volumes.Where(v => v.DriveType == "Local Disk").ToList();
        s.TotalBytes = fixedVols.Sum(v => v.TotalBytes ?? 0);
        s.FreeBytes = fixedVols.Sum(v => v.FreeBytes ?? 0);
        s.UsedBytes = s.TotalBytes - s.FreeBytes;
        s.UsagePercent = DeviceCalculations.StorageUsagePercent(s.UsedBytes, s.TotalBytes);
        s.SystemDrive = Environment.GetEnvironmentVariable("SystemDrive");
    }

    private static string? MapMediaType(ushort? c) => c switch
    { 3 => "HDD", 4 => "SSD", 5 => "SCM", 0 => "Unspecified", null => null, _ => $"Media {c}" };

    private static string? MapBusType(ushort? c) => c switch
    {
        1 => "SCSI", 2 => "ATAPI", 3 => "ATA", 4 => "1394", 5 => "SSA", 6 => "Fibre Channel",
        7 => "USB", 8 => "RAID", 9 => "iSCSI", 10 => "SAS", 11 => "SATA", 12 => "SD",
        13 => "MMC", 17 => "NVMe", null => null, _ => $"Bus {c}"
    };

    private static string? MapHealth(ushort? c) => c switch
    { 0 => "Healthy", 1 => "Warning", 2 => "Unhealthy", null => null, _ => $"Health {c}" };

    private static string? MapOpStatus(ushort? c) => c switch
    { 1 => "Other", 2 => "OK", 3 => "Degraded", 4 => "Stressed", 53251 => "Online",
      null => null, _ => $"Status {c}" };

    private static string? MapDriveType(uint? c) => c switch
    {
        0 => "Unknown", 1 => "No Root Directory", 2 => "Removable Disk", 3 => "Local Disk",
        4 => "Network Drive", 5 => "Optical Disc", 6 => "RAM Disk", null => null, _ => $"Type {c}"
    };
}
