namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief Serial 통신 샘플 탭 이벤트 처리 클래스입니다.
/// </summary>
public sealed class SerialCommunicationTabEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief SerialCommunicationTabEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public SerialCommunicationTabEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }
}