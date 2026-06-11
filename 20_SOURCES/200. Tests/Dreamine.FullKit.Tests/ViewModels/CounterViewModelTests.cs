using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace Dreamine.FullKit.Tests.ViewModels;

public sealed class CounterViewModelTests
{
    private static CounterViewModel CreateSut() =>
        new CounterViewModel(new CounterService());

    [Fact]
    public void Increment_IncreasesCountByOne()
    {
        var sut = CreateSut();
        sut.IncrementCommand.Execute(null);
        Assert.Equal(1, sut.Count);
    }

    [Fact]
    public void Reset_ReturnsCountToZero()
    {
        var sut = CreateSut();
        sut.IncrementCommand.Execute(null);
        sut.IncrementCommand.Execute(null);
        sut.ResetCommand.Execute(null);
        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void Increment_AddsLogEntry()
    {
        var sut = CreateSut();
        var initialCount = sut.Logs.Count;
        sut.IncrementCommand.Execute(null);
        Assert.Equal(initialCount + 1, sut.Logs.Count);
        Assert.Contains("1", sut.Logs[0].Message); // newest-first
    }

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
