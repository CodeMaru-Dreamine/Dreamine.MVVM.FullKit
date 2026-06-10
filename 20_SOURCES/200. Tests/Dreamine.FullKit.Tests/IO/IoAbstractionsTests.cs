using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Abstractions.Options;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Abstractions.Enums;

namespace Dreamine.FullKit.Tests.IO;

public sealed class IoAbstractionsTests
{
    [Fact]
    public void IoResult_SuccessAndFailureExposeState()
    {
        var success = IoResult.Success();
        var failure = IoResult.Failure("bad", 7);
        var valueSuccess = IoResult<int>.Success(42);
        var valueFailure = IoResult<int>.Failure("no value");

        Assert.True(success.IsSuccess);
        Assert.False(failure.IsSuccess);
        Assert.Equal(7, failure.ErrorCode);
        Assert.Equal(42, valueSuccess.Value);
        Assert.False(valueFailure.IsSuccess);
        Assert.Equal(0, valueFailure.Value);
    }

    [Fact]
    public void PointRecords_PreserveValueSemantics()
    {
        Assert.Equal(new IoPoint(1, 2, "Input"), new IoPoint(1, 2, "Input"));
        Assert.Equal(new AnalogIoPoint(1, 3, "Pressure", "bar"), new AnalogIoPoint(1, 3, "Pressure", "bar"));
    }

    [Fact]
    public void IoConnectionOptions_ExposeDefaults()
    {
        var options = new IoConnectionOptions();

        Assert.Equal(IoProvider.Generic, options.Provider);
        Assert.Equal(0, options.DeviceIndex);
        Assert.Null(options.Name);
        Assert.Empty(options.Properties);
    }
}
