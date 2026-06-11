using System;
using System.Collections.ObjectModel;
using Dreamine.MVVM.ViewModels;
using SampleCrossUi.Shared.Models;
using SampleCrossUi.Shared.Services;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Provides a UI-independent counter sample ViewModel.
/// Reusable across WPF, WinForms, Blazor, and .NET MAUI without modification.
/// </summary>
public sealed class CounterViewModel : ViewModelBase
{
    private readonly ICounterService _counterService;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="CounterViewModel"/> class.
    /// </summary>
    /// <param name="counterService">The counter service.</param>
    public CounterViewModel(ICounterService counterService)
    {
        ArgumentNullException.ThrowIfNull(counterService);

        _counterService = counterService;

        IncrementCommand = new RelayCommand(Increment);
        ResetCommand = new RelayCommand(Reset);

        AddLog("Counter sample started.");
    }

    /// <summary>Gets the increment command.</summary>
    public RelayCommand IncrementCommand { get; }

    /// <summary>Gets the reset command.</summary>
    public RelayCommand ResetCommand { get; }

    /// <summary>Gets the operation log list (newest first).</summary>
    public ObservableCollection<CounterLogItem> Logs { get; } = new();

    /// <summary>Gets the current counter value.</summary>
    public int Count
    {
        get => _count;
        private set => SetProperty(ref _count, value);
    }

    private void Increment()
    {
        Count = _counterService.Increment(Count);
        AddLog($"Incremented → {Count}");
    }

    private void Reset()
    {
        Count = _counterService.Reset();
        AddLog("Counter reset.");
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, new CounterLogItem(DateTime.Now, message));
    }
}
