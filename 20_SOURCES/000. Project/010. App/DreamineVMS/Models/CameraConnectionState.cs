namespace DreamineVMS.Models;

/// <summary>
/// \brief 카메라 연결 상태를 나타냅니다.
/// </summary>
public enum CameraConnectionState
{
    /// <summary>\brief 연결되지 않은 상태입니다.</summary>
    Disconnected,
    /// <summary>\brief 연결 중인 상태입니다.</summary>
    Connecting,
    /// <summary>\brief 연결된 상태입니다.</summary>
    Connected,
    /// <summary>\brief 오류 상태입니다.</summary>
    Faulted
}
