using Dreamine.UI.Blazor;
using Xunit;

namespace Dreamine.UI.Blazor.Tests;

public class DreamineCheckLedCornerTests
{
    [Theory]
    [InlineData(DreamineCheckLedCorner.TopLeft)]
    [InlineData(DreamineCheckLedCorner.TopRight)]
    [InlineData(DreamineCheckLedCorner.BottomLeft)]
    [InlineData(DreamineCheckLedCorner.BottomRight)]
    public void AllCornerValues_AreDefined(DreamineCheckLedCorner corner)
    {
        Assert.True(Enum.IsDefined(typeof(DreamineCheckLedCorner), corner));
    }

    [Fact]
    public void EnumHasFourValues()
    {
        Assert.Equal(4, Enum.GetValues<DreamineCheckLedCorner>().Length);
    }
}
