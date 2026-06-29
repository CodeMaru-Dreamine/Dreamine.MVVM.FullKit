using System.Collections.ObjectModel;
using Dreamine.MVVM.ViewModels;
using Dreamine.UI.Wpf.Controls.Navigation;
using Dreamine.UI.Wpf.Controls.ViewRegion;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private object? _currentView;
    public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }

    public ObservableCollection<ButtonData> NavButtons { get; } = new();

    private string? _currentPageName;

    public MainViewModel(CounterViewModel counterVm, ControlsViewModel controlsVm, PopupViewModel popupVm)
    {
        // 탭 이름과 전환할 ViewModel을 한 곳에 모아 데이터 기반으로 NavButtons를 생성한다.
        var pages = new (string Content, object ViewModel)[]
        {
            ("Counter", counterVm),
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

    private void SwitchTo(string name, object vm, int index)
    {
        if (_currentPageName != null)
            ViewSwitcher.NotifyHidden(_currentPageName);

        CurrentView = vm;
        SelectNav(index);
        ViewSwitcher.NotifyShown(name);
        _currentPageName = name;
    }

    private void SelectNav(int index)
    {
        for (int i = 0; i < NavButtons.Count; i++)
            NavButtons[i].IsSelected = i == index;
    }
}
