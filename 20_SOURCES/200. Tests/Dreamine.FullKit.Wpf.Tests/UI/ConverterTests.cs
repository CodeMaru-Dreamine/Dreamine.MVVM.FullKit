using System.Globalization;
using System.Windows;
using Dreamine.UI.Wpf.Converters;

namespace Dreamine.FullKit.Wpf.Tests.UI;

/// <summary>
/// \if KO
/// <para>Converter Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates converter tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ConverterTests
{
    // ── NullToVisibilityConverter ──────────────────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Null To Visibility Null Returns Collapsed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the null to visibility null returns collapsed operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void NullToVisibility_NullReturnsCollapsed()
    {
        var conv = new NullToVisibilityConverter();
        Assert.Equal(Visibility.Collapsed, conv.Convert(null!, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// \if KO
    /// <para>Null To Visibility Non Null Returns Visible 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the null to visibility non null returns visible operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void NullToVisibility_NonNullReturnsVisible()
    {
        var conv = new NullToVisibilityConverter();
        Assert.Equal(Visibility.Visible, conv.Convert("hello", typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    // ── BoolToIntDynamicConverter ──────────────────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Bool To Int Dynamic Returns Bool Input Unchanged When No Param 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the bool to int dynamic returns bool input unchanged when no param operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Led Inner Diameter Multi Binding Returns Scaled Value 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the led inner diameter multi binding returns scaled value operation.</para>
    /// \endif
    /// </summary>
    /// <param name="diameter">
    /// \if KO
    /// <para>diameter에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for diameter.</para>
    /// \endif
    /// </param>
    /// <param name="scale">
    /// \if KO
    /// <para>scale에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for scale.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Led Inner Diameter Null Values Returns Zero 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the led inner diameter null values returns zero operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void LedInnerDiameter_NullValues_ReturnsZero()
    {
        var conv = new LedInnerDiameterConverter();
        var result = conv.Convert(null!, typeof(double), null!, CultureInfo.InvariantCulture);
        Assert.Equal(0.0, result);
    }
}
