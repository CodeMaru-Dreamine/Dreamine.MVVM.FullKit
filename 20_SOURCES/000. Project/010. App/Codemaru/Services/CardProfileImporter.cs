using Codemaru.Models;
using System.Text;

namespace Codemaru.Services;

public sealed class CardProfileImporter
{
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

    private static string DecodeValue(string keyPart, string rawValue)
    {
        var value = rawValue;
        if (keyPart.Contains("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase))
        {
            value = DecodeQuotedPrintable(rawValue, GetCharset(keyPart));
        }

        return UnescapeVCard(value).Trim();
    }

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

    private static bool IsHex(char value)
    {
        return (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'F') ||
            (value >= 'a' && value <= 'f');
    }

    private static string UnescapeVCard(string value)
    {
        return value
            .Replace("\\n", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
            .Replace("\\;", ";", StringComparison.Ordinal)
            .Replace("\\,", ",", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    private static string ParseName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split(';');
        return string.Join(" ", parts.Take(2).Reverse().Where(static part => !string.IsNullOrWhiteSpace(part))).Trim();
    }

    private static string ParseAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split(';');
        return string.Join(" ", parts.Skip(2).Where(static part => !string.IsNullOrWhiteSpace(part))).Trim();
    }

    private static string First(Dictionary<string, List<string>> values, string key)
    {
        return values.TryGetValue(key, out var list) ? list.FirstOrDefault() ?? string.Empty : string.Empty;
    }

    private static string Join(Dictionary<string, List<string>> values, string key)
    {
        return values.TryGetValue(key, out var list)
            ? string.Join(Environment.NewLine, list.Where(static value => !string.IsNullOrWhiteSpace(value)))
            : string.Empty;
    }

    private static string Coalesce(params string?[] values)
    {
        return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}
