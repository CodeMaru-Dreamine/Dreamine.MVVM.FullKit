using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Abstractions.Options;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Abstractions.Enums;

namespace Dreamine.FullKit.Tests.IO;

/// <summary>
/// \if KO
/// <para>Io Abstractions Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates io abstractions tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class IoAbstractionsTests
{
    /// <summary>
    /// \if KO
    /// <para>Io Result Success And Failure Expose State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the io result success and failure expose state operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Point Records Preserve Value Semantics 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the point records preserve value semantics operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void PointRecords_PreserveValueSemantics()
    {
        Assert.Equal(new IoPoint(1, 2, "Input"), new IoPoint(1, 2, "Input"));
        Assert.Equal(new AnalogIoPoint(1, 3, "Pressure", "bar"), new AnalogIoPoint(1, 3, "Pressure", "bar"));
    }

    /// <summary>
    /// \if KO
    /// <para>Io Connection Options Expose Defaults 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the io connection options expose defaults operation.</para>
    /// \endif
    /// </summary>
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
