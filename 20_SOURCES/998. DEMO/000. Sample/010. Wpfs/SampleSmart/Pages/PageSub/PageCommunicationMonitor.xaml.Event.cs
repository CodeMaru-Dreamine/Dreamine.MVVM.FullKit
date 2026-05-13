using Dreamine.Communication.Wpf.ViewModels;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine Communication 모니터 샘플 페이지 이벤트 처리 클래스입니다.
/// </summary>
public sealed class PageCommunicationMonitorEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief PageCommunicationMonitorEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 실행 컨텍스트입니다.</param>
    public PageCommunicationMonitorEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \brief Communication Monitor ViewModel입니다.
    /// </summary>
    public CommunicationMonitorViewModel Monitor => _runtime.Monitor;
}
