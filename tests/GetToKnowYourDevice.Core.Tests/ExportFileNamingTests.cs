using GetToKnowYourDevice.Core.Export;
using Xunit;

namespace GetToKnowYourDevice.Core.Tests;

public class ExportFileNamingTests
{
    [Fact]
    public void Sanitize_RemovesInvalidCharacters()
    {
        var result = ExportFileNaming.SanitizeDeviceName("My/Device:Name*?");
        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain(':', result);
        Assert.DoesNotContain('*', result);
        Assert.DoesNotContain('?', result);
    }

    [Fact]
    public void Sanitize_CollapsesWhitespaceToUnderscore()
    {
        var result = ExportFileNaming.SanitizeDeviceName("My  Device   Name");
        Assert.DoesNotContain("  ", result);
        Assert.Contains("My_Device_Name", result);
    }

    [Fact]
    public void Sanitize_EmptyInput_ReturnsDevice()
    {
        Assert.Equal("Device", ExportFileNaming.SanitizeDeviceName(""));
        Assert.Equal("Device", ExportFileNaming.SanitizeDeviceName(null));
        Assert.Equal("Device", ExportFileNaming.SanitizeDeviceName("   "));
    }

    [Fact]
    public void BuildFileName_FollowsExpectedFormat()
    {
        var stamp = new DateTimeOffset(2026, 3, 5, 14, 30, 45, TimeSpan.Zero);
        var name = ExportFileNaming.BuildFileName("MyPC", stamp, "json");

        Assert.Equal("GetToKnowYourDevice_MyPC_2026-03-05_14-30-45.json", name);
    }

    [Fact]
    public void BuildFileName_StripsLeadingDotFromExtension()
    {
        var stamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var name = ExportFileNaming.BuildFileName("PC", stamp, ".pdf");
        Assert.EndsWith(".pdf", name);
        Assert.DoesNotContain("..pdf", name);
    }
}
