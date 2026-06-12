using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

public class DreamineExpanderTests
{
    [Fact]
    public void IsExpanded_DefaultIsTrue()
    {
        var exp = new DreamineExpander();
        Assert.True(exp.IsExpanded);
    }

    [Fact]
    public void IsExpanded_SetFalse_CollapsesHeight()
    {
        var exp = new DreamineExpander();
        int expandedH = exp.Height;
        exp.IsExpanded = false;
        Assert.True(exp.Height < expandedH);
    }

    [Fact]
    public void IsExpanded_RaisesExpandedChanged()
    {
        var exp = new DreamineExpander();
        int fired = 0;
        exp.ExpandedChanged += (_, _) => fired++;
        exp.IsExpanded = false;
        exp.IsExpanded = true;
        Assert.Equal(2, fired);
    }

    [Fact]
    public void Header_SetAndGet_Roundtrip()
    {
        var exp = new DreamineExpander { Header = "Section A" };
        Assert.Equal("Section A", exp.Header);
    }

    [Fact]
    public void Content_IsNotNull()
    {
        var exp = new DreamineExpander();
        Assert.NotNull(exp.Content);
    }

    [Fact]
    public void Collapse_ThenExpand_RestoresHeight()
    {
        var exp = new DreamineExpander();
        int original = exp.Height;
        exp.IsExpanded = false;
        exp.IsExpanded = true;
        Assert.Equal(original, exp.Height);
    }
}
