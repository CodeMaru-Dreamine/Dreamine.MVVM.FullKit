using Dreamine.UI.Maui.Popup;
using Xunit;

namespace Dreamine.UI.Maui.Tests;

public class DreamineDialogResultTests
{
    [Theory]
    [InlineData(DreamineDialogResult.None)]
    [InlineData(DreamineDialogResult.OK)]
    [InlineData(DreamineDialogResult.Cancel)]
    [InlineData(DreamineDialogResult.Yes)]
    [InlineData(DreamineDialogResult.No)]
    public void AllResultValues_AreDefined(DreamineDialogResult result)
    {
        Assert.True(Enum.IsDefined(typeof(DreamineDialogResult), result));
    }

    [Fact]
    public void EnumHasFiveValues()
    {
        Assert.Equal(5, Enum.GetValues<DreamineDialogResult>().Length);
    }

    [Fact]
    public void None_IsDefault()
    {
        var result = default(DreamineDialogResult);
        Assert.Equal(DreamineDialogResult.None, result);
    }
}
