using GetToKnowYourDevice.Core.Calculations;
using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Models;

namespace GetToKnowYourDevice.Core.Health;

/// <summary>
/// Computes the device health score from explicit, testable rules. Starts at base 100
/// and deducts per finding. Every deduction records its reason, source property, severity,
/// and recommendation so the score is fully explainable in the UI.
/// </summary>
public sealed class HealthScoreEngine
{
    private readonly HealthThresholds _t;

    public HealthScoreEngine(HealthThresholds? thresholds = null)
        => _t = thresholds ?? new HealthThresholds();

    public HealthInfo Evaluate(CanonicalReport report)
    {
        var findings = new List<HealthFinding>();

        EvaluateStorage(report, findings);
        EvaluateBattery(report, findings);
        EvaluateDrivers(report, findings);
        EvaluateSecurity(report, findings);

        var totalDeduction = findings.Sum(f => f.Deduction);
        var score = Math.Clamp(100 - totalDeduction, 0, 100);

        var categoryScores = Enum.GetValues<HealthCategory>().ToDictionary(
            c => c,
            c => Math.Clamp(100 - findings.Where(f => f.Category == c).Sum(f => f.Deduction), 0, 100));

        return new HealthInfo
        {
            BaseScore = 100,
            Score = score,
            Findings = findings,
            CategoryScores = categoryScores
        };
    }

