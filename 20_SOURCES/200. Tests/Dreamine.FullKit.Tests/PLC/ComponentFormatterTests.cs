using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Mitsubishi.MxComponent.Devices;
using Dreamine.PLC.Mitsubishi.MxComponent.Options;
using Dreamine.PLC.Omron.CxComponent.Devices;
using Dreamine.PLC.Omron.CxComponent.Options;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>Component Formatter Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates component formatter tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ComponentFormatterTests
{
    /// <summary>
    /// \if KO
    /// <para>Mitsubishi Mx Formatter Formats Decimal And Hex Devices 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the mitsubishi mx formatter formats decimal and hex devices operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void MitsubishiMxFormatter_FormatsDecimalAndHexDevices()
    {
        Assert.Equal("D100", MitsubishiMxDeviceNameFormatter.Format(new PlcAddress(PlcDeviceType.D, 100)));
        Assert.Equal("X10", MitsubishiMxDeviceNameFormatter.Format(new PlcAddress(PlcDeviceType.X, 16)));
        Assert.Equal("M5.2", MitsubishiMxDeviceNameFormatter.Format(new PlcAddress(PlcDeviceType.M, 5, 2)));
        Assert.Equal("D101", MitsubishiMxDeviceNameFormatter.FormatOffset(new PlcAddress(PlcDeviceType.D, 100), 1));
    }

    /// <summary>
    /// \if KO
    /// <para>Omron Cx Formatter Formats Mapped Areas 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the omron cx formatter formats mapped areas operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void OmronCxFormatter_FormatsMappedAreas()
    {
        Assert.Equal("D100", OmronCxAddressNameFormatter.Format(new PlcAddress(PlcDeviceType.D, 100)));
        Assert.Equal("W10", OmronCxAddressNameFormatter.Format(new PlcAddress(PlcDeviceType.M, 10)));
        Assert.Equal("CIO1.3", OmronCxAddressNameFormatter.Format(new PlcAddress(PlcDeviceType.X, 1, 3)));
        Assert.Equal("H7", OmronCxAddressNameFormatter.FormatOffset(new PlcAddress(PlcDeviceType.R, 5), 2));
    }

    /// <summary>
    /// \if KO
    /// <para>Component Options Expose Defaults 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the component options expose defaults operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ComponentOptions_ExposeDefaults()
    {
        Assert.False(string.IsNullOrWhiteSpace(new MitsubishiMxComponentOptions().ProgId));
        Assert.Equal("OMRON.Compolet.CJ2Compolet", new OmronCxComponentOptions().ProgId);
    }
}
