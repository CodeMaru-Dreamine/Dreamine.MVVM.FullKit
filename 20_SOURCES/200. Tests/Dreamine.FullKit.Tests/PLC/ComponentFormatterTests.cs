using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Mitsubishi.MxComponent.Devices;
using Dreamine.PLC.Mitsubishi.MxComponent.Options;
using Dreamine.PLC.Omron.CxComponent.Devices;
using Dreamine.PLC.Omron.CxComponent.Options;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class ComponentFormatterTests
{
    [Fact]
    public void MitsubishiMxFormatter_FormatsDecimalAndHexDevices()
    {
        Assert.Equal("D100", MitsubishiMxDeviceNameFormatter.Format(new PlcAddress(PlcDeviceType.D, 100)));
        Assert.Equal("X10", MitsubishiMxDeviceNameFormatter.Format(new PlcAddress(PlcDeviceType.X, 16)));
        Assert.Equal("M5.2", MitsubishiMxDeviceNameFormatter.Format(new PlcAddress(PlcDeviceType.M, 5, 2)));
        Assert.Equal("D101", MitsubishiMxDeviceNameFormatter.FormatOffset(new PlcAddress(PlcDeviceType.D, 100), 1));
    }

    [Fact]
    public void OmronCxFormatter_FormatsMappedAreas()
    {
        Assert.Equal("D100", OmronCxAddressNameFormatter.Format(new PlcAddress(PlcDeviceType.D, 100)));
        Assert.Equal("W10", OmronCxAddressNameFormatter.Format(new PlcAddress(PlcDeviceType.M, 10)));
        Assert.Equal("CIO1.3", OmronCxAddressNameFormatter.Format(new PlcAddress(PlcDeviceType.X, 1, 3)));
        Assert.Equal("H7", OmronCxAddressNameFormatter.FormatOffset(new PlcAddress(PlcDeviceType.R, 5), 2));
    }

    [Fact]
    public void ComponentOptions_ExposeDefaults()
    {
        Assert.False(string.IsNullOrWhiteSpace(new MitsubishiMxComponentOptions().ProgId));
        Assert.Equal("OMRON.Compolet.CJ2Compolet", new OmronCxComponentOptions().ProgId);
    }
}
