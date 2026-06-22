namespace GetToKnowYourDevice.Core.Enums;

/// <summary>Outcome of a single scanner section.</summary>
public enum ScanStatus
{
    Success,
    PartialSuccess,
    Unavailable,
    PermissionRequired,
    TimedOut,
    Cancelled,
    Failed
}

/// <summary>How much of the device a scan run covers.</summary>
public enum ScanMode
{
    Quick,
    Full,
    Custom
}

/// <summary>User-selectable scanner groups for Custom Scan and orchestration.</summary>
[Flags]
public enum ScanCategory
{
    None        = 0,
    System      = 1 << 0,
    Hardware    = 1 << 1,
    Storage     = 1 << 2,
    Battery     = 1 << 3,
    Drivers     = 1 << 4,
    Security    = 1 << 5,
    Network     = 1 << 6,
    Peripherals = 1 << 7,
    Diagnostics = 1 << 8
}

/// <summary>Severity used by the health rules engine. Intentionally non-medical wording.</summary>
public enum HealthSeverity
{
    Information,
    Recommendation,
    Warning,
    AttentionRequired
}

/// <summary>Categories the device health score is broken down into.</summary>
public enum HealthCategory
{
    Hardware,
    Storage,
    Battery,
    Security,
    Drivers
}

/// <summary>Classification of a value's volatility, used by the comparison policy.</summary>
public enum FieldStability
{
    Stable,
    SemiStable,
    Volatile,
    Sensitive
}

/// <summary>Kind of change detected when comparing two reports.</summary>
public enum ChangeKind
{
    Unchanged,
    Added,
    Removed,
    Changed,
    Improved,
    Warning,
    Critical
}

/// <summary>Confidence in a reported value (e.g. GPU AdapterRAM from WMI is low confidence).</summary>
public enum DataConfidence
{
    High,
    Medium,
    Low
}
