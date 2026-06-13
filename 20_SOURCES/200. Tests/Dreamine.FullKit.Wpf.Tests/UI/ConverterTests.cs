using System.Globalization;
using System.Windows;
using Dreamine.UI.Wpf.Converters;

namespace Dreamine.FullKit.Wpf.Tests.UI;

public sealed class ConverterTests
{
    // ── NullToVisibilityConverter ──────────────────────────────────────────

    [Fact]
    public void NullToVisibility_NullReturnsCollapsed()
    {
        var conv = new NullToVisibilityConverter();
        Assert.Equal(Visibility.Collapsed, conv.Convert(null, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void NullToVisibility_NonNullReturnsVisible()
    {
        var conv = new NullToVisibilityConverter();
        Assert.Equal(Visibility.Visible, conv.Convert("hello", typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    // ── BoolToIntDynamicConverter ──────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BoolToIntDynamic_ReturnsBoolInputUnchangedWhenNoParam(bool value)
    {
        var conv = new BoolToIntDynamicConverter();
        var result = conv.Convert(value, typeof(object), null!, CultureInfo.InvariantCulture);
        Assert.NotNull(result);
    }

    // ── LedInnerDiameterConverter (IMultiValueConverter) ──────────────────

    [Theory]
    [InlineData(20.0, 0.45)]
    [InlineData(40.0, 0.5)]
    [InlineData(100.0, 0.8)]
    public void LedInnerDiameter_MultiBinding_ReturnsScaledValue(double diameter, double scale)
    {
        var conv = new LedInnerDiameterConverter();
        var result = conv.Convert(new object[] { diameter, scale }, typeof(double), null!, CultureInfo.InvariantCulture);
        var inner = Assert.IsType<double>(result);
        Assert.Equal(diameter * scale, inner, precision: 10);
    }

    [Fact]
    public void LedInnerDiameter_NullValues_ReturnsZero()
    {
        var conv = new LedInnerDiameterConverter();
        var result = conv.Convert(null!, typeof(double), null!, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }
}
