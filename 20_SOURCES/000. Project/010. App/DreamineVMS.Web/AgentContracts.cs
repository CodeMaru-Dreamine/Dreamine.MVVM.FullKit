namespace DreamineVMS.Web;

/// <summary>Agent 로그인을 위한 자격 증명입니다.</summary>
internal sealed record AgentLoginRequest(string Email, string Password);

/// <summary>Agent와 서버가 교환하는 카메라 구성입니다.</summary>
internal sealed record AgentCameraDto(
    string Id,
    string Name,
    string Host,
    string RtspUrl,
    bool AutoReconnect,
    bool IsPublic);

/// <summary>인증된 Agent에 반환되는 세션과 카메라 구성입니다.</summary>
internal sealed record AgentLoginResponse(
    string Token,
    string TenantId,
    List<AgentCameraDto> Cameras);

/// <summary>Agent가 서버에 동기화할 카메라 구성입니다.</summary>
internal sealed record AgentSyncRequest(
    string Token,
    List<AgentCameraDto> Cameras);
