using System.Collections.Specialized;
using System.ComponentModel;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui.Views;

public partial class CounterPage : ContentView
{
    private readonly CounterViewModel _viewModel;

    public CounterPage(CounterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnPropertyChanged;
        _viewModel.Logs.CollectionChanged += OnLogsChanged;
        Refresh();
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
        => _viewModel.IncrementCommand.Execute(null);

    private void OnResetClicked(object? sender, EventArgs e)
        => _viewModel.ResetCommand.Execute(null);

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(Refresh);

    private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(RefreshLogs);

    private void Refresh()
    {
        CountLabel.Text = $"Count: {_viewModel.Count}";
        RefreshLogs();
    }

    private void RefreshLogs()
    {
        LogList.ItemsSource = _viewModel.Logs
            .Select(l => $"[{l.CreatedAt:HH:mm:ss}] {l.Message}")
            .ToList();
    }
}
