using GetToKnowYourDevice.Export.Csv;
using Xunit;

namespace GetToKnowYourDevice.Export.Tests;

public class CsvWriterTests
{
    [Fact]
    public void Escape_FieldWithComma_IsQuoted()
    {
        var w = new CsvWriter(",");
        Assert.Equal("\"a,b\"", w.Escape("a,b"));
    }

    [Fact]
    public void Escape_FieldWithQuote_DoublesQuote()
    {
        var w = new CsvWriter(",");
        Assert.Equal("\"say \"\"hi\"\"\"", w.Escape("say \"hi\""));
    }

    [Fact]
    public void Escape_FieldWithNewline_IsQuoted()
    {
        var w = new CsvWriter(",");
        Assert.StartsWith("\"", w.Escape("line1\nline2"));
    }

    [Fact]
    public void Escape_PlainField_NotQuoted()
    {
        var w = new CsvWriter(",");
        Assert.Equal("plain", w.Escape("plain"));
    }

    [Fact]
    public void Escape_Null_ReturnsEmpty()
    {
        var w = new CsvWriter(",");
        Assert.Equal("", w.Escape(null));
    }

    [Fact]
    public void CustomDelimiter_FieldWithSemicolon_IsQuoted()
    {
        var w = new CsvWriter(";");
        Assert.Equal("\"a;b\"", w.Escape("a;b"));
        // But comma is fine with semicolon delimiter.
        Assert.Equal("a,b", w.Escape("a,b"));
    }

    [Fact]
    public void WriteRow_ProducesDelimitedLine()
    {
        var w = new CsvWriter(",");
        w.WriteRow(["a", "b", "c"]);
        Assert.Equal("a,b,c\r\n", w.ToCsvString());
    }

    [Fact]
    public void Format_DistinguishesNullFromEmpty()
    {
        // null -> null (caller renders as empty), but "Unavailable" passes through as text.
        Assert.Null(CsvWriter.Format(null));
        Assert.Equal("Unavailable", CsvWriter.Format("Unavailable"));
    }

    [Fact]
    public void Format_Bool_LowercaseTrueFalse()
    {
        Assert.Equal("true", CsvWriter.Format(true));
        Assert.Equal("false", CsvWriter.Format(false));
    }

    [Fact]
    public void ToBytes_EmitsUtf8Bom()
    {
        var w = new CsvWriter(",");
        w.WriteRow(["x"]);
        var bytes = w.ToBytes();
        // UTF-8 BOM = EF BB BF
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }
}
