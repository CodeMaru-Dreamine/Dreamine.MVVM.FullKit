namespace Codemaru.Services;

/// <summary>
/// Generates QR code SVG strings from a payload URL.
/// </summary>
public interface IQrSvgGenerator
{
    /// <summary>
    /// Creates an SVG representation of a QR code for the given payload.
    /// </summary>
    /// <param name="payload">The URL or text encoded in the QR code.</param>
    /// <param name="foreground">Optional foreground hex color (default: #111111).</param>
    /// <param name="background">Optional background hex color (default: #ffffff).</param>
    /// <returns>An SVG string representing the QR code.</returns>
    string CreateSvg(string payload, string foreground = "#111111", string background = "#ffffff");
}
