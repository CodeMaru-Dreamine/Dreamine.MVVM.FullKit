using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

public class DreamineRadioButtonTests
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
            var rb = new DreamineRadioButton();
            Assert.False(rb.IsChecked);
        });

    [Fact]
    public void IsChecked_SetToTrue_PropertyChanges()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton { IsChecked = true };
            Assert.True(rb.IsChecked);
        });

    [Fact]
    public void IsChecked_SetTrue_RaisesCheckedChanged()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            int fired = 0;
            rb.CheckedChanged += (_, _) => fired++;
            rb.IsChecked = true;
            Assert.Equal(1, fired);
        });

    [Fact]
    public void GroupName_DefaultIsEmpty()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            Assert.Equal(string.Empty, rb.GroupName);
        });

    [Fact]
    public void GroupName_SetAndGet_Roundtrip()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton { GroupName = "group1" };
            Assert.Equal("group1", rb.GroupName);
        });

    [Fact]
    public void SameGroup_SelectingOne_UnchecksOther()
        => Sta(() =>
        {
            using var container = new Panel();
            var rb1 = new DreamineRadioButton { GroupName = "g", IsChecked = true };
            var rb2 = new DreamineRadioButton { GroupName = "g" };
            container.Controls.Add(rb1);
            container.Controls.Add(rb2);
            rb2.IsChecked = true;
            Assert.False(rb1.IsChecked);
            Assert.True(rb2.IsChecked);
        });

    [Fact]
    public void DifferentGroups_SelectingOne_DoesNotAffectOther()
        => Sta(() =>
        {
            using var container = new Panel();
            var rb1 = new DreamineRadioButton { GroupName = "g1", IsChecked = true };
            var rb2 = new DreamineRadioButton { GroupName = "g2" };
            container.Controls.Add(rb1);
            container.Controls.Add(rb2);
            rb2.IsChecked = true;
            Assert.True(rb1.IsChecked);
            Assert.True(rb2.IsChecked);
        });

    [Fact]
    public void NoParent_SettingChecked_DoesNotThrow()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            var ex = Record.Exception(() => rb.IsChecked = true);
            Assert.Null(ex);
        });
}
