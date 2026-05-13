namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief InMemory Communication 샘플 탭 이벤트 처리 클래스입니다.
/// </summary>
public sealed class InMemoryCommunicationTabEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief InMemoryCommunicationTabEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public InMemoryCommunicationTabEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        InMemorySendText = "Hello from SampleSmart InMemory";
        InMemoryReceiveText = "Manual InMemory receive test";
    }

    /// <summary>
    /// \brief InMemory 송신 문자열입니다.
    /// </summary>
    public string InMemorySendText { get; set; }

    /// <summary>
    /// \brief InMemory 수동 수신 문자열입니다.
    /// </summary>
    public string InMemoryReceiveText { get; set; }

    /// <summary>
    /// \brief InMemory 채널을 추가합니다.
    /// </summary>
    public void AddChannel()
    {
        _runtime.AddInMemoryChannel();
    }

    /// <summary>
    /// \brief InMemory MessageBus에 연결합니다.
    /// </summary>
    public void Connect()
    {
        _ = _runtime.ConnectInMemoryAsync();
    }

    /// <summary>
    /// \brief InMemory MessageBus 연결을 해제합니다.
    /// </summary>
    public void Disconnect()
    {
        _ = _runtime.DisconnectInMemoryAsync();
    }

    /// <summary>
    /// \brief InMemory MessageBus로 테스트 메시지를 송신합니다.
    /// </summary>
    public void SendTest()
    {
        _ = _runtime.SendInMemoryAsync(InMemorySendText);
    }

    /// <summary>
    /// \brief InMemory 수동 수신 로그를 추가합니다.
    /// </summary>
    public void ReceiveTest()
    {
        _runtime.ReceiveInMemory(InMemoryReceiveText);
    }
}