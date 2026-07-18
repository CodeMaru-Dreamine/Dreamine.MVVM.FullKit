using System.Collections.ObjectModel;
using Dreamine.MVVM.ViewModels;
using Dreamine.UI.Wpf.Controls.Navigation;
using Dreamine.UI.Wpf.Controls.ViewRegion;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Wpf.ViewModels;

/// <summary>
/// \if KO
/// <para>Main View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates main view model functionality and related state.</para>
/// \endif
/// </summary>
public class MainViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>current View 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current view value.</para>
    /// \endif
    /// </summary>
    private object? _currentView;
    /// <summary>
    /// \if KO
    /// <para>Current View 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the current view value.</para>
    /// \endif
    /// </summary>
    public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }

    /// <summary>
    /// \if KO
    /// <para>Nav Buttons 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the nav buttons value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<ButtonData> NavButtons { get; } = new();

    /// <summary>
    /// \if KO
    /// <para>current Page Name 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current page name value.</para>
    /// \endif
    /// </summary>
    private string? _currentPageName;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MainViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MainViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="counterVm">
    /// \if KO
    /// <para>counter Vm에 사용할 <c>CounterViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CounterViewModel</c> value used for counter vm.</para>
    /// \endif
    /// </param>
    /// <param name="lightBulbVm">
    /// \if KO
    /// <para>light Bulb Vm에 사용할 <c>LightBulbViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbViewModel</c> value used for light bulb vm.</para>
    /// \endif
    /// </param>
    /// <param name="controlsVm">
    /// \if KO
    /// <para>controls Vm에 사용할 <c>ControlsViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ControlsViewModel</c> value used for controls vm.</para>
    /// \endif
    /// </param>
    /// <param name="popupVm">
    /// \if KO
    /// <para>popup Vm에 사용할 <c>PopupViewModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PopupViewModel</c> value used for popup vm.</para>
    /// \endif
    /// </param>
    public MainViewModel(CounterViewModel counterVm, LightBulbViewModel lightBulbVm, ControlsViewModel controlsVm, PopupViewModel popupVm)
    {
        // 탭 이름과 전환할 ViewModel을 한 곳에 모아 데이터 기반으로 NavButtons를 생성한다.
        var pages = new (string Content, object ViewModel)[]
        {
            ("Counter", counterVm),
            ("Light Bulb", lightBulbVm),
            ("Controls", controlsVm),
            ("Popup", popupVm),
        };

        // ViewSwitcher.NotifyShown/NotifyHidden 데모: 탭이 전환될 때마다 해당 ViewModel이
        // IActivatable/IVisibilityAware를 구현하면 Activate/Deactivate, OnShown/OnHidden이 호출된다.
        foreach (var page in pages)
            ViewSwitcher.RegisterViewModel(page.Content, page.ViewModel);

        for (int i = 0; i < pages.Length; i++)
        {
            var index = i;
            var name = pages[i].Content;
            var vm = pages[i].ViewModel;

            // VsNavigationHelper.Create는 Dreamine.UI.Wpf.Controls.Navigation에서 제공하는
            // 표준 ButtonData 팩토리로, 직접 ButtonData를 new해서 스타일을 반복 지정하지 않아도 된다.
            NavButtons.Add(VsNavigationHelper.Create(
                name,
                new RelayCommand(() => SwitchTo(name, vm, index)),
                isSelected: index == 0));
        }

        SwitchTo(pages[0].Content, pages[0].ViewModel, 0);
    }

    /// <summary>
    /// \if KO
    /// <para>Switch To 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the switch to operation.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="vm">
    /// \if KO
    /// <para>vm에 사용할 <c>object</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>object</c> value used for vm.</para>
    /// \endif
    /// </param>
    /// <param name="index">
    /// \if KO
    /// <para>index에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for index.</para>
    /// \endif
    /// </param>
    private void SwitchTo(string name, object vm, int index)
    {
        if (_currentPageName != null)
            ViewSwitcher.NotifyHidden(_currentPageName);

        CurrentView = vm;
        SelectNav(index);
        ViewSwitcher.NotifyShown(name);
        _currentPageName = name;
    }

    /// <summary>
    /// \if KO
    /// <para>Select Nav 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the select nav operation.</para>
    /// \endif
    /// </summary>
    /// <param name="index">
    /// \if KO
    /// <para>index에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for index.</para>
    /// \endif
    /// </param>
    private void SelectNav(int index)
    {
        for (int i = 0; i < NavButtons.Count; i++)
            NavButtons[i].IsSelected = i == index;
    }
}
