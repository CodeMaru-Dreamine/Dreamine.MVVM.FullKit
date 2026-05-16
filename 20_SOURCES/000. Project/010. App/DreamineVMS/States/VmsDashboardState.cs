namespace DreamineVMS.States;

/// <summary>
/// \brief WPF Shell과 Blazor Dashboard가 공유하는 VMS 상태입니다.
/// </summary>
public sealed class VmsDashboardState
{
    /// <summary>\brief 빈 상태입니다.</summary>
    public static VmsDashboardState Empty => new();

    /// <summary>\brief 전체 카메라 수입니다.</summary>
    public int TotalCameraCount { get; init; }

    /// <summary>\brief 연결된 카메라 수입니다.</summary>
    public int ConnectedCameraCount { get; init; }

    /// <summary>\brief 녹화 중인 카메라 수입니다.</summary>
    public int RecordingCameraCount { get; init; }

    /// <summary>\brief 마지막 이벤트 메시지입니다.</summary>
    public string LastEvent { get; init; } = "VMS application started.";

    /// <summary>\brief 마지막 갱신 시각입니다.</summary>
    public DateTimeOffset? LastUpdated { get; init; } = DateTimeOffset.Now;
}
