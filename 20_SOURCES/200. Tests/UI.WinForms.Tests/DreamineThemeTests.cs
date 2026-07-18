using System.Drawing;
using Dreamine.UI.WinForms;
using Xunit;

namespace Dreamine.UI.WinForms.Tests;

/// <summary>
/// \if KO
/// <para>WinForms Dreamine 테마의 주요 색상과 크기 상수를 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies key colors and dimension constants of the WinForms Dreamine theme.</para>
/// \endif
/// </summary>
public class DreamineThemeTests
{
    /// <summary>
    /// \if KO
    /// <para>애플리케이션 배경색이 예상한 어두운 색상인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the application background is the expected dark color.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void AppBackground_IsExpectedDarkColor()
    {
        Assert.Equal(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x2E), DreamineTheme.AppBackground);
    }

    /// <summary>
    /// \if KO
    /// <para>파란 강조 색상이 예상한 값인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the blue accent has the expected value.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void AccentBlue_IsExpectedColor()
    {
        Assert.Equal(Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF), DreamineTheme.AccentBlue);
    }

    /// <summary>
    /// \if KO
    /// <para>포커스 테두리 색상이 파란 강조 색상과 일치하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the focus-border color matches the blue accent.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void BorderFocus_MatchesAccentBlue()
    {
        Assert.Equal(DreamineTheme.AccentBlue, DreamineTheme.BorderFocus);
    }

    /// <summary>
    /// \if KO
    /// <para>주요 텍스트 색상이 흰색인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the primary text color is white.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void TextPrimary_IsWhite()
    {
        Assert.Equal(Color.White, DreamineTheme.TextPrimary);
    }

    /// <summary>
    /// \if KO
    /// <para>기본 모서리 반지름이 양수인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default corner radius is positive.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CornerRadius_IsPositive()
    {
        Assert.True(DreamineTheme.CornerRadius > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>호버 오버레이가 보이는 알파 값을 갖는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the hover overlay has a visible alpha value.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void HoverOverlay_HasNonZeroAlpha()
    {
        Assert.True(DreamineTheme.HoverOverlay.A > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>누름 오버레이의 알파가 호버 오버레이보다 큰지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the pressed-overlay alpha exceeds the hover-overlay alpha.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void PressOverlay_AlphaGreaterThanHover()
    {
        Assert.True(DreamineTheme.PressOverlay.A > DreamineTheme.HoverOverlay.A);
    }
}
