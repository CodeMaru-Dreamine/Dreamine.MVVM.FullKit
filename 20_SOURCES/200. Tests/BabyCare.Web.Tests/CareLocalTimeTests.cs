using BabyCare.Application;
using Xunit;

namespace BabyCare.Tests;

public class CareLocalTimeTests
{
    [Theory]
    [InlineData("2026-09-25T00:50")]
    [InlineData("2026-09-25T00:50:00")]
    [InlineData("2026-09-25T00:50:00.000")]
    public void BrowserLocalValuesKeepTheEnteredWallClockTime(string value)
    {
        Assert.True(CareLocalTime.TryParse(value, out var time));
        Assert.Equal(new DateTime(2026, 9, 25, 0, 50, 0), time);
        Assert.Equal(DateTimeKind.Unspecified, time.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-09-25T25:00")]
    [InlineData("2026-09-25T00:50Z")]
    public void InvalidOrZonedInputCannotSilentlySaveTheOldTime(string value)
        => Assert.False(CareLocalTime.TryParse(value, out _));
}
