using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Core.Devices;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class DefaultPlcAddressParserTests
{
    [Theory]
    [InlineData("D100", PlcDeviceType.D, 100, null)]
    [InlineData("m10.2", PlcDeviceType.M, 10, 2)]
    [InlineData(" ZR4096.15 ", PlcDeviceType.ZR, 4096, 15)]
    public void Parse_ReturnsAddressForSupportedFormats(
        string text,
        PlcDeviceType expectedDeviceType,
        int expectedOffset,
        int? expectedBitOffset)
    {
        var result = new DefaultPlcAddressParser().Parse(text);

        Assert.True(result.IsSuccess);
        Assert.Equal(new PlcAddress(expectedDeviceType, expectedOffset, expectedBitOffset), result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Q10")]
    [InlineData("D")]
    [InlineData("D-1")]
    [InlineData("D10.16")]
    [InlineData("D10.1.2")]
    public void Parse_ReturnsFailureForInvalidFormats(string text)
    {
        var result = new DefaultPlcAddressParser().Parse(text);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void TryParse_ReturnsFalseAndDefaultAddressForInvalidText()
    {
        var parser = new DefaultPlcAddressParser();

        var success = parser.TryParse("invalid", out var address);

        Assert.False(success);
        Assert.Equal(default, address);
    }
}
