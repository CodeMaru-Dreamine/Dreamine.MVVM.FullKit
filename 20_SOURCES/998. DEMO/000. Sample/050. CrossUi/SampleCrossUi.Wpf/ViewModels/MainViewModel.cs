using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Dreamine.MVVM.ViewModels;
using Dreamine.UI.Wpf.Controls.Navigation;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private object? _currentView;
    public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }

    public ObservableCollection<ButtonData> NavButtons { get; } = new();

    public MainViewModel(CounterViewModel counterVm, ControlsViewModel controlsVm, PopupViewModel popupVm)
    {
        var bg     = new SolidColorBrush(Color.FromRgb(13, 27, 62));
        var shine  = new SolidColorBrush(Color.FromRgb(30, 144, 255));

        var margin = new Thickness(6, 8, 6, 8);

        NavButtons.Add(new ButtonData
        {
            Content = "Counter",
            Background = bg, ShineColor = shine, Margin = margin,
            Command = new RelayCommand(() => { CurrentView = counterVm; SelectNav(0); })
        });
        NavButtons.Add(new ButtonData
        {
            Content = "Controls",
            Background = bg, ShineColor = shine, Margin = margin,
            Command = new RelayCommand(() => { CurrentView = controlsVm; SelectNav(1); })
        });
        NavButtons.Add(new ButtonData
        {
            Content = "Popup",
            Background = bg, ShineColor = shine, Margin = margin,
            Command = new RelayCommand(() => { CurrentView = popupVm; SelectNav(2); })
        });

        // default page
        CurrentView = counterVm;
        NavButtons[0].IsSelected = true;
    }

    private void SelectNav(int index)
    {
        for (int i = 0; i < NavButtons.Count; i++)
            NavButtons[i].IsSelected = i == index;
    }
}
