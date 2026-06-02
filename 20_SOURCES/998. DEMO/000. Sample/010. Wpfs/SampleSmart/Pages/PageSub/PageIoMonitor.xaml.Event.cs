using SampleSmart.Pages.PageSub.IoTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine I/O 모니터 샘플 페이지 이벤트 처리 클래스입니다.
/// </summary>
public sealed class PageIoMonitorEvent
{
    /// <summary>
    /// \brief PageIoMonitorEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">I/O 샘플 실행 컨텍스트입니다.</param>
    public PageIoMonitorEvent(IoSampleRuntime runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \brief I/O 샘플 Runtime입니다.
    /// </summary>
    public IoSampleRuntime Runtime { get; }

    /// <summary>
    /// \brief 실물 Fastech UDP 컨트롤러를 선택합니다.
    /// </summary>
    public void UseRealController()
    {
        Runtime.UseRealController();
    }

    /// <summary>
    /// \brief 샘플 컨트롤러를 선택합니다.
    /// </summary>
    public void UseSampleController()
    {
        Runtime.UseSampleController();
    }

    /// <summary>
    /// \brief I/O 컨트롤러에 연결합니다.
    /// </summary>
    public void Connect()
    {
        _ = Runtime.ConnectAsync();
    }

    /// <summary>
    /// \brief 실물 장비에 Probe 명령을 전송합니다.
    /// </summary>
    public void ProbeHardware()
    {
        _ = Runtime.ProbeHardwareAsync();
    }

    /// <summary>
    /// \brief I/O 컨트롤러 연결을 해제합니다.
    /// </summary>
    public void Disconnect()
    {
        _ = Runtime.DisconnectAsync();
    }

    /// <summary>
    /// \brief 샘플 입력 패턴을 토글합니다.
    /// </summary>
    public void ToggleInputs()
    {
        _ = Runtime.ToggleSampleInputsAsync();
    }

    /// <summary>
    /// \brief 입력 상태를 갱신합니다.
    /// </summary>
    public void RefreshInputs()
    {
        _ = Runtime.RefreshInputsAsync();
    }

    /// <summary>
    /// \brief 출력 상태를 씁니다.
    /// </summary>
    public void WriteOutputs()
    {
        _ = Runtime.WriteOutputsAsync();
    }

    /// <summary>
    /// \brief 출력 상태를 읽습니다.
    /// </summary>
    public void ReadOutputs()
    {
        _ = Runtime.ReadOutputsAsync();
    }
}
