namespace DreamineVMS.Models;

/// <summary>
/// \brief VMS에서 관리하는 CCTV 카메라 장치 정보입니다.
/// </summary>
public sealed class CameraDevice
{
    /// <summary>\brief 카메라 고유 식별자입니다.</summary>
    public required string Id { get; init; }

    /// <summary>\brief 카메라 표시 이름입니다.</summary>
    public required string Name { get; init; }

    /// <summary>\brief 카메라 IP 또는 호스트입니다.</summary>
    public required string Host { get; init; }

    /// <summary>\brief RTSP 주소입니다.</summary>
    public required string RtspUrl { get; init; }

    /// <summary>\brief 화면 배치 순서입니다.</summary>
    public int DisplayOrder { get; init; }

    /// <summary>\brief 사용 여부입니다.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>\brief 자동 재접속 사용 여부입니다.</summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>\brief HLS 재생 URL입니다.</summary>
    public string HlsUrl => $"/hls/{Id}/index.m3u8";
}
