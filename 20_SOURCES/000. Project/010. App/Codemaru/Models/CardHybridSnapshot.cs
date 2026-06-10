namespace Codemaru.Models;

/// <summary>
/// \brief Represents the persisted CardHybrid workspace snapshot.
/// </summary>
/// <param name="UserId">The owner user ID for the snapshot.</param>
/// <param name="Profile">The current card profile.</param>
/// <param name="History">The saved profile history.</param>
/// <param name="SavedAt">The last saved time.</param>
public sealed record CardHybridSnapshot(
    string? UserId,
    CardProfile Profile,
    IReadOnlyList<CardHistoryEntry> History,
    DateTime SavedAt);
