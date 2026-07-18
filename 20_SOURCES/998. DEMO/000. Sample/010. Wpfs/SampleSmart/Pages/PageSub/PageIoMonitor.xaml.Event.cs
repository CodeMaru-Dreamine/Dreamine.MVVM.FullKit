using SampleSmart.Pages.PageSub.IoTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \if KO
/// <para>\brief Dreamine I/O 모니터 샘플 페이지 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates page io monitor event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PageIoMonitorEvent
{
    /// <summary>
    /// \if KO
    /// <para>\brief PageIoMonitorEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PageIoMonitorEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>I/O 샘플 실행 컨텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoSampleRuntime</c> value used for runtime.</para>
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
    public PageIoMonitorEvent(IoSampleRuntime runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \if KO
    /// <para>\brief I/O 샘플 Runtime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the runtime value.</para>
    /// \endif
    /// </summary>
    public IoSampleRuntime Runtime { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 실물 Fastech UDP 컨트롤러를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use real controller operation.</para>
    /// \endif
    /// </summary>
    public void UseRealController()
    {
        Runtime.UseRealController();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 샘플 컨트롤러를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use sample controller operation.</para>
    /// \endif
    /// </summary>
    public void UseSampleController()
    {
        Runtime.UseSampleController();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief I/O 컨트롤러에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect operation.</para>
    /// \endif
    /// </summary>
    public void Connect()
    {
        _ = Runtime.ConnectAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 실물 장비에 Probe 명령을 전송합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the probe hardware operation.</para>
    /// \endif
    /// </summary>
    public void ProbeHardware()
    {
        _ = Runtime.ProbeHardwareAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief I/O 컨트롤러 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect operation.</para>
    /// \endif
    /// </summary>
    public void Disconnect()
    {
        _ = Runtime.DisconnectAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 샘플 입력 패턴을 토글합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle inputs operation.</para>
    /// \endif
    /// </summary>
    public void ToggleInputs()
    {
        _ = Runtime.ToggleSampleInputsAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 입력 상태를 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh inputs operation.</para>
    /// \endif
    /// </summary>
    public void RefreshInputs()
    {
        _ = Runtime.RefreshInputsAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 출력 상태를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes outputs data.</para>
    /// \endif
    /// </summary>
    public void WriteOutputs()
    {
        _ = Runtime.WriteOutputsAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 출력 상태를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads outputs data.</para>
    /// \endif
    /// </summary>
    public void ReadOutputs()
    {
        _ = Runtime.ReadOutputsAsync();
    }
}
