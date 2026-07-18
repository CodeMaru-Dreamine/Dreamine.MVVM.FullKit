using Dreamine.UI.Blazor;
using Xunit;

namespace Dreamine.UI.Blazor.Tests;

/// <summary>
/// \if KO
/// <para>Blazor 깜빡임 팝업 옵션의 기본값과 속성 저장 동작을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies default values and property storage behavior of Blazor blinking-popup options.</para>
/// \endif
/// </summary>
public class BlinkPopupOptionsTests
{
    /// <summary>
    /// \if KO
    /// <para>기본적으로 깜빡임이 활성화되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that blinking is enabled by default.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void UseBlink_DefaultIsTrue()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.UseBlink);
    }

    /// <summary>
    /// \if KO
    /// <para>첫 번째 기본 색상이 설정되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the first default color is configured.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Color1_DefaultIsSet()
    {
        var opt = new BlinkPopupOptions();
        Assert.False(string.IsNullOrEmpty(opt.Color1));
    }

    /// <summary>
    /// \if KO
    /// <para>두 번째 기본 색상이 설정되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the second default color is configured.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Color2_DefaultIsSet()
    {
        var opt = new BlinkPopupOptions();
        Assert.False(string.IsNullOrEmpty(opt.Color2));
    }

    /// <summary>
    /// \if KO
    /// <para>기본 전경색이 설정되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default foreground color is configured.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ForegroundColor_DefaultIsSet()
    {
        var opt = new BlinkPopupOptions();
        Assert.False(string.IsNullOrEmpty(opt.ForegroundColor));
    }

    /// <summary>
    /// \if KO
    /// <para>기본 깜빡임 간격이 양수인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default blink interval is positive.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void BlinkIntervalMs_DefaultIsPositive()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.BlinkIntervalMs > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>제목의 기본값이 <see langword="null"/>인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the title defaults to <see langword="null"/>.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Title_NullByDefault()
    {
        var opt = new BlinkPopupOptions();
        Assert.Null(opt.Title);
    }

    /// <summary>
    /// \if KO
    /// <para>모든 옵션 속성의 설정 및 조회 왕복 동작을 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior for every option property.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void AllProperties_SetAndGet_Roundtrip()
    {
        var opt = new BlinkPopupOptions
        {
            Title = "경고",
            Message = "오류가 발생했습니다.",
            OkText = "확인",
            CancelText = "취소",
            UseBlink = false,
            Color1 = "#FF0000",
            Color2 = "#880000",
            ForegroundColor = "#FFFFFF",
            BlinkIntervalMs = 300
        };

        Assert.Equal("경고", opt.Title);
        Assert.Equal("오류가 발생했습니다.", opt.Message);
        Assert.Equal("확인", opt.OkText);
        Assert.Equal("취소", opt.CancelText);
        Assert.False(opt.UseBlink);
        Assert.Equal("#FF0000", opt.Color1);
        Assert.Equal("#880000", opt.Color2);
        Assert.Equal("#FFFFFF", opt.ForegroundColor);
        Assert.Equal(300, opt.BlinkIntervalMs);
    }
}
