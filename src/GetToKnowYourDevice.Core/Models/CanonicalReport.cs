namespace GetToKnowYourDevice.Core.Models;

/// <summary>
/// The single canonical report model. Sole source of truth for UI, scan history,
/// comparison, and all exports (JSON/CSV/PDF). Do not create per-feature variants.
/// </summary>
public sealed class CanonicalReport
{
    public ReportMetadata ReportMetadata { get; set; } = new();
    public DeviceIdentity DeviceIdentity { get; set; } = new();
    public OperatingSystemInfo OperatingSystem { get; set; } = new();
    public HardwareInfo Hardware { get; set; } = new();
    public StorageInfo Storage { get; set; } = new();
    public List<BatteryInfo> Batteries { get; set; } = [];
    public List<DriverInfo> Drivers { get; set; } = [];
    public SecurityInfo Security { get; set; } = new();
    public NetworkInfo Network { get; set; } = new();
    public HealthInfo Health { get; set; } = new();
    public List<ScanDiagnostic> ScanDiagnostics { get; set; } = [];
}
