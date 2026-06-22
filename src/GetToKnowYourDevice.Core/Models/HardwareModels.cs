using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Models;

public sealed class HardwareInfo
{
    public SystemInfo System { get; set; } = new();
    public MotherboardInfo Motherboard { get; set; } = new();
    public BiosInfo Bios { get; set; } = new();
    public List<ProcessorInfo> Processors { get; set; } = [];
    public MemorySummary MemorySummary { get; set; } = new();
    public List<MemoryModule> MemoryModules { get; set; } = [];
    public List<GraphicsAdapter> GraphicsAdapters { get; set; } = [];
    public List<DisplayInfo> Displays { get; set; } = [];
    public List<AudioDevice> AudioDevices { get; set; } = [];
    public List<PeripheralDevice> Peripherals { get; set; } = [];
}

public sealed class SystemInfo
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? ProductName { get; set; }
    public string? SystemFamily { get; set; }
    public string? SystemType { get; set; }
    public string? SystemSku { get; set; }
    public string? Uuid { get; set; }
    public string? SerialNumber { get; set; }
    public string? ChassisType { get; set; }
    public string? AssetTag { get; set; }
    public string? BootState { get; set; }
    public string? DeviceRole { get; set; }
    public bool? HypervisorDetected { get; set; }
    public bool? IsVirtualMachine { get; set; }
    public string? VirtualMachineVendor { get; set; }
}

public sealed class MotherboardInfo
{
    public string? Manufacturer { get; set; }
    public string? Product { get; set; }
    public string? Version { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public bool? IsHostingBoard { get; set; }
    public bool? IsReplaceable { get; set; }
    public string? BusArchitecture { get; set; }
}

public sealed class BiosInfo
{
    public string? Manufacturer { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public DateTimeOffset? ReleaseDate { get; set; }
    public string? SmbiosVersion { get; set; }
    public string? FirmwareType { get; set; }
    public bool? UefiSupported { get; set; }
    public bool? SecureBootSupported { get; set; }
    public bool? SecureBootEnabled { get; set; }
    public string? SerialNumber { get; set; }
    public string? EmbeddedControllerVersion { get; set; }
}

public sealed class ProcessorInfo
{
    public string? Name { get; set; }
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public string? Architecture { get; set; }
    public string? Socket { get; set; }
    public string? ProcessorId { get; set; }
    public int? PhysicalCores { get; set; }
    public int? LogicalProcessors { get; set; }
    public int? BaseClockMhz { get; set; }
    public int? MaxClockMhz { get; set; }
    public int? CurrentClockMhz { get; set; }
    public long? L2CacheKb { get; set; }
    public long? L3CacheKb { get; set; }
    public int? AddressWidth { get; set; }
    public int? DataWidth { get; set; }
    public bool? VirtualizationSupported { get; set; }
    public bool? VirtualizationEnabled { get; set; }
    public bool? SlatSupported { get; set; }
    public double? CurrentLoadPercent { get; set; }
    public string? Status { get; set; }
    public string? Availability { get; set; }
}

public sealed class MemorySummary
{
    public long? InstalledBytes { get; set; }
    public long? UsableBytes { get; set; }
    public long? AvailableBytes { get; set; }
    public long? UsedBytes { get; set; }
    public double? UsagePercent { get; set; }
    public double? MemoryLoadPercent { get; set; }
    public int? TotalSlots { get; set; }
    public int? UsedSlots { get; set; }
    public int? EmptySlots { get; set; }
    public long? MaxCapacityBytes { get; set; }
    public long? PageFileTotalBytes { get; set; }
    public long? PageFileAvailableBytes { get; set; }
}

public sealed class MemoryModule
{
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public long? CapacityBytes { get; set; }
    public int? SpeedMhz { get; set; }
    public int? ConfiguredSpeedMhz { get; set; }
    public string? MemoryType { get; set; }
    public int? SmbiosMemoryType { get; set; }
    public string? FormFactor { get; set; }
    public string? BankLabel { get; set; }
    public string? DeviceLocator { get; set; }
    public int? VoltageMv { get; set; }
    public int? MinVoltageMv { get; set; }
    public int? MaxVoltageMv { get; set; }
    public int? DataWidth { get; set; }
    public int? TotalWidth { get; set; }
    public string? TypeDetail { get; set; }
    public string? Status { get; set; }
}

public sealed class GraphicsAdapter
{
    public string? Name { get; set; }
    public string? Vendor { get; set; }
    public string? AdapterCompatibility { get; set; }
    public long? AdapterRamBytes { get; set; }
    public DataConfidence AdapterRamConfidence { get; set; } = DataConfidence.Low;
    public string? DriverVersion { get; set; }
    public DateTimeOffset? DriverDate { get; set; }
    public string? VideoProcessor { get; set; }
    public string? VideoArchitecture { get; set; }
    public string? VideoMemoryType { get; set; }
    public string? CurrentResolution { get; set; }
    public int? RefreshRateHz { get; set; }
    public int? ColorDepth { get; set; }
    public int? CurrentBitsPerPixel { get; set; }
    public string? Status { get; set; }
    public string? Availability { get; set; }
    public string? DeviceId { get; set; }
    public string? PnpDeviceId { get; set; }
}

public sealed class DisplayInfo
{
    public string? MonitorName { get; set; }
    public string? Manufacturer { get; set; }
    public string? ProductCode { get; set; }
    public string? SerialNumber { get; set; }
    public string? Resolution { get; set; }
    public int? RefreshRateHz { get; set; }
    public string? PhysicalDimensions { get; set; }
    public bool? IsPrimary { get; set; }
    public string? ConnectionType { get; set; }
    public bool? HdrSupported { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AudioDevice
{
    public string? Name { get; set; }
    public string? Manufacturer { get; set; }
    public string? ProductName { get; set; }
    public string? DriverVersion { get; set; }
    public DateTimeOffset? DriverDate { get; set; }
    public string? Status { get; set; }
    public bool? IsDefaultPlayback { get; set; }
    public bool? IsDefaultCommunicationPlayback { get; set; }
    public bool? IsDefaultRecording { get; set; }
    public bool? IsDefaultCommunicationRecording { get; set; }
}

public sealed class PeripheralDevice
{
    public string? Name { get; set; }
    public string? DeviceClass { get; set; }
    public string? Manufacturer { get; set; }
    public string? Status { get; set; }
    public string? PnpDeviceId { get; set; }
    public int? ErrorCode { get; set; }
}
