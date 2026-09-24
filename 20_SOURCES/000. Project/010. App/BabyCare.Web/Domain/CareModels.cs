namespace BabyCare.Domain;

public sealed record CareKind(string Id, string Label, string Icon, bool HasAmount, bool HasDuration, string[] Examples);

public sealed record CareEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Kind { get; set; } = "feeding";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public int? AmountMl { get; set; }
    public string FeedingMode { get; set; } = "";
    public string BreastSide { get; set; } = "";
    public decimal? AmountGrams { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? LeftAmountMl { get; set; }
    public int? RightAmountMl { get; set; }
    public string PumpMethod { get; set; } = "";
    public List<string> PhotoIds { get; set; } = [];
    public int? ExpectedDurationMinutes { get; set; }
    public bool ConfirmedFromEstimate { get; set; }
    public string? StoolColor { get; set; }
    public string? UrineColor { get; set; }
    public bool StoolColorConfirmed { get; set; }
    public bool UrineColorConfirmed { get; set; }
    public string Detail { get; set; } = "";
    public string Note { get; set; } = "";
    public string NoteLanguage { get; set; } = "";
    public Dictionary<string, string?> NoteTranslations { get; set; } = new();
    public int Version { get; set; }
    public string UpdatedBy { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FamilyState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string BabyName { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public List<string> Members { get; set; } = [];
    public CarePreferences Preferences { get; set; } = new();
    public List<CareEntry> Entries { get; set; } = [];
    public List<CareAudit> Audit { get; set; } = [];
}

public sealed record CareAudit(string EntryId, string Actor, string Action, DateTimeOffset At, CareEntry? Before);
public sealed record FamilySummary(string Id, string Name, string BabyName);
public sealed record ParseResult(CareEntry? Entry, string? Question);
public sealed class CareException(string message) : Exception(message);

public sealed record CarePreferences
{
    public DateOnly? BirthDate { get; set; }
    public string? AvatarPhotoId { get; set; }
    public int? DailyMilkGoalMl { get; set; }
    public int? DiaperCheckMinutes { get; set; }
    public bool UseFeedingDefault { get; set; } = true;
    public int FeedingMinutes { get; set; } = 20;
    public int BurpingMinutes { get; set; } = 20;
    public int DiaperToSleepMinutes { get; set; } = 10;
    public bool SuggestSleep { get; set; } = true;
    public int Version { get; set; }
}
