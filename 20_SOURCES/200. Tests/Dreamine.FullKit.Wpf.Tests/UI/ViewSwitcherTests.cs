using Dreamine.MVVM.Interfaces;
using Dreamine.UI.Wpf.Controls.ViewRegion;

namespace Dreamine.FullKit.Wpf.Tests.UI;

/// <summary>
/// \if KO
/// <para>View Switcher Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates view switcher tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ViewSwitcherTests
{
    /// <summary>
    /// \if KO
    /// <para>Register And Notify Shown Calls Activate And On Shown 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register and notify shown calls activate and on shown operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void RegisterAndNotifyShown_CallsActivateAndOnShown()
    {
        var vm = new TrackingViewModel();
        ViewSwitcher.RegisterViewModel("TestView", vm);

        ViewSwitcher.NotifyShown("TestView");

        Assert.True(vm.Activated);
        Assert.True(vm.Shown);
    }

    /// <summary>
    /// \if KO
    /// <para>Notify Hidden Calls On Hidden And Deactivate 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the notify hidden calls on hidden and deactivate operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Notify Shown Unregistered Key Does Not Throw 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the notify shown unregistered key does not throw operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void NotifyShown_UnregisteredKey_DoesNotThrow()
    {
        var ex = Record.Exception(() => ViewSwitcher.NotifyShown("NonExistentView_xyz"));
        Assert.Null(ex);
    }

    /// <summary>
    /// \if KO
    /// <para>Register View Model Overwrites Previous Entry 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register view model overwrites previous entry operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Tracking View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates tracking view model functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TrackingViewModel : IActivatable, IVisibilityAware
    {
        /// <summary>
        /// \if KO
        /// <para>Activated 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the activated value.</para>
        /// \endif
        /// </summary>
        public bool Activated { get; private set; }
        /// <summary>
        /// \if KO
        /// <para>Deactivated 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the deactivated value.</para>
        /// \endif
        /// </summary>
        public bool Deactivated { get; private set; }
        /// <summary>
        /// \if KO
        /// <para>Shown 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the shown value.</para>
        /// \endif
        /// </summary>
        public bool Shown { get; private set; }
        /// <summary>
        /// \if KO
        /// <para>Hidden 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the hidden value.</para>
        /// \endif
        /// </summary>
        public bool Hidden { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Activate 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the activate operation.</para>
        /// \endif
        /// </summary>
        public void Activate() => Activated = true;
        /// <summary>
        /// \if KO
        /// <para>Deactivate 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the deactivate operation.</para>
        /// \endif
        /// </summary>
        public void Deactivate() => Deactivated = true;
        /// <summary>
        /// \if KO
        /// <para>Shown 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Handles the shown event or state change.</para>
        /// \endif
        /// </summary>
        public void OnShown() => Shown = true;
        /// <summary>
        /// \if KO
        /// <para>Hidden 이벤트 또는 상태 변경을 처리합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Handles the hidden event or state change.</para>
        /// \endif
        /// </summary>
        public void OnHidden() => Hidden = true;
    }
}
