using System.Runtime.ExceptionServices;
using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

public class DreamineCheckBoxTests
{
    private static void Sta(Action action)
    {
        Exception? ex = null;
        var t = new Thread(() => { try { action(); } catch (Exception e) { ex = e; } });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (ex is not null) ExceptionDispatchInfo.Capture(ex).Throw();
    }

    [Fact]
    public void IsChecked_DefaultIsFalse()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox();
            Assert.False(cb.IsChecked);
        });

    [Fact]
    public void IsChecked_SetToTrue_PropertyChanges()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox { IsChecked = true };
            Assert.True(cb.IsChecked);
        });

    [Fact]
    public void IsChecked_Toggle_RaisesCheckedChanged()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox();
            int fired = 0;
            cb.CheckedChanged += (_, _) => fired++;
            cb.IsChecked = true;
            cb.IsChecked = false;
            Assert.Equal(2, fired);
        });

    [Fact]
    public void IsChecked_SameValue_DoesNotRaiseEvent()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox { IsChecked = false };
            int fired = 0;
            cb.CheckedChanged += (_, _) => fired++;
            cb.IsChecked = false;
            Assert.Equal(0, fired);
        });

    [Fact]
    public void Content_SetAndGet_Roundtrip()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox { Content = "Accept terms" };
            Assert.Equal("Accept terms", cb.Content);
        });
}
