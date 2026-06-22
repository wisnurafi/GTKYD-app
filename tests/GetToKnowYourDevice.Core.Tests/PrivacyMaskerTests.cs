using GetToKnowYourDevice.Core.Models;
using GetToKnowYourDevice.Core.Privacy;
using Xunit;

namespace GetToKnowYourDevice.Core.Tests;

public class PrivacyMaskerTests
{
    private static CanonicalReport SampleReport() => new()
    {
        DeviceIdentity = new DeviceIdentity
        {
            DeviceName = "MyLaptop",
            CurrentUsername = "alice",
            SerialNumber = "SN123456",
            Uuid = "UUID-ABCDEF"
        },
        Network = new NetworkInfo
        {
            Summary = new NetworkSummary { LocalIPv4 = "192.168.1.100" },
            Adapters = [new NetworkAdapter { MacAddress = "AA:BB:CC:DD:EE:FF" }]
        }
    };

    [Fact]
    public void Mask_SerialNumbers_HidesValue()
    {
        var masker = new PrivacyMasker();
        var masked = masker.Mask(SampleReport(), new MaskingOptions { MaskSerialNumbers = true });

        Assert.NotEqual("SN123456", masked.DeviceIdentity.SerialNumber);
        Assert.StartsWith("SN", masked.DeviceIdentity.SerialNumber);
        Assert.Contains('*', masked.DeviceIdentity.SerialNumber!);
    }

    [Fact]
    public void Mask_DoesNotMutateOriginal()
    {
        var original = SampleReport();
        var masker = new PrivacyMasker();
        masker.Mask(original, MaskingOptions.All());

        // Original keeps true values.
        Assert.Equal("SN123456", original.DeviceIdentity.SerialNumber);
        Assert.Equal("alice", original.DeviceIdentity.CurrentUsername);
    }

    [Fact]
    public void Mask_All_SetsMaskingAppliedFlag()
    {
        var masked = new PrivacyMasker().Mask(SampleReport(), MaskingOptions.All());
        Assert.True(masked.ReportMetadata.MaskingApplied);
    }

    [Fact]
    public void Mask_NothingEnabled_LeavesValuesAndClearsFlag()
    {
        var masked = new PrivacyMasker().Mask(SampleReport(), new MaskingOptions());
        Assert.Equal("MyLaptop", masked.DeviceIdentity.DeviceName);
        Assert.False(masked.ReportMetadata.MaskingApplied);
    }

    [Fact]
    public void Mask_MacAddress_WhenEnabled()
    {
        var masked = new PrivacyMasker().Mask(SampleReport(), new MaskingOptions { MaskMacAddress = true });
        Assert.NotEqual("AA:BB:CC:DD:EE:FF", masked.Network.Adapters[0].MacAddress);
    }

    [Fact]
    public void MaskValue_ShortValue_FullyMasked()
    {
        var masker = new PrivacyMasker();
        Assert.Equal("**", masker.MaskValue("ab"));
    }

    [Fact]
    public void MaskValue_Null_ReturnsNull()
    {
        Assert.Null(new PrivacyMasker().MaskValue(null));
    }
}
