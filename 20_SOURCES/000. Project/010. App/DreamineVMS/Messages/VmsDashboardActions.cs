namespace DreamineVMS.Messages;

/// <summary>
/// \brief VmsDashboardActionRequestedMessage에서 사용하는 Action 문자열 상수입니다.
/// </summary>
/// <remarks>
/// 메시지 발행 측과 처리 측이 문자열 오타로 어긋나는 것을 막기 위해 상수화합니다.
/// </remarks>
public static class VmsDashboardActions
{
    /// <summary>\brief Embedded Dashboard에서 강제 새로고침을 요청합니다.</summary>
    public const string EmbeddedRefresh = "embedded.refresh";

    /// <summary>\brief Server Dashboard에서 Live 탭으로 전환을 요청합니다.</summary>
    public const string ServerOpenLive = "server.open-live";
}
