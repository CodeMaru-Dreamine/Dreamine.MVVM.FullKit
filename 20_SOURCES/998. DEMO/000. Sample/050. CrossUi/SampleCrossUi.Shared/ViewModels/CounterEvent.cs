using System;
using System.Collections.ObjectModel;
using SampleCrossUi.Shared.Models;
using SampleCrossUi.Shared.Services;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Counter 화면의 실제 동작(증가/리셋/로그 기록)을 처리합니다.
/// CounterViewModel은 [DreamineCommand]를 통해 이 클래스의 메서드를 호출만 합니다.
/// </summary>
public sealed class CounterEvent
{
    private readonly ICounterService _service;

    public CounterEvent(ICounterService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        AddLog("Counter sample started.");
    }

    public int Count { get; private set; }

    public ObservableCollection<CounterLogItem> Logs { get; } = new();

    /// <summary>Count가 갱신될 때마다 발생합니다. ViewModel이 구독해서 OnPropertyChanged를 전달합니다.</summary>
    public event EventHandler? CountChanged;

    public void Increment()
    {
        Count = _service.Increment(Count);
        AddLog($"Incremented → {Count}");
        CountChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        Count = _service.Reset();
        AddLog("Counter reset.");
        CountChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, new CounterLogItem(DateTime.Now, message));
    }
}
