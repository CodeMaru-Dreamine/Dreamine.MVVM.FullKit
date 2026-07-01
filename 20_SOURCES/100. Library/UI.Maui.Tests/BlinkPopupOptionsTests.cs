using Dreamine.UI.Maui.Popup;
using Xunit;

namespace Dreamine.UI.Maui.Tests;

public class BlinkPopupOptionsTests
{
    [Fact]
    public void UseBlink_DefaultIsTrue()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.UseBlink);
    }

    [Fact]
    public void BlinkIntervalMs_DefaultIsPositive()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.BlinkIntervalMs > 0);
    }

    [Fact]
    public void Title_NullByDefault()
    {
        var opt = new BlinkPopupOptions();
        Assert.Null(opt.Title);
    }

    [Fact]
    public void Message_NullByDefault()
    {
        var opt = new BlinkPopupOptions();
        Assert.Null(opt.Message);
    }

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
