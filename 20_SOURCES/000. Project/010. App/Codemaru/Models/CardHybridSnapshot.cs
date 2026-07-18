namespace Codemaru.Models;

/// <summary>
/// \if KO
/// <para>Card Hybrid Snapshot 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>\brief Represents the persisted CardHybrid workspace snapshot.</para>
/// \endif
/// </summary>
public sealed record CardHybridSnapshot(
    string? UserId,
    CardProfile Profile,
    IReadOnlyList<CardHistoryEntry> History,
    DateTime SavedAt);
