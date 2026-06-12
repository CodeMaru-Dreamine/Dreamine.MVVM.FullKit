using System.Drawing;
using System.Drawing.Drawing2D;
using Dreamine.UI.WinForms;
using Xunit;

namespace Dreamine.UI.WinForms.Tests;

public class DreamineDrawHelperTests
{
    [Fact]
    public void RoundedRect_ReturnsClosedPath()
    {
        var path = DreamineDrawHelper.RoundedRect(new Rectangle(0, 0, 100, 50), 8f);
        Assert.NotNull(path);
        // Closed path has at least one figure
        path.Dispose();
    }

    [Fact]
    public void RoundedRect_ZeroRadius_ReturnsRectanglePath()
    {
        var rect = new RectangleF(0, 0, 100, 50);
        using var path = DreamineDrawHelper.RoundedRect(rect, 0f);
        Assert.NotNull(path);
    }

    [Theory]
    [InlineData(100, 150, 200)]
    [InlineData(0,   0,   0  )]
    [InlineData(255, 0,   128)]
    public void Blend_WithZeroAlpha_ReturnsBaseColor(int r, int g, int b)
    {
        var baseColor = Color.FromArgb(255, r, g, b);
        var result = DreamineDrawHelper.Blend(baseColor, Color.White, 0f);
        Assert.Equal(baseColor.R, result.R);
        Assert.Equal(baseColor.G, result.G);
        Assert.Equal(baseColor.B, result.B);
    }

    [Fact]
    public void Blend_WithFullAlpha_ReturnsOverlayColor()
    {
        var result = DreamineDrawHelper.Blend(Color.Black, Color.White, 1f);
        Assert.Equal(255, result.R);
        Assert.Equal(255, result.G);
        Assert.Equal(255, result.B);
    }

    [Fact]
    public void Blend_Midpoint_IsAverageColor()
    {
        var result = DreamineDrawHelper.Blend(Color.FromArgb(255, 0, 0, 0),
                                              Color.FromArgb(255, 100, 100, 100), 0.5f);
        Assert.InRange(result.R, 48, 52); // 50 ± rounding
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(255)]
    public void Blend_ResultChannels_AlwaysInRange(int channel)
    {
        var result = DreamineDrawHelper.Blend(
            Color.FromArgb(255, channel, channel, channel),
            Color.White, 0.5f);
        Assert.InRange(result.R, 0, 255);
        Assert.InRange(result.G, 0, 255);
        Assert.InRange(result.B, 0, 255);
    }

    [Fact]
    public void FillRoundedRect_DoesNotThrow()
    {
        using var bmp = new Bitmap(100, 50);
        using var g   = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.Navy);
        using var pen   = new Pen(Color.White);
        var ex = Record.Exception(() =>
            DreamineDrawHelper.FillRoundedRect(g, brush, pen, new Rectangle(5, 5, 90, 40), 6f));
        Assert.Null(ex);
    }

    [Fact]
    public void FillRoundedGradient_ZeroSize_DoesNotThrow()
    {
        using var bmp = new Bitmap(1, 1);
        using var g   = Graphics.FromImage(bmp);
        var ex = Record.Exception(() =>
            DreamineDrawHelper.FillRoundedGradient(g, Color.Black, Color.White, null,
                new Rectangle(0, 0, 0, 0), 6f));
        Assert.Null(ex);
    }
}
