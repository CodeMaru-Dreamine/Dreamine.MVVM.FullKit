namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \brief InMemory PLC 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class InMemoryPlcTabEvent
{
    private readonly PlcSampleRuntime _runtime;

    /// <summary>
    /// \brief InMemoryPlcTabEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">PLC 샘플 공유 런타임입니다.</param>
    public InMemoryPlcTabEvent(PlcSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \brief InMemory PLC Client를 선택합니다.
    /// </summary>
    public void UseInMemory()
    {
        _runtime.UseInMemoryClient();
    }
}
