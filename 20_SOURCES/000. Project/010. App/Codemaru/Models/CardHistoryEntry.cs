namespace Codemaru.Models;

/// <summary>
/// \if KO
/// <para>Card History Entry 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card history entry functionality and related state.</para>
/// \endif
/// </summary>
public sealed record CardHistoryEntry(
    Guid Id,
    CardProfile Profile,
    string LandingUrl,
    string QrPayload,
    DateTime CreatedAt);
