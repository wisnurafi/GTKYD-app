using GetToKnowYourDevice.Core.Enums;
using GetToKnowYourDevice.Core.Health;
using GetToKnowYourDevice.Core.Models;
using Xunit;

namespace GetToKnowYourDevice.Core.Tests;

public class HealthScoreEngineTests
{
    private static CanonicalReport HealthyReport() => new()
    {
        Storage = new StorageInfo
        {
            Volumes = [new StorageVolume { DriveLetter = "C:", TotalBytes = 100, FreeBytes = 50 }],
            PhysicalDisks = [new PhysicalDisk { HealthStatus = "Healthy" }]
        },
        Security = new SecurityInfo
        {
            Device = new DeviceSecurity { SecureBootEnabled = true, PendingReboot = false },
            AntivirusProducts = [new AntivirusProduct { IsEnabled = true, RealTimeProtectionEnabled = true }]
        }
    };

    [Fact]
    public void HealthyDevice_ScoresFull100()
    {
        var engine = new HealthScoreEngine();
        var result = engine.Evaluate(HealthyReport());
        Assert.Equal(100, result.Score);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void LowStorage_DeductsAndRecordsFinding()
    {
        var report = HealthyReport();
        report.Storage.Volumes[0].FreeBytes = 3; // 3% free, below 5% critical

        var engine = new HealthScoreEngine();
        var result = engine.Evaluate(report);

        Assert.True(result.Score < 100);
        Assert.Contains(result.Findings, f => f.Category == HealthCategory.Storage);
    }

    [Fact]
    public void SecureBootDisabled_ProducesSecurityFinding()
    {
        var report = HealthyReport();
        report.Security.Device.SecureBootEnabled = false;

        var result = new HealthScoreEngine().Evaluate(report);

        Assert.Contains(result.Findings, f => f.RuleId == "security.secureboot.disabled");
    }

    [Fact]
    public void AntivirusDisabled_ProducesAttentionFinding()
    {
        var report = HealthyReport();
        report.Security.AntivirusProducts[0].IsEnabled = false;

        var result = new HealthScoreEngine().Evaluate(report);

        Assert.Contains(result.Findings, f =>
            f.RuleId == "security.antivirus.disabled" && f.Severity == HealthSeverity.AttentionRequired);
    }

    [Fact]
    public void UnsignedDriver_DeductsScore()
    {
        var report = HealthyReport();
        report.Drivers = [new DriverInfo { DeviceName = "X", IsSigned = false }];

        var result = new HealthScoreEngine().Evaluate(report);

        Assert.Contains(result.Findings, f => f.RuleId == "drivers.unsigned");
    }

    [Fact]
    public void Score_NeverGoesBelowZero()
    {
        var report = HealthyReport();
        report.Storage.Volumes[0].FreeBytes = 0;
        report.Storage.PhysicalDisks[0].HealthStatus = "Unhealthy";
        report.Security.Device.SecureBootEnabled = false;
        report.Security.AntivirusProducts[0].IsEnabled = false;
        report.Batteries = [new BatteryInfo { HealthPercent = 10 }];
        report.Drivers = Enumerable.Range(0, 50)
            .Select(i => new DriverInfo { DeviceName = $"D{i}", IsSigned = false, ConfigManagerErrorCode = 1 })
            .ToList();

        var result = new HealthScoreEngine().Evaluate(report);

        Assert.InRange(result.Score, 0, 100);
    }

    [Fact]
    public void ConfigurableThreshold_ChangesOutcome()
    {
        var report = HealthyReport();
        report.Storage.Volumes[0].FreeBytes = 20; // 20% free

        // Default warning threshold = 15%, so 20% is fine.
        Assert.Empty(new HealthScoreEngine().Evaluate(report).Findings);

        // Raise warning threshold to 25%, so 20% now triggers.
        var strict = new HealthScoreEngine(new HealthThresholds { StorageFreePercentWarning = 25 });
        Assert.Contains(strict.Evaluate(report).Findings, f => f.Category == HealthCategory.Storage);
    }
}
