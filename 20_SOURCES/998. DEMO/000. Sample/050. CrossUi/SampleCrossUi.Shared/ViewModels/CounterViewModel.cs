using System.Collections.ObjectModel;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleCrossUi.Shared.Models;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Provides a UI-independent counter sample ViewModel.
/// Reusable across WPF, WinForms, Blazor, and .NET MAUI without modification.
/// Actual logic lives in <see cref="CounterEvent"/>; this class only exposes
/// the [DreamineCommand]-generated forwarding commands and read-only state.
/// </summary>
public partial class CounterViewModel : ViewModelBase
{
    [DreamineEvent]
    private CounterEvent _event;

    /// <summary>Gets the current counter value.</summary>
    public int Count => Event.Count;

    /// <summary>Gets the operation log list (newest first).</summary>
    public ObservableCollection<CounterLogItem> Logs => Event.Logs;

    [DreamineCommand("Event.Increment")]
    private partial void Increment();

    [DreamineCommand("Event.Reset")]
    private partial void Reset();

    /// <summary>
    /// Initializes a new instance of the <see cref="CounterViewModel"/> class.
    /// </summary>
    /// <param name="event">The counter event handler.</param>
    public CounterViewModel(CounterEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
        Event.CountChanged += (_, _) => OnPropertyChanged(nameof(Count));
    }
}
