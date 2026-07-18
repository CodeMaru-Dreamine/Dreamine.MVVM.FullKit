using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace Dreamine.FullKit.Tests.ViewModels;

/// <summary>
/// \if KO
/// <para>Counter View Model Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates counter view model tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CounterViewModelTests
{
    /// <summary>
    /// \if KO
    /// <para>Sut 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the sut value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Sut 작업에서 생성한 <c>CounterViewModel</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CounterViewModel</c> result produced by the create sut operation.</para>
    /// \endif
    /// </returns>
    private static CounterViewModel CreateSut() =>
        new CounterViewModel(new CounterEvent(new CounterService()));

    /// <summary>
    /// \if KO
    /// <para>Increment Increases Count By One 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the increment increases count by one operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Increment_IncreasesCountByOne()
    {
        var sut = CreateSut();
        sut.IncrementCommand.Execute(null);
        Assert.Equal(1, sut.Count);
    }

    /// <summary>
    /// \if KO
    /// <para>Reset Returns Count To Zero 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset returns count to zero operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Reset_ReturnsCountToZero()
    {
        var sut = CreateSut();
        sut.IncrementCommand.Execute(null);
        sut.IncrementCommand.Execute(null);
        sut.ResetCommand.Execute(null);
        Assert.Equal(0, sut.Count);
    }

    /// <summary>
    /// \if KO
    /// <para>Increment Adds Log Entry 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the increment adds log entry operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Increment_AddsLogEntry()
    {
        var sut = CreateSut();
        var initialCount = sut.Logs.Count;
        sut.IncrementCommand.Execute(null);
        Assert.Equal(initialCount + 1, sut.Logs.Count);
        Assert.Contains("1", sut.Logs[0].Message); // newest-first
    }

    /// <summary>
    /// \if KO
    /// <para>Reset Adds Log Entry 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset adds log entry operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Reset_AddsLogEntry()
    {
        var sut = CreateSut();
        sut.IncrementCommand.Execute(null);
        var countBeforeReset = sut.Logs.Count;
        sut.ResetCommand.Execute(null);
        Assert.Equal(countBeforeReset + 1, sut.Logs.Count);
        Assert.Contains("reset", sut.Logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }
}
