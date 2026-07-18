using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>Plc Abstractions Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates plc abstractions tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PlcAbstractionsTests
{
    /// <summary>
    /// \if KO
    /// <para>Plc Address To String Includes Bit Offset When Present 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plc address to string includes bit offset when present operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void PlcAddress_ToStringIncludesBitOffsetWhenPresent()
    {
        Assert.Equal("D10", new PlcAddress(PlcDeviceType.D, 10).ToString());
        Assert.Equal("M20.3", new PlcAddress(PlcDeviceType.M, 20, 3).ToString());
        Assert.True(new PlcAddress(PlcDeviceType.M, 20, 3).IsBitAddress);
    }

    /// <summary>
    /// \if KO
    /// <para>Plc Result Success And Failure Expose State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plc result success and failure expose state operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void PlcResult_SuccessAndFailureExposeState()
    {
        var success = PlcResult.Success();
        var failure = PlcResult.Failure("bad", 9);
        var valueSuccess = PlcResult<string>.Success("ok");
        var valueFailure = PlcResult<string>.Failure("missing");

        Assert.True(success.IsSuccess);
        Assert.False(failure.IsSuccess);
        Assert.Equal(9, failure.ErrorCode);
        Assert.Equal("ok", valueSuccess.Value);
        Assert.False(valueFailure.IsSuccess);
        Assert.Null(valueFailure.Value);
    }

    /// <summary>
    /// \if KO
    /// <para>Plc Result Failure Rejects Empty Message 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plc result failure rejects empty message operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void PlcResult_FailureRejectsEmptyMessage()
    {
        Assert.Throws<ArgumentException>(() => PlcResult.Failure(""));
        Assert.Throws<ArgumentException>(() => PlcResult<int>.Failure(" "));
    }
}
