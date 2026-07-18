using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Core.Devices;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>Default Plc Address Parser Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates default plc address parser tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DefaultPlcAddressParserTests
{
    /// <summary>
    /// \if KO
    /// <para>Parse Returns Address For Supported Formats 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse returns address for supported formats operation.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="expectedDeviceType">
    /// \if KO
    /// <para>expected Device Type에 사용할 <c>PlcDeviceType</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PlcDeviceType</c> value used for expected device type.</para>
    /// \endif
    /// </param>
    /// <param name="expectedOffset">
    /// \if KO
    /// <para>expected Offset에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for expected offset.</para>
    /// \endif
    /// </param>
    /// <param name="expectedBitOffset">
    /// \if KO
    /// <para>expected Bit Offset에 사용할 <c>int?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> value used for expected bit offset.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Parse Returns Failure For Invalid Formats 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse returns failure for invalid formats operation.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Parse Returns False And Default Address For Invalid Text 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to parse returns false and default address for invalid text and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void TryParse_ReturnsFalseAndDefaultAddressForInvalidText()
    {
        var parser = new DefaultPlcAddressParser();

        var success = parser.TryParse("invalid", out var address);

        Assert.False(success);
        Assert.Equal(default, address);
    }
}
