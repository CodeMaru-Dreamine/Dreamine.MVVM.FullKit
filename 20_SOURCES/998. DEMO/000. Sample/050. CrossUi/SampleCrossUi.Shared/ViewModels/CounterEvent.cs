using System;
using System.Collections.ObjectModel;
using SampleCrossUi.Shared.Models;
using SampleCrossUi.Shared.Services;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Counter 화면의 실제 동작(증가/리셋/로그 기록)을 처리합니다. CounterViewModel은 [DreamineCommand]를 통해 이 클래스의 메서드를 호출만 합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates counter event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CounterEvent
{
    /// <summary>
    /// \if KO
    /// <para>service 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the service value.</para>
    /// \endif
    /// </summary>
    private readonly ICounterService _service;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="CounterEvent"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CounterEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="service">
    /// \if KO
    /// <para>service에 사용할 <c>ICounterService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICounterService</c> value used for service.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public CounterEvent(ICounterService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        AddLog("Counter sample started.");
    }

    /// <summary>
    /// \if KO
    /// <para>Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the count value.</para>
    /// \endif
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Logs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the logs value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<CounterLogItem> Logs { get; } = new();

    /// <summary>
    /// \if KO
    /// <para>Count가 갱신될 때마다 발생합니다. ViewModel이 구독해서 OnPropertyChanged를 전달합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when count changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? CountChanged;

    /// <summary>
    /// \if KO
    /// <para>Increment 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the increment operation.</para>
    /// \endif
    /// </summary>
    public void Increment()
    {
        Count = _service.Increment(Count);
        AddLog($"Incremented → {Count}");
        CountChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// \if KO
    /// <para>Reset 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset operation.</para>
    /// \endif
    /// </summary>
    public void Reset()
    {
        Count = _service.Reset();
        AddLog("Counter reset.");
        CountChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// \if KO
    /// <para>Log 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the log item.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    private void AddLog(string message)
    {
        Logs.Insert(0, new CounterLogItem(DateTime.Now, message));
    }
}
