using System.Globalization;
using System.Text;

namespace GetToKnowYourDevice.Export.Csv;

/// <summary>
/// Builds RFC 4180-style CSV with a configurable delimiter and proper escaping. Distinguishes
/// null (field omitted by scanner) from "Unavailable" (queried but not exposed): callers pass
/// the marker they want. Fields containing the delimiter, quotes, or newlines are quoted.
/// </summary>
public sealed class CsvWriter(string delimiter = ",")
{
    private readonly StringBuilder _sb = new();
    private readonly string _delimiter = string.IsNullOrEmpty(delimiter) ? "," : delimiter;

    public void WriteRow(IEnumerable<string?> fields)
    {
        var first = true;
        foreach (var field in fields)
        {
            if (!first) _sb.Append(_delimiter);
            _sb.Append(Escape(field));
            first = false;
        }
        _sb.Append("\r\n");
    }

    /// <summary>Escapes a single CSV field. Null becomes empty; embedded quotes are doubled.</summary>
    public string Escape(string? field)
    {
        if (field is null) return "";
        var needsQuoting = field.Contains(_delimiter) || field.Contains('"')
            || field.Contains('\n') || field.Contains('\r');
        if (!needsQuoting) return field;
        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }

    public string ToCsvString() => _sb.ToString();

    /// <summary>UTF-8 bytes with a leading BOM so spreadsheet apps detect the encoding.</summary>
    public byte[] ToBytes()
    {
        var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var preamble = enc.GetPreamble();
        var body = enc.GetBytes(_sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    /// <summary>Formats a value for CSV: null -> empty, others -> invariant string.</summary>
    public static string? Format(object? value) => value switch
    {
        null => null,
        bool b => b ? "true" : "false",
        DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
