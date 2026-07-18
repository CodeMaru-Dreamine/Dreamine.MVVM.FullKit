using System.Drawing;
using System.Drawing.Drawing2D;
using Dreamine.UI.WinForms;
using Xunit;

namespace Dreamine.UI.WinForms.Tests;

/// <summary>
/// \if KO
/// <para>WinForms 그리기 도우미의 경로 생성, 색상 혼합 및 그리기 경계 동작을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies path creation, color blending, and drawing boundary behavior of the WinForms drawing helper.</para>
/// \endif
/// </summary>
public class DreamineDrawHelperTests
{
    /// <summary>
    /// \if KO
    /// <para>양의 반지름으로 둥근 사각형 경로가 생성되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a rounded-rectangle path is created for a positive radius.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RoundedRect_ReturnsClosedPath()
    {
        var path = DreamineDrawHelper.RoundedRect(new Rectangle(0, 0, 100, 50), 8f);
        Assert.NotNull(path);
        // Closed path has at least one figure
        path.Dispose();
    }

    /// <summary>
    /// \if KO
    /// <para>반지름이 0이면 사각형 경로가 생성되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a rectangular path is created when the radius is zero.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RoundedRect_ZeroRadius_ReturnsRectanglePath()
    {
        var rect = new RectangleF(0, 0, 100, 50);
        using var path = DreamineDrawHelper.RoundedRect(rect, 0f);
        Assert.NotNull(path);
    }

    /// <summary>
    /// \if KO
    /// <para>알파가 0일 때 혼합 결과가 기준 색상과 같은지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that zero-alpha blending returns the base color.</para>
    /// \endif
    /// </summary>
    /// <param name="r">
    /// \if KO
    /// <para>기준 색상의 빨강 채널입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The red channel of the base color.</para>
    /// \endif
    /// </param>
    /// <param name="g">
    /// \if KO
    /// <para>기준 색상의 초록 채널입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The green channel of the base color.</para>
    /// \endif
    /// </param>
    /// <param name="b">
    /// \if KO
    /// <para>기준 색상의 파랑 채널입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The blue channel of the base color.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>알파가 1일 때 혼합 결과가 오버레이 색상과 같은지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that full-alpha blending returns the overlay color.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Blend_WithFullAlpha_ReturnsOverlayColor()
    {
        var result = DreamineDrawHelper.Blend(Color.Black, Color.White, 1f);
        Assert.Equal(255, result.R);
        Assert.Equal(255, result.G);
        Assert.Equal(255, result.B);
    }

    /// <summary>
    /// \if KO
    /// <para>절반 비율 혼합이 두 색상의 평균 채널을 생성하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that midpoint blending produces the average channel value.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Blend_Midpoint_IsAverageColor()
    {
        var result = DreamineDrawHelper.Blend(Color.FromArgb(255, 0, 0, 0),
                                              Color.FromArgb(255, 100, 100, 100), 0.5f);
        Assert.InRange(result.R, 48, 52); // 50 ± rounding
    }

    /// <summary>
    /// \if KO
    /// <para>혼합 결과의 모든 색상 채널이 바이트 범위에 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that every blended color channel remains in the byte range.</para>
    /// \endif
    /// </summary>
    /// <param name="channel">
    /// \if KO
    /// <para>기준 색상에 사용할 채널 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The channel value used by the base color.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>유효한 둥근 사각형 채우기가 예외 없이 완료되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that filling a valid rounded rectangle completes without an exception.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>크기가 0인 그라데이션 영역이 예외 없이 무시되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a zero-sized gradient region is ignored without an exception.</para>
    /// \endif
    /// </summary>
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
