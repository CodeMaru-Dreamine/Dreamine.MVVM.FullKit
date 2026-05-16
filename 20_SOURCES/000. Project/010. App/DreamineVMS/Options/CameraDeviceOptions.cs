namespace DreamineVMS.Options;

/// <summary>
/// \brief 설정 파일에서 읽는 카메라 설정입니다.
/// </summary>
public sealed class CameraDeviceOptions
{
    /// <summary>\brief 카메라 고유 식별자입니다.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>\brief 카메라 표시 이름입니다.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>\brief 카메라 IP 또는 호스트입니다.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>\brief RTSP 주소입니다.</summary>
    public string RtspUrl { get; set; } = string.Empty;

    /// <summary>\brief 화면 표시 순서입니다.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>\brief 사용 여부입니다.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>\brief 자동 재접속 여부입니다.</summary>
    public bool AutoReconnect { get; set; } = true;
}
