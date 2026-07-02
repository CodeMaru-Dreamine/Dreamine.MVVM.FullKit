using System.ComponentModel;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui.Views;

public partial class LightBulbPage : ContentView
{
    private readonly LightBulbViewModel _viewModel;
    private bool _refreshing;

    public LightBulbPage(LightBulbViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Refresh();
    }

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        _viewModel.ToggleCommand.Execute(null);
    }

    private void OnPowerChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_refreshing || _viewModel.IsOn == e.Value) return;
        _viewModel.ToggleCommand.Execute(null);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(Refresh);
    }

    private void Refresh()
    {
        _refreshing = true;
        PowerCheckBox.IsChecked = _viewModel.IsOn;
        Bulb.IsOn = _viewModel.IsOn;
        StatusLabel.Text = _viewModel.StatusText;
        CountLabel.Text = $"Toggled {_viewModel.ToggleCount}x";
        StatusLabel.TextColor = _viewModel.IsOn ? Color.FromArgb("#B7791F") : Color.FromArgb("#64748B");
        _refreshing = false;
    }
}
