using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Pages.PageSub.IoTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \if KO
/// <para>\brief Dreamine I/O 모니터 샘플 페이지 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates page io monitor view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class PageIoMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief Dreamine I/O 모니터 샘플 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private PageIoMonitorEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief I/O 샘플 Runtime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the runtime value.</para>
    /// \endif
    /// </summary>
    public IoSampleRuntime Runtime => Event.Runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief 실물 UDP 컨트롤러 선택 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use real controller operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseRealController")]
    private partial void UseRealController();

    /// <summary>
    /// \if KO
    /// <para>\brief 샘플 컨트롤러 선택 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use sample controller operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseSampleController")]
    private partial void UseSampleController();

    /// <summary>
    /// \if KO
    /// <para>\brief 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Connect")]
    private partial void Connect();

    /// <summary>
    /// \if KO
    /// <para>\brief 실물 장비 Probe 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the probe hardware operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ProbeHardware")]
    private partial void ProbeHardware();

    /// <summary>
    /// \if KO
    /// <para>\brief 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Disconnect")]
    private partial void Disconnect();

    /// <summary>
    /// \if KO
    /// <para>\brief 입력 토글 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle inputs operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ToggleInputs")]
    private partial void ToggleInputs();

    /// <summary>
    /// \if KO
    /// <para>\brief 입력 갱신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh inputs operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.RefreshInputs")]
    private partial void RefreshInputs();

    /// <summary>
    /// \if KO
    /// <para>\brief 출력 쓰기 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes outputs data.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.WriteOutputs")]
    private partial void WriteOutputs();

    /// <summary>
    /// \if KO
    /// <para>\brief 출력 읽기 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads outputs data.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ReadOutputs")]
    private partial void ReadOutputs();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PageIoMonitorViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PageIoMonitorViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>PageIoMonitorEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the I/O monitor page.</para>
    /// \endif
    /// </param>
    public PageIoMonitorViewModel(PageIoMonitorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
