using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

/// <summary>
/// \if KO
/// <para>Dreamine LED의 켜짐, 맥동, 모서리, 지름 및 해제 동작을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies Dreamine LED on, pulse, corner, diameter, and disposal behavior.</para>
/// \endif
/// </summary>
public class DreamineCheckLedTests
{
    /// <summary>
    /// \if KO
    /// <para>LED가 기본적으로 켜져 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the LED is on by default.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsOn_DefaultIsTrue()
    {
        var led = new DreamineCheckLed();
        Assert.True(led.IsOn);
    }

    /// <summary>
    /// \if KO
    /// <para>켜짐 상태를 양방향으로 전환할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the on state can be toggled in both directions.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsOn_Toggle_PropertyChanges()
    {
        var led = new DreamineCheckLed { IsOn = false };
        Assert.False(led.IsOn);
        led.IsOn = true;
        Assert.True(led.IsOn);
    }

    /// <summary>
    /// \if KO
    /// <para>맥동 상태 기본값이 거짓인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that pulse state defaults to false.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsPulse_DefaultIsFalse()
    {
        var led = new DreamineCheckLed();
        Assert.False(led.IsPulse);
    }

    /// <summary>
    /// \if KO
    /// <para>맥동 상태를 설정하고 해제할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that pulse state can be enabled and disabled.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsPulse_SetTrue_PropertyChanges()
    {
        var led = new DreamineCheckLed { IsPulse = true };
        Assert.True(led.IsPulse);
        led.IsPulse = false;
        Assert.False(led.IsPulse);
    }

    /// <summary>
    /// \if KO
    /// <para>기본 모서리가 오른쪽 위인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default corner is top right.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Corner_DefaultIsTopRight()
    {
        var led = new DreamineCheckLed();
        Assert.Equal(LedCorner.TopRight, led.Corner);
    }

    /// <summary>
    /// \if KO
    /// <para>모든 정의된 모서리 값을 속성에 설정할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that every defined corner value can be assigned to the property.</para>
    /// \endif
    /// </summary>
    /// <param name="corner">
    /// \if KO
    /// <para>설정하고 검증할 모서리입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The corner to assign and verify.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>기본 LED 지름이 양수인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default LED diameter is positive.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Diameter_DefaultIsPositive()
    {
        var led = new DreamineCheckLed();
        Assert.True(led.Diameter > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>여러 지름 값의 설정 및 조회가 같은 값을 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior for multiple diameter values.</para>
    /// \endif
    /// </summary>
    /// <param name="d">
    /// \if KO
    /// <para>설정하고 검증할 지름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The diameter to assign and verify.</para>
    /// \endif
    /// </param>
    [Theory]
    [InlineData(10f)]
    [InlineData(24f)]
    [InlineData(42f)]
    public void Diameter_SetValue_Roundtrip(float d)
    {
        var led = new DreamineCheckLed { Diameter = d };
        Assert.Equal(d, led.Diameter);
    }

    /// <summary>
    /// \if KO
    /// <para>맥동 중인 LED를 예외 없이 해제할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a pulsing LED can be disposed without an exception.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var led = new DreamineCheckLed { IsPulse = true };
        var ex = Record.Exception(() => led.Dispose());
        Assert.Null(ex);
    }
}
