using GetToKnowYourDevice.Core.Enums;

namespace GetToKnowYourDevice.Core.Health;

/// <summary>
/// Configurable thresholds for the health rules engine. Exposed via settings so the
/// scoring is tunable and never relies on magic numbers buried in logic.
/// </summary>
public sealed class HealthThresholds
{
    public double StorageFreePercentWarning { get; set; } = 15.0;
    public double StorageFreePercentCritical { get; set; } = 5.0;
    public double BatteryHealthWarning { get; set; } = 80.0;
    public double BatteryHealthCritical { get; set; } = 60.0;

    public int StorageWarningDeduction { get; set; } = 10;
    public int StorageCriticalDeduction { get; set; } = 20;
    public int BatteryWarningDeduction { get; set; } = 10;
    public int BatteryCriticalDeduction { get; set; } = 20;
    public int UnsignedDriverDeduction { get; set; } = 8;
    public int DriverErrorDeduction { get; set; } = 6;
    public int SecureBootDisabledDeduction { get; set; } = 10;
    public int AntivirusDisabledDeduction { get; set; } = 15;
    public int RealtimeProtectionOffDeduction { get; set; } = 12;
    public int PendingRebootDeduction { get; set; } = 5;
    public int DiskUnhealthyDeduction { get; set; } = 20;
}
