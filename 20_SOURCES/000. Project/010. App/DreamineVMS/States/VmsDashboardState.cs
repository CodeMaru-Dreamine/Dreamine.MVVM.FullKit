namespace DreamineVMS.States;

/// <summary>
/// \if KO
/// <para>\brief WPF Shell과 Blazor Dashboard가 공유하는 VMS 상태입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms dashboard state functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsDashboardState
{
    /// <summary>
    /// \if KO
    /// <para>\brief 빈 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the empty value.</para>
    /// \endif
    /// </summary>
    public static VmsDashboardState Empty => new();

    /// <summary>
    /// \if KO
    /// <para>\brief 전체 카메라 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the total camera count value.</para>
    /// \endif
    /// </summary>
    public int TotalCameraCount { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 연결된 카메라 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the connected camera count value.</para>
    /// \endif
    /// </summary>
    public int ConnectedCameraCount { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 녹화 중인 카메라 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the recording camera count value.</para>
    /// \endif
    /// </summary>
    public int RecordingCameraCount { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 이벤트 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last event value.</para>
    /// \endif
    /// </summary>
    public string LastEvent { get; init; } = "VMS application started.";

    /// <summary>
    /// \if KO
    /// <para>\brief 마지막 갱신 시각입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last updated value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset? LastUpdated { get; init; } = DateTimeOffset.Now;
}
