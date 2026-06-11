using QRCoder;
using System.Text.RegularExpressions;

namespace Codemaru.Services;

public sealed partial class FreeQrSvgGenerator : IQrSvgGenerator
{
    public string CreateSvg(string payload, string foreground = "#111111", string background = "#ffffff")
    {
        var text = string.IsNullOrWhiteSpace(payload) ? "https://dreamine.local/card" : payload.Trim();
        var fg = NormalizeColor(foreground, "#111111");
        var bg = NormalizeColor(background, "#ffffff");

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        var qr = new SvgQRCode(data);
        var svg = qr.GetGraphic(
            pixelsPerModule: 6,
            darkColorHex: fg,
            lightColorHex: bg,
            drawQuietZones: false,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);

        return AddCssClass(svg);
    }

    private static string AddCssClass(string svg)
    {
        return SvgTagRegex().Replace(svg, match =>
        {
            var tag = match.Value;
            return tag.Contains("class=", StringComparison.OrdinalIgnoreCase)
                ? tag
                : tag.Replace("<svg", "<svg class=\"qr-svg\"", StringComparison.Ordinal);
        }, count: 1);
    }

    private static string NormalizeColor(string color, string fallback)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return fallback;
        }

        var value = color.Trim();
        return value.Length is 4 or 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit)
            ? value
            : fallback;
    }

    [GeneratedRegex("<svg\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SvgTagRegex();
}
