using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Pages.PageSub.IoTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine I/O 모니터 샘플 페이지 ViewModel입니다.
/// </summary>
public partial class PageIoMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \brief Dreamine I/O 모니터 샘플 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private PageIoMonitorEvent _event;

    /// <summary>
    /// \brief I/O 샘플 Runtime입니다.
    /// </summary>
    public IoSampleRuntime Runtime => Event.Runtime;

    /// <summary>
    /// \brief 실물 UDP 컨트롤러 선택 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseRealController")]
    private partial void UseRealController();

    /// <summary>
    /// \brief 샘플 컨트롤러 선택 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseSampleController")]
    private partial void UseSampleController();

    /// <summary>
    /// \brief 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.Connect")]
    private partial void Connect();

    /// <summary>
    /// \brief 실물 장비 Probe 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ProbeHardware")]
    private partial void ProbeHardware();

    /// <summary>
    /// \brief 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.Disconnect")]
    private partial void Disconnect();

    /// <summary>
    /// \brief 입력 토글 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ToggleInputs")]
    private partial void ToggleInputs();

    /// <summary>
    /// \brief 입력 갱신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.RefreshInputs")]
    private partial void RefreshInputs();

    /// <summary>
    /// \brief 출력 쓰기 명령입니다.
    /// </summary>
    [DreamineCommand("Event.WriteOutputs")]
    private partial void WriteOutputs();

    /// <summary>
    /// \brief 출력 읽기 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ReadOutputs")]
    private partial void ReadOutputs();

    /// <summary>
    /// Initializes a new instance of the <see cref="PageIoMonitorViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the I/O monitor page.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public PageIoMonitorViewModel(PageIoMonitorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
