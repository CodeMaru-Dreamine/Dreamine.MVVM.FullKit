using Dreamine.UI.Blazor;
using Xunit;

namespace Dreamine.UI.Blazor.Tests;

public class BlinkPopupOptionsTests
{
    [Fact]
    public void UseBlink_DefaultIsTrue()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.UseBlink);
    }

    [Fact]
    public void Color1_DefaultIsSet()
    {
        var opt = new BlinkPopupOptions();
        Assert.False(string.IsNullOrEmpty(opt.Color1));
    }

    [Fact]
    public void Color2_DefaultIsSet()
    {
        var opt = new BlinkPopupOptions();
        Assert.False(string.IsNullOrEmpty(opt.Color2));
    }

    [Fact]
    public void ForegroundColor_DefaultIsSet()
    {
        var opt = new BlinkPopupOptions();
        Assert.False(string.IsNullOrEmpty(opt.ForegroundColor));
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
