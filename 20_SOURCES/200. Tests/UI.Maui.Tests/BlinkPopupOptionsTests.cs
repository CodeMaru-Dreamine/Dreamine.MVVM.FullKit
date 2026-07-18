using Dreamine.UI.Maui.Popup;
using Xunit;

namespace Dreamine.UI.Maui.Tests;

/// <summary>
/// \if KO
/// <para>MAUI 깜빡임 팝업 옵션의 기본값과 속성 저장 동작을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies default values and property storage behavior of MAUI blinking-popup options.</para>
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
    /// <para>메시지의 기본값이 <see langword="null"/>인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the message defaults to <see langword="null"/>.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Message_NullByDefault()
    {
        var opt = new BlinkPopupOptions();
        Assert.Null(opt.Message);
    }

    /// <summary>
    /// \if KO
    /// <para>문자열과 기본 동작 속성의 설정 및 조회 왕복 동작을 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior for string and behavioral properties.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void AllStringProperties_SetAndGet_Roundtrip()
    {
        var opt = new BlinkPopupOptions
        {
            Title = "경고",
            Message = "오류 발생",
            OkText = "확인",
            CancelText = "취소",
            UseBlink = false,
            BlinkIntervalMs = 300
        };

        Assert.Equal("경고", opt.Title);
        Assert.Equal("오류 발생", opt.Message);
        Assert.Equal("확인", opt.OkText);
        Assert.Equal("취소", opt.CancelText);
        Assert.False(opt.UseBlink);
        Assert.Equal(300, opt.BlinkIntervalMs);
    }
}
