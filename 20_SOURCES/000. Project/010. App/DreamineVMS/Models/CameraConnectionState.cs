namespace DreamineVMS.Models;

/// <summary>
/// \if KO
/// <para>\brief 카메라 연결 상태를 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera connection state functionality and related state.</para>
/// \endif
/// </summary>
public enum CameraConnectionState
{
    /// <summary>
    /// \if KO
    /// <para>\brief 연결되지 않은 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the disconnected value.</para>
    /// \endif
    /// </summary>
    Disconnected,
    /// <summary>
    /// \if KO
    /// <para>\brief 연결 중인 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the connecting value.</para>
    /// \endif
    /// </summary>
    Connecting,
    /// <summary>
    /// \if KO
    /// <para>\brief 연결된 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the connected value.</para>
    /// \endif
    /// </summary>
    Connected,
    /// <summary>
    /// \if KO
    /// <para>\brief 오류 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the faulted value.</para>
    /// \endif
    /// </summary>
    Faulted
}
