namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>I Qr Svg Generator 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Generates QR code SVG strings from a payload URL.</para>
/// \endif
/// </summary>
public interface IQrSvgGenerator
{
    /// <summary>
    /// \if KO
    /// <para>Svg 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates an SVG representation of a QR code for the given payload.</para>
    /// \endif
    /// </summary>
    /// <param name="payload">
    /// \if KO
    /// <para>payload에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The URL or text encoded in the QR code.</para>
    /// \endif
    /// </param>
    /// <param name="foreground">
    /// \if KO
    /// <para>foreground에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Optional foreground hex color (default: #111111).</para>
    /// \endif
    /// </param>
    /// <param name="background">
    /// \if KO
    /// <para>background에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Optional background hex color (default: <c>ffffff</c>).</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>An SVG string representing the QR code.</para>
    /// \endif
    /// </returns>
    string CreateSvg(string payload, string foreground = "#111111", string background = "#ffffff");
}
