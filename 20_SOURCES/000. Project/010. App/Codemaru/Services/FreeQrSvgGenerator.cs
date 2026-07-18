using QRCoder;
using System.Text.RegularExpressions;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>Free Qr Svg Generator 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates free qr svg generator functionality and related state.</para>
/// \endif
/// </summary>
public sealed partial class FreeQrSvgGenerator : IQrSvgGenerator
{
    /// <summary>
    /// \if KO
    /// <para>Svg 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the svg value.</para>
    /// \endif
    /// </summary>
    /// <param name="payload">
    /// \if KO
    /// <para>payload에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for payload.</para>
    /// \endif
    /// </param>
    /// <param name="foreground">
    /// \if KO
    /// <para>foreground에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for foreground.</para>
    /// \endif
    /// </param>
    /// <param name="background">
    /// \if KO
    /// <para>background에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for background.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the create svg operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Css Class 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the css class item.</para>
    /// \endif
    /// </summary>
    /// <param name="svg">
    /// \if KO
    /// <para>svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for svg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Add Css Class 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the add css class operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Normalize Color 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize color operation.</para>
    /// \endif
    /// </summary>
    /// <param name="color">
    /// \if KO
    /// <para>color에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for color.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Color 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize color operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Svg Tag Regex 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the svg tag regex operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Svg Tag Regex 작업에서 생성한 <c>Regex</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Regex</c> result produced by the svg tag regex operation.</para>
    /// \endif
    /// </returns>
    [GeneratedRegex("<svg\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SvgTagRegex();
}
