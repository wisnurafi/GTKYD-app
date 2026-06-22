using GetToKnowYourDevice.Core.Calculations;
using Xunit;

namespace GetToKnowYourDevice.Core.Tests;

public class DeviceCalculationsTests
{
    [Fact]
    public void BatteryHealth_NormalValues_ComputesPercent()
    {
        // 40000 / 50000 = 80%
        var health = DeviceCalculations.BatteryHealthPercent(40000, 50000);
        Assert.Equal(80.0, health);
    }

    [Fact]
    public void BatteryHealth_ZeroDesignCapacity_ReturnsNull()
    {
        Assert.Null(DeviceCalculations.BatteryHealthPercent(40000, 0));
    }

    [Fact]
    public void BatteryHealth_NullDesignCapacity_ReturnsNull()
    {
        Assert.Null(DeviceCalculations.BatteryHealthPercent(40000, null));
    }

    [Fact]
    public void BatteryHealth_NullFullCharge_ReturnsNull()
    {
        Assert.Null(DeviceCalculations.BatteryHealthPercent(null, 50000));
    }

    [Fact]
    public void BatteryWear_FromHealth_IsComplement()
    {
        Assert.Equal(20.0, DeviceCalculations.BatteryWearPercent(80.0));
    }

    [Fact]
    public void BatteryWear_NullHealth_ReturnsNull()
    {
        Assert.Null(DeviceCalculations.BatteryWearPercent(null));
    }

    [Fact]
    public void StorageUsage_NormalValues_ComputesPercent()
    {
        // 75 / 100 = 75%
        Assert.Equal(75.0, DeviceCalculations.StorageUsagePercent(75, 100));
    }

    [Fact]
    public void StorageUsage_ZeroTotal_ReturnsNull()
    {
        Assert.Null(DeviceCalculations.StorageUsagePercent(50, 0));
    }

    [Fact]
    public void StorageUsage_ClampsAbove100()
    {
        Assert.Equal(100.0, DeviceCalculations.StorageUsagePercent(150, 100));
    }

    [Fact]
    public void StorageFree_NormalValues_ComputesPercent()
    {
        Assert.Equal(25.0, DeviceCalculations.StorageFreePercent(25, 100));
    }
}
