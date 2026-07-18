namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \if KO
/// <para>\brief InMemory Communication 샘플 탭 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory communication tab event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class InMemoryCommunicationTabEvent
{
    /// <summary>
    /// \if KO
    /// <para>runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime value.</para>
    /// \endif
    /// </summary>
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief InMemoryCommunicationTabEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="InMemoryCommunicationTabEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>Communication 샘플 공유 Runtime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CommunicationSampleRuntime</c> value used for runtime.</para>
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
    public InMemoryCommunicationTabEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        InMemorySendText = "Hello from SampleSmart InMemory";
        InMemoryReceiveText = "Manual InMemory receive test";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the in memory send text value.</para>
    /// \endif
    /// </summary>
    public string InMemorySendText { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 수동 수신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the in memory receive text value.</para>
    /// \endif
    /// </summary>
    public string InMemoryReceiveText { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 채널을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the channel item.</para>
    /// \endif
    /// </summary>
    public void AddChannel()
    {
        _runtime.AddInMemoryChannel();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory MessageBus에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect operation.</para>
    /// \endif
    /// </summary>
    public void Connect()
    {
        _ = _runtime.ConnectInMemoryAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory MessageBus 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect operation.</para>
    /// \endif
    /// </summary>
    public void Disconnect()
    {
        _ = _runtime.DisconnectInMemoryAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory MessageBus로 테스트 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send test operation.</para>
    /// \endif
    /// </summary>
    public void SendTest()
    {
        _ = _runtime.SendInMemoryAsync(InMemorySendText);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 수동 수신 로그를 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the receive test operation.</para>
    /// \endif
    /// </summary>
    public void ReceiveTest()
    {
        _runtime.ReceiveInMemory(InMemoryReceiveText);
    }
}