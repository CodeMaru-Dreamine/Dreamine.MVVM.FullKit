using Dreamine.MVVM.Interfaces;
using Dreamine.UI.Wpf.Controls.ViewRegion;

namespace Dreamine.FullKit.Wpf.Tests.UI;

public sealed class ViewSwitcherTests
{
    [Fact]
    public void RegisterAndNotifyShown_CallsActivateAndOnShown()
    {
        var vm = new TrackingViewModel();
        ViewSwitcher.RegisterViewModel("TestView", vm);

        ViewSwitcher.NotifyShown("TestView");

        Assert.True(vm.Activated);
        Assert.True(vm.Shown);
    }

    [Fact]
    public void NotifyHidden_CallsOnHiddenAndDeactivate()
    {
        var vm = new TrackingViewModel();
        ViewSwitcher.RegisterViewModel("TestView2", vm);

        ViewSwitcher.NotifyShown("TestView2");
        ViewSwitcher.NotifyHidden("TestView2");

        Assert.True(vm.Hidden);
        Assert.True(vm.Deactivated);
    }

    [Fact]
    public void NotifyShown_UnregisteredKey_DoesNotThrow()
    {
        var ex = Record.Exception(() => ViewSwitcher.NotifyShown("NonExistentView_xyz"));
        Assert.Null(ex);
    }

    [Fact]
    public void RegisterViewModel_OverwritesPreviousEntry()
    {
        var vm1 = new TrackingViewModel();
        var vm2 = new TrackingViewModel();
        ViewSwitcher.RegisterViewModel("OverwriteView", vm1);
        ViewSwitcher.RegisterViewModel("OverwriteView", vm2);

        ViewSwitcher.NotifyShown("OverwriteView");

        Assert.False(vm1.Activated);
        Assert.True(vm2.Activated);
    }

    private sealed class TrackingViewModel : IActivatable, IVisibilityAware
    {
        public bool Activated { get; private set; }
        public bool Deactivated { get; private set; }
        public bool Shown { get; private set; }
        public bool Hidden { get; private set; }

        public void Activate() => Activated = true;
        public void Deactivate() => Deactivated = true;
        public void OnShown() => Shown = true;
        public void OnHidden() => Hidden = true;
    }
}
