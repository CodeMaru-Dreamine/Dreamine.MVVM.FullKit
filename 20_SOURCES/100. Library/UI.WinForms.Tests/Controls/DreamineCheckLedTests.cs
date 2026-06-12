using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

public class DreamineCheckLedTests
{
    [Fact]
    public void IsOn_DefaultIsTrue()
    {
        var led = new DreamineCheckLed();
        Assert.True(led.IsOn);
    }

    [Fact]
    public void IsOn_Toggle_PropertyChanges()
    {
        var led = new DreamineCheckLed { IsOn = false };
        Assert.False(led.IsOn);
        led.IsOn = true;
        Assert.True(led.IsOn);
    }

    [Fact]
    public void IsPulse_DefaultIsFalse()
    {
        var led = new DreamineCheckLed();
        Assert.False(led.IsPulse);
    }

    [Fact]
    public void IsPulse_SetTrue_PropertyChanges()
    {
        var led = new DreamineCheckLed { IsPulse = true };
        Assert.True(led.IsPulse);
        led.IsPulse = false;
        Assert.False(led.IsPulse);
    }

    [Fact]
    public void Corner_DefaultIsTopRight()
    {
        var led = new DreamineCheckLed();
        Assert.Equal(LedCorner.TopRight, led.Corner);
    }

    [Theory]
    [InlineData(LedCorner.TopLeft)]
    [InlineData(LedCorner.TopRight)]
    [InlineData(LedCorner.BottomLeft)]
    [InlineData(LedCorner.BottomRight)]
    public void Corner_AllValues_Accepted(LedCorner corner)
    {
        var led = new DreamineCheckLed { Corner = corner };
        Assert.Equal(corner, led.Corner);
    }

    [Fact]
    public void Diameter_DefaultIsPositive()
    {
        var led = new DreamineCheckLed();
        Assert.True(led.Diameter > 0);
    }

    [Theory]
    [InlineData(10f)]
    [InlineData(24f)]
    [InlineData(42f)]
    public void Diameter_SetValue_Roundtrip(float d)
    {
        var led = new DreamineCheckLed { Diameter = d };
        Assert.Equal(d, led.Diameter);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var led = new DreamineCheckLed { IsPulse = true };
        var ex = Record.Exception(() => led.Dispose());
        Assert.Null(ex);
    }
}
