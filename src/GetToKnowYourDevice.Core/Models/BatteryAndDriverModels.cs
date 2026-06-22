namespace GetToKnowYourDevice.Core.Models;

public sealed class BatteryInfo
{
    public string? Manufacturer { get; set; }
    public string? DeviceName { get; set; }
    public string? ProductName { get; set; }
    public string? Chemistry { get; set; }
    public string? SerialNumber { get; set; }
    public string? BatteryId { get; set; }
    public double? ChargePercent { get; set; }
    public string? ChargingStatus { get; set; }
    public bool? AcPowerOnline { get; set; }
    public TimeSpan? EstimatedRuntime { get; set; }
    public long? EstimatedChargeRemainingMwh { get; set; }
    public long? DesignCapacityMwh { get; set; }
    public long? FullChargeCapacityMwh { get; set; }
    public long? CurrentCapacityMwh { get; set; }
    public long? RemainingCapacityMwh { get; set; }

    /// <summary>FullChargeCapacity / DesignCapacity * 100. Null when not computable.</summary>
    public double? HealthPercent { get; set; }

    /// <summary>100 - HealthPercent. Null when health not computable.</summary>
    public double? WearPercent { get; set; }

    public int? CycleCount { get; set; }
    public int? DesignVoltageMv { get; set; }
    public int? CurrentVoltageMv { get; set; }
    public int? ChargeRateMw { get; set; }
    public int? DischargeRateMw { get; set; }
    public double? TemperatureCelsius { get; set; }
    public string? BatteryStatus { get; set; }
    public bool? IsCritical { get; set; }
    public double? LowBatteryThresholdPercent { get; set; }
}

public sealed class DriverInfo
{
    public string? DeviceName { get; set; }
    public string? DeviceClass { get; set; }
    public string? Manufacturer { get; set; }
    public string? DriverProvider { get; set; }
    public string? DriverVersion { get; set; }
    public DateTimeOffset? DriverDate { get; set; }
    public string? InfName { get; set; }
    public string? DeviceId { get; set; }
    public string? PnpDeviceId { get; set; }
    public bool? IsSigned { get; set; }
    public string? Signer { get; set; }
    public string? Status { get; set; }
    public int? ConfigManagerErrorCode { get; set; }
    public string? ServiceName { get; set; }

    // Extended detail fields (populated for detail drawer)
    public string? DeviceDescription { get; set; }
    public List<string> HardwareIds { get; set; } = [];
    public List<string> CompatibleIds { get; set; } = [];
    public string? DevicePath { get; set; }
    public List<string> DriverFilePaths { get; set; } = [];
    public DateTimeOffset? InstallDate { get; set; }

    /// <summary>
    /// True only when DriverDate is older than a local informational threshold.
    /// This is NOT a check against the latest version on the internet.
    /// </summary>
    public bool IsOldDriverCandidate { get; set; }
}
