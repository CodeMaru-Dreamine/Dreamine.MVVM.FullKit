using System.Collections.ObjectModel;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleCrossUi.Shared.Models;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Counter View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a UI-independent counter sample ViewModel. Reusable across WPF, WinForms, Blazor, and .NET MAUI without modification. Actual logic lives in <see cref="CounterEvent"/>; this class only exposes the [DreamineCommand]-generated forwarding commands and read-only state.</para>
/// \endif
/// </summary>
public partial class CounterViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>event 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private CounterEvent _event;

    /// <summary>
    /// \if KO
    /// <para>Count 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current counter value.</para>
    /// \endif
    /// </summary>
    public int Count => Event.Count;

    /// <summary>
    /// \if KO
    /// <para>Logs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the operation log list (newest first).</para>
    /// \endif
    /// </summary>
    public ObservableCollection<CounterLogItem> Logs => Event.Logs;

    /// <summary>
    /// \if KO
    /// <para>Increment 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the increment operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Increment")]
    private partial void Increment();

    /// <summary>
    /// \if KO
    /// <para>Reset 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Reset")]
    private partial void Reset();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="CounterViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CounterViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>CounterEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The counter event handler.</para>
    /// \endif
    /// </param>
    public CounterViewModel(CounterEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
        Event.CountChanged += (_, _) => OnPropertyChanged(nameof(Count));
    }
}
