using System.Drawing;
using System.Windows.Input;
using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

public class DreamineButtonTests
{
    [Fact]
    public void DefaultContent_IsEmpty()
    {
        var btn = new DreamineButton();
        Assert.Equal(string.Empty, btn.Content);
    }

    [Fact]
    public void Content_SetAndGet_Roundtrip()
    {
        var btn = new DreamineButton { Content = "Click Me" };
        Assert.Equal("Click Me", btn.Content);
    }

    [Fact]
    public void ShineColor_DefaultIsEmpty()
    {
        var btn = new DreamineButton();
        Assert.Equal(Color.Empty, btn.ShineColor);
    }

    [Fact]
    public void ShineColor_SetAndGet_Roundtrip()
    {
        var btn = new DreamineButton { ShineColor = Color.Blue };
        Assert.Equal(Color.Blue, btn.ShineColor);
    }

    [Fact]
    public void IsSelected_DefaultIsFalse()
    {
        var btn = new DreamineButton();
        Assert.False(btn.IsSelected);
    }

    [Fact]
    public void IsSelected_ToggleWorks()
    {
        var btn = new DreamineButton { IsSelected = true };
        Assert.True(btn.IsSelected);
        btn.IsSelected = false;
        Assert.False(btn.IsSelected);
    }

    [Fact]
    public void CornerRadius_DefaultIsPositive()
    {
        var btn = new DreamineButton();
        Assert.True(btn.CornerRadius > 0);
    }

    [Fact]
    public void Command_Execute_InvokesAction()
    {
        bool executed = false;
        var btn = new DreamineButton
        {
            Command = new RelayTestCommand(_ => executed = true)
        };
        btn.Command.Execute(null);
        Assert.True(executed);
    }

    [Fact]
    public void CommandParameter_SetAndGet_Roundtrip()
    {
        var param = new object();
        var btn = new DreamineButton { CommandParameter = param };
        Assert.Same(param, btn.CommandParameter);
    }

    [Fact]
    public void DefaultSize_IsReasonable()
    {
        var btn = new DreamineButton();
        Assert.True(btn.Width > 0 && btn.Height > 0);
    }

    // Simple test ICommand
    private sealed class RelayTestCommand(Action<object?> execute) : ICommand
    {
        event EventHandler? ICommand.CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? p) => true;
        public void Execute(object? p) => execute(p);
    }
}
