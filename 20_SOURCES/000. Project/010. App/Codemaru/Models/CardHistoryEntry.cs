namespace Codemaru.Models;

public sealed record CardHistoryEntry(
    Guid Id,
    CardProfile Profile,
    string LandingUrl,
    string QrPayload,
    DateTime CreatedAt);
