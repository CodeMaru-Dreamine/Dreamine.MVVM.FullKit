namespace DreamineVMS.Messages;

/// <summary>
/// \if KO
/// <para>\brief VmsDashboardActionRequestedMessage에서 사용하는 Action 문자열 상수입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms dashboard actions functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>메시지 발행 측과 처리 측이 문자열 오타로 어긋나는 것을 막기 위해 상수화합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public static class VmsDashboardActions
{
    /// <summary>
    /// \if KO
    /// <para>\brief Embedded Dashboard에서 강제 새로고침을 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the embedded refresh value.</para>
    /// \endif
    /// </summary>
    public const string EmbeddedRefresh = "embedded.refresh";

    /// <summary>
    /// \if KO
    /// <para>\brief Server Dashboard에서 Live 탭으로 전환을 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the server open live value.</para>
    /// \endif
    /// </summary>
    public const string ServerOpenLive = "server.open-live";

    /// <summary>
    /// \if KO
    /// <para>\brief 선택한 카메라 연결을 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the camera connect value.</para>
    /// \endif
    /// </summary>
    public const string CameraConnect = "camera.connect";

    /// <summary>
    /// \if KO
    /// <para>\brief 선택한 카메라 연결 해제를 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the camera disconnect value.</para>
    /// \endif
    /// </summary>
    public const string CameraDisconnect = "camera.disconnect";

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 연결을 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the camera start all value.</para>
    /// \endif
    /// </summary>
    public const string CameraStartAll = "camera.start-all";

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 연결 해제를 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the camera stop all value.</para>
    /// \endif
    /// </summary>
    public const string CameraStopAll = "camera.stop-all";

    /// <summary>
    /// \if KO
    /// <para>\brief WPF Shell 로그 삭제를 요청합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the clear logs value.</para>
    /// \endif
    /// </summary>
    public const string ClearLogs = "logs.clear";
}
