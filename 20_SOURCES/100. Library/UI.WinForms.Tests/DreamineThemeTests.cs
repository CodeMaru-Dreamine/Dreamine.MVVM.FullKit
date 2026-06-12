using System.Drawing;
using Dreamine.UI.WinForms;
using Xunit;

namespace Dreamine.UI.WinForms.Tests;

public class DreamineThemeTests
{
    [Fact]
    public void AppBackground_IsExpectedDarkColor()
    {
        Assert.Equal(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x2E), DreamineTheme.AppBackground);
    }

    [Fact]
    public void AccentBlue_IsExpectedColor()
    {
        Assert.Equal(Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF), DreamineTheme.AccentBlue);
    }

    [Fact]
    public void BorderFocus_MatchesAccentBlue()
    {
        Assert.Equal(DreamineTheme.AccentBlue, DreamineTheme.BorderFocus);
    }

    [Fact]
    public void TextPrimary_IsWhite()
    {
        Assert.Equal(Color.White, DreamineTheme.TextPrimary);
    }

    [Fact]
    public void CornerRadius_IsPositive()
    {
        Assert.True(DreamineTheme.CornerRadius > 0);
    }

    [Fact]
    public void HoverOverlay_HasNonZeroAlpha()
    {
        Assert.True(DreamineTheme.HoverOverlay.A > 0);
    }

    [Fact]
    public void PressOverlay_AlphaGreaterThanHover()
    {
        Assert.True(DreamineTheme.PressOverlay.A > DreamineTheme.HoverOverlay.A);
    }
}
