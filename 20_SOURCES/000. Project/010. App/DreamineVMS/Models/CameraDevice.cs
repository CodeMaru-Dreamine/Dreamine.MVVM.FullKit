namespace DreamineVMS.Models;

/// <summary>
/// \if KO
/// <para>\brief VMS에서 관리하는 CCTV 카메라 장치 정보입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera device functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CameraDevice
{
    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 고유 식별자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 표시 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 IP 또는 호스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief RTSP 주소입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the rtsp url value.</para>
    /// \endif
    /// </summary>
    public required string RtspUrl { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 화면 배치 순서입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the display order value.</para>
    /// \endif
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 소유자 사용자 ID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tenant id value.</para>
    /// \endif
    /// </summary>
    public string TenantId { get; init; } = "";

    /// <summary>
    /// \if KO
    /// <para>\brief 사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the enabled value.</para>
    /// \endif
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// \if KO
    /// <para>\brief 자동 재접속 사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the auto reconnect value.</para>
    /// \endif
    /// </summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>
    /// \if KO
    /// <para>\brief 외부에 공개할지 여부입니다. true이면 /{slug}/live 에 노출됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is public value.</para>
    /// \endif
    /// </summary>
    public bool IsPublic { get; init; } = false;

    /// <summary>
    /// \if KO
    /// <para>\brief HTTP(S)로 시작하는 직접 재생 가능한 HLS URL 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is direct hls value.</para>
    /// \endif
    /// </summary>
    public bool IsDirectHls =>
        RtspUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        RtspUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>\brief HLS 재생 URL입니다. 직접 HLS면 원본 URL, RTSP면 변환 경로를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hls url value.</para>
    /// \endif
    /// </summary>
    public string HlsUrl => IsDirectHls ? RtspUrl : $"/hls/{Id}/index.m3u8";
}
