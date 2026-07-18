using Codemaru.Models;
using System.Text;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>Card Profile Importer 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card profile importer functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CardProfileImporter
{
    /// <summary>
    /// \if KO
    /// <para>Import V Card 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the import v card operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <param name="template">
    /// \if KO
    /// <para>template에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for template.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Import V Card 작업에서 생성한 <c>CardProfile</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> result produced by the import v card operation.</para>
    /// \endif
    /// </returns>
    public CardProfile ImportVCard(string content, CardProfile template)
    {
        var values = ReadVCardValues(content);
        var name = First(values, "FN");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ParseName(First(values, "N"));
        }

        var company = First(values, "ORG");
        var title = First(values, "TITLE");
        var email = Join(values, "EMAIL");
        var phone = Join(values, "TEL");
        var address = ParseAddress(First(values, "ADR"));
        var url = First(values, "URL");
        var note = First(values, "NOTE");
        var photo = First(values, "PHOTO");
        var brand = Coalesce(company, template.BackBrand, template.Brand);

        return template with
        {
            Brand = brand,
            BackBrand = brand,
            Tagline = string.IsNullOrWhiteSpace(title) ? template.Tagline : title,
            BackTagline = string.IsNullOrWhiteSpace(company) ? template.BackTagline : company,
            Name = Coalesce(name, template.Name),
            Role = Coalesce(title, template.Role),
            Email = Coalesce(email, template.Email),
            Phone = Coalesce(phone, template.Phone),
            Address = Coalesce(address, template.Address),
            Website = Coalesce(url, template.Website),
            ShortBio = Coalesce(note, template.ShortBio),
            LandingDescription = Coalesce(note, template.LandingDescription),
            VCardNote = Coalesce(note, template.VCardNote),
            LogoImageDataUrl = string.IsNullOrWhiteSpace(photo) ? template.LogoImageDataUrl : photo,
            ImportedFrontImageDataUrl = null,
            ImportedBackImageDataUrl = null,
            IncludePhoneInVCard = !string.IsNullOrWhiteSpace(phone) || template.IncludePhoneInVCard,
            IncludeAddressInVCard = !string.IsNullOrWhiteSpace(address) || template.IncludeAddressInVCard
        };
    }

    /// <summary>
    /// \if KO
    /// <para>V Card Values 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads v card values data.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Read V Card Values 작업에서 생성한 <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> result produced by the read v card values operation.</para>
    /// \endif
    /// </returns>
    private static Dictionary<string, List<string>> ReadVCardValues(string content)
    {
        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var lines = UnfoldLines(content);

        foreach (var line in lines)
        {
            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                continue;
            }

            var keyPart = line[..colon];
            var rawValue = line[(colon + 1)..];
            var key = keyPart.Split(';', 2)[0].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = DecodeValue(keyPart, rawValue);
            if (key.Equals("PHOTO", StringComparison.OrdinalIgnoreCase))
            {
                value = DecodePhoto(keyPart, rawValue);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!values.TryGetValue(key, out var list))
            {
                list = new List<string>();
                values[key] = list;
            }

            list.Add(value);
        }

        return values;
    }

    /// <summary>
    /// \if KO
    /// <para>Unfold Lines 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unfold lines operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unfold Lines 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the unfold lines operation.</para>
    /// \endif
    /// </returns>
    private static List<string> UnfoldLines(string content)
    {
        var normalized = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var lines = new List<string>();

        foreach (var line in normalized.Split('\n'))
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && lines.Count > 0)
            {
                lines[^1] += line[1..];
            }
            else
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>
    /// \if KO
    /// <para>Decode Value 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the decode value operation.</para>
    /// \endif
    /// </summary>
    /// <param name="keyPart">
    /// \if KO
    /// <para>key Part에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key part.</para>
    /// \endif
    /// </param>
    /// <param name="rawValue">
    /// \if KO
    /// <para>raw Value에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for raw value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Decode Value 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the decode value operation.</para>
    /// \endif
    /// </returns>
    private static string DecodeValue(string keyPart, string rawValue)
    {
        var value = rawValue;
        if (keyPart.Contains("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase))
        {
            value = DecodeQuotedPrintable(rawValue, GetCharset(keyPart));
        }

        return UnescapeVCard(value).Trim();
    }

    /// <summary>
    /// \if KO
    /// <para>Decode Photo 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the decode photo operation.</para>
    /// \endif
    /// </summary>
    /// <param name="keyPart">
    /// \if KO
    /// <para>key Part에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key part.</para>
    /// \endif
    /// </param>
    /// <param name="rawValue">
    /// \if KO
    /// <para>raw Value에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for raw value.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Decode Photo 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the decode photo operation.</para>
    /// \endif
    /// </returns>
    private static string DecodePhoto(string keyPart, string rawValue)
    {
        if (rawValue.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return rawValue.Trim();
        }

        var base64 = new string(rawValue.Where(static c => !char.IsWhiteSpace(c)).ToArray());
        if (base64.Length == 0)
        {
            return string.Empty;
        }

        var type = "png";
        if (keyPart.Contains("JPEG", StringComparison.OrdinalIgnoreCase) ||
            keyPart.Contains("JPG", StringComparison.OrdinalIgnoreCase))
        {
            type = "jpeg";
        }
        else if (keyPart.Contains("SVG", StringComparison.OrdinalIgnoreCase))
        {
            type = "svg+xml";
        }
        else if (keyPart.Contains("WEBP", StringComparison.OrdinalIgnoreCase))
        {
            type = "webp";
        }

        return $"data:image/{type};base64,{base64}";
    }

    /// <summary>
    /// \if KO
    /// <para>Charset 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the charset value.</para>
    /// \endif
    /// </summary>
    /// <param name="keyPart">
    /// \if KO
    /// <para>key Part에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key part.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Charset 작업에서 생성한 <c>Encoding</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Encoding</c> result produced by the get charset operation.</para>
    /// \endif
    /// </returns>
    private static Encoding GetCharset(string keyPart)
    {
        var parts = keyPart.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var charsetPart = parts.FirstOrDefault(part => part.StartsWith("CHARSET=", StringComparison.OrdinalIgnoreCase));
        var charset = charsetPart?.Split('=', 2)[1].Trim('"');
        if (string.IsNullOrWhiteSpace(charset))
        {
            return Encoding.UTF8;
        }

        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(charset);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Decode Quoted Printable 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the decode quoted printable operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="encoding">
    /// \if KO
    /// <para>encoding에 사용할 <c>Encoding</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Encoding</c> value used for encoding.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Decode Quoted Printable 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the decode quoted printable operation.</para>
    /// \endif
    /// </returns>
    private static string DecodeQuotedPrintable(string value, Encoding encoding)
    {
        var bytes = new List<byte>();
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '=' && i + 2 < value.Length &&
                IsHex(value[i + 1]) && IsHex(value[i + 2]))
            {
                bytes.Add(Convert.ToByte(value.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
            {
                bytes.Add((byte)value[i]);
            }
        }

        return encoding.GetString(bytes.ToArray());
    }

    /// <summary>
    /// \if KO
    /// <para>Is Hex 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is hex.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Hex 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is hex condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsHex(char value)
    {
        return (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'F') ||
            (value >= 'a' && value <= 'f');
    }

    /// <summary>
    /// \if KO
    /// <para>Unescape V Card 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unescape v card operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unescape V Card 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the unescape v card operation.</para>
    /// \endif
    /// </returns>
    private static string UnescapeVCard(string value)
    {
        return value
            .Replace("\\n", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
            .Replace("\\;", ";", StringComparison.Ordinal)
            .Replace("\\,", ",", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the parse name operation.</para>
    /// \endif
    /// </returns>
    private static string ParseName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split(';');
        return string.Join(" ", parts.Take(2).Reverse().Where(static part => !string.IsNullOrWhiteSpace(part))).Trim();
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Address 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse address operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Address 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the parse address operation.</para>
    /// \endif
    /// </returns>
    private static string ParseAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split(';');
        return string.Join(" ", parts.Skip(2).Where(static part => !string.IsNullOrWhiteSpace(part))).Trim();
    }

    /// <summary>
    /// \if KO
    /// <para>First 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the first operation.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>First 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the first operation.</para>
    /// \endif
    /// </returns>
    private static string First(Dictionary<string, List<string>> values, string key)
    {
        return values.TryGetValue(key, out var list) ? list.FirstOrDefault() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// \if KO
    /// <para>Join 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the join operation.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Dictionary&lt;string, List&lt;string&gt;&gt;</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Join 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the join operation.</para>
    /// \endif
    /// </returns>
    private static string Join(Dictionary<string, List<string>> values, string key)
    {
        return values.TryGetValue(key, out var list)
            ? string.Join(Environment.NewLine, list.Where(static value => !string.IsNullOrWhiteSpace(value)))
            : string.Empty;
    }

    /// <summary>
    /// \if KO
    /// <para>Coalesce 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the coalesce operation.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>string?[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?[]</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Coalesce 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the coalesce operation.</para>
    /// \endif
    /// </returns>
    private static string Coalesce(params string?[] values)
    {
        return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}
