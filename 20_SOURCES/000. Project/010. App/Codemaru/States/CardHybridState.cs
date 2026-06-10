using Codemaru.Models;

namespace Codemaru.States;

public sealed record CardHybridState(
    CardProfile Profile,
    string QrPayload,
    string QrSvg,
    IReadOnlyList<CardHistoryEntry> History,
    DateTime LastUpdated)
{
    public static CardHybridState CreateDefault(string qrSvg)
    {
        var profile = CardProfile.Default;

        return Create(profile, qrSvg, Array.Empty<CardHistoryEntry>());
    }

    public static CardHybridState Create(CardProfile profile, string qrSvg, IReadOnlyList<CardHistoryEntry> history)
    {
        return new CardHybridState(
            Profile: profile,
            QrPayload: profile.LandingUrl,
            QrSvg: qrSvg,
            History: history,
            LastUpdated: DateTime.Now);
    }
}