    private void EvaluateStorage(CanonicalReport report, List<HealthFinding> findings)
    {
        foreach (var vol in report.Storage.Volumes)
        {
            var freePct = DeviceCalculations.StorageFreePercent(vol.FreeBytes, vol.TotalBytes);
            if (freePct is null) continue;

            if (freePct.Value <= _t.StorageFreePercentCritical)
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "storage.free.critical",
                    Category = HealthCategory.Storage,
                    Severity = HealthSeverity.AttentionRequired,
                    Deduction = _t.StorageCriticalDeduction,
                    Reason = $"Drive {vol.DriveLetter} free space is {freePct.Value:0.#}%, below critical threshold {_t.StorageFreePercentCritical}%.",
                    SourceProperty = $"Storage.Volumes[{vol.DriveLetter}].FreeBytes",
                    Recommendation = "Free up disk space to keep the system responsive."
                });
            }
            else if (freePct.Value < _t.StorageFreePercentWarning)
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "storage.free.warning",
                    Category = HealthCategory.Storage,
                    Severity = HealthSeverity.Warning,
                    Deduction = _t.StorageWarningDeduction,
                    Reason = $"Drive {vol.DriveLetter} free space is {freePct.Value:0.#}%, below {_t.StorageFreePercentWarning}%.",
                    SourceProperty = $"Storage.Volumes[{vol.DriveLetter}].FreeBytes",
                    Recommendation = "Consider freeing up disk space."
                });
            }
        }

        foreach (var disk in report.Storage.PhysicalDisks)
        {
            if (!string.IsNullOrEmpty(disk.HealthStatus) &&
                !disk.HealthStatus.Equals("Healthy", StringComparison.OrdinalIgnoreCase) &&
                !disk.HealthStatus.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "storage.disk.health",
                    Category = HealthCategory.Storage,
                    Severity = HealthSeverity.AttentionRequired,
                    Deduction = _t.DiskUnhealthyDeduction,
                    Reason = $"Disk '{disk.FriendlyName ?? disk.Model}' reports health status '{disk.HealthStatus}'.",
                    SourceProperty = "Storage.PhysicalDisks.HealthStatus",
                    Recommendation = "Back up important data and investigate the drive."
                });
            }
        }
    }

    private void EvaluateBattery(CanonicalReport report, List<HealthFinding> findings)
    {
        foreach (var bat in report.Batteries)
        {
            if (bat.HealthPercent is null) continue;
            var health = bat.HealthPercent.Value;

            if (health <= _t.BatteryHealthCritical)
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "battery.health.critical",
                    Category = HealthCategory.Battery,
                    Severity = HealthSeverity.AttentionRequired,
                    Deduction = _t.BatteryCriticalDeduction,
                    Reason = $"Battery health is {health:0.#}%, below critical threshold {_t.BatteryHealthCritical}%.",
                    SourceProperty = "Batteries.HealthPercent",
                    Recommendation = "Battery capacity is significantly reduced. Consider servicing."
                });
            }
            else if (health < _t.BatteryHealthWarning)
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "battery.health.warning",
                    Category = HealthCategory.Battery,
                    Severity = HealthSeverity.Warning,
                    Deduction = _t.BatteryWarningDeduction,
                    Reason = $"Battery health is {health:0.#}%, below {_t.BatteryHealthWarning}%.",
                    SourceProperty = "Batteries.HealthPercent",
                    Recommendation = "Battery shows wear. Monitor runtime over time."
                });
            }
        }
    }

    private void EvaluateDrivers(CanonicalReport report, List<HealthFinding> findings)
    {
        var unsigned = report.Drivers.Count(d => d.IsSigned == false);
        if (unsigned > 0)
        {
            findings.Add(new HealthFinding
            {
                RuleId = "drivers.unsigned",
                Category = HealthCategory.Drivers,
                Severity = HealthSeverity.Warning,
                Deduction = _t.UnsignedDriverDeduction,
                Reason = $"{unsigned} unsigned driver(s) detected.",
                SourceProperty = "Drivers.IsSigned",
                Recommendation = "Unsigned drivers can be a stability or security risk. Review them."
            });
        }

        var errored = report.Drivers.Count(d => d.ConfigManagerErrorCode is > 0);
        if (errored > 0)
        {
            findings.Add(new HealthFinding
            {
                RuleId = "drivers.error",
                Category = HealthCategory.Drivers,
                Severity = HealthSeverity.Warning,
                Deduction = _t.DriverErrorDeduction,
                Reason = $"{errored} device(s) report a driver error code.",
                SourceProperty = "Drivers.ConfigManagerErrorCode",
                Recommendation = "Review devices reporting errors in Device Manager."
            });
        }
    }

    private void EvaluateSecurity(CanonicalReport report, List<HealthFinding> findings)
    {
        var sec = report.Security;

        if (sec.Device.SecureBootEnabled == false)
        {
            findings.Add(new HealthFinding
            {
                RuleId = "security.secureboot.disabled",
                Category = HealthCategory.Security,
                Severity = HealthSeverity.Warning,
                Deduction = _t.SecureBootDisabledDeduction,
                Reason = "Secure Boot is disabled.",
                SourceProperty = "Security.Device.SecureBootEnabled",
                Recommendation = "Enabling Secure Boot improves boot integrity, where supported."
            });
        }

        if (sec.Device.PendingReboot == true)
        {
            findings.Add(new HealthFinding
            {
                RuleId = "security.pendingreboot",
                Category = HealthCategory.Security,
                Severity = HealthSeverity.Recommendation,
                Deduction = _t.PendingRebootDeduction,
                Reason = "A system restart is pending.",
                SourceProperty = "Security.Device.PendingReboot",
                Recommendation = "Restart to finish applying updates."
            });
        }

        // Antivirus rules only fire when at least one product is known.
        if (sec.AntivirusProducts.Count > 0)
        {
            var anyEnabled = sec.AntivirusProducts.Any(a => a.IsEnabled == true);
            if (!anyEnabled)
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "security.antivirus.disabled",
                    Category = HealthCategory.Security,
                    Severity = HealthSeverity.AttentionRequired,
                    Deduction = _t.AntivirusDisabledDeduction,
                    Reason = "No enabled antivirus product detected.",
                    SourceProperty = "Security.AntivirusProducts.IsEnabled",
                    Recommendation = "Ensure antivirus protection is active."
                });
            }
            else if (sec.AntivirusProducts.Any(a => a.IsEnabled == true && a.RealTimeProtectionEnabled == false))
            {
                findings.Add(new HealthFinding
                {
                    RuleId = "security.antivirus.realtime",
                    Category = HealthCategory.Security,
                    Severity = HealthSeverity.Warning,
                    Deduction = _t.RealtimeProtectionOffDeduction,
                    Reason = "Antivirus real-time protection is disabled.",
                    SourceProperty = "Security.AntivirusProducts.RealTimeProtectionEnabled",
                    Recommendation = "Enable real-time protection for continuous scanning."
                });
            }
        }
    }
}
