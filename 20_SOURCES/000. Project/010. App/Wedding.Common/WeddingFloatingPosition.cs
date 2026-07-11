namespace Wedding.Common;

/// <summary>
/// Percent-based position for draggable floating elements inside a mobile/desktop viewport.
/// </summary>
public sealed class WeddingFloatingPosition
{
    public double? DesktopX { get; set; }
    public double? DesktopY { get; set; }
    public double? MobileX { get; set; }
    public double? MobileY { get; set; }

    public bool HasDesktop => DesktopX.HasValue && DesktopY.HasValue;
    public bool HasMobile => MobileX.HasValue && MobileY.HasValue;
}
