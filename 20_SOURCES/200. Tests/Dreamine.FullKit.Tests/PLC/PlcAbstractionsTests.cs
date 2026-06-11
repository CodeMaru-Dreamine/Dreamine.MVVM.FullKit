using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Abstractions.Results;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class PlcAbstractionsTests
{
    [Fact]
    public void PlcAddress_ToStringIncludesBitOffsetWhenPresent()
    {
        Assert.Equal("D10", new PlcAddress(PlcDeviceType.D, 10).ToString());
        Assert.Equal("M20.3", new PlcAddress(PlcDeviceType.M, 20, 3).ToString());
        Assert.True(new PlcAddress(PlcDeviceType.M, 20, 3).IsBitAddress);
    }

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

    [Fact]
    public void PlcResult_FailureRejectsEmptyMessage()
    {
        Assert.Throws<ArgumentException>(() => PlcResult.Failure(""));
        Assert.Throws<ArgumentException>(() => PlcResult<int>.Failure(" "));
    }
}
