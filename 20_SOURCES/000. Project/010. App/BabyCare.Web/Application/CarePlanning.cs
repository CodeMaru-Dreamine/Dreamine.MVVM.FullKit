using BabyCare.Domain;

namespace BabyCare.Application;

public sealed record CareSuggestion(CareEntry Entry, string Reason);

public static class CarePlanning
{
    public static CareEntry ApplyFeedingDefault(CareEntry entry, CarePreferences preferences) =>
        entry.Kind == "feeding" && entry.Version == 0 && entry.EndedAt is null && preferences.UseFeedingDefault
            ? entry with { ExpectedDurationMinutes = preferences.FeedingMinutes } : entry;

    // Suggestions never write records or count toward the sleep total. Every saved proposal is reviewed.
    public static IReadOnlyList<CareSuggestion> Suggest(FamilyState family, DateTimeOffset now)
    {
        if (!family.Preferences.SuggestSleep) return [];
        // A parent's pumping session is not an infant wake/sleep event.
        var entries = family.Entries.Where(e => e.Kind != "pumping").OrderBy(e => e.StartedAt).ThenBy(e => e.Id).ToArray();
        var result = new List<CareSuggestion>();
        foreach (var source in entries.Where(e => e.Kind is "feeding" or "burping" or "diaper"))
        {
            var end = source.EndedAt ?? (source.ExpectedDurationMinutes is { } minutes
                ? source.StartedAt.AddMinutes(minutes) : source.Kind == "diaper" ? source.StartedAt : (DateTimeOffset?)null);
            if (end is null || end > now) continue;
            // Any intervening event interrupts the assumption; never bridge across a measured event or an open record.
            var next = entries.Where(e => e.Id != source.Id && e.StartedAt >= source.StartedAt)
                .OrderBy(e => e.StartedAt).FirstOrDefault();
            var until = next?.StartedAt ?? now;
            if (until <= end) continue;
            var sleepStart = end.Value;
            if (source.Kind == "feeding")
            {
                var burpEnd = end.Value.AddMinutes(family.Preferences.BurpingMinutes);
                if (family.Preferences.BurpingMinutes > 0 && burpEnd <= until && burpEnd <= now &&
                    !Overlaps(entries, end.Value, burpEnd, source.Id))
                    result.Add(new(new CareEntry { Kind = "burping", StartedAt = end.Value, EndedAt = burpEnd,
                        ConfirmedFromEstimate = true }, "수유 종료 + 기본 트림 시간으로 계산한 추정입니다. 실제로 트림시킨 시간을 확인해주세요."));
                sleepStart = burpEnd;
            }
            else if (source.Kind == "diaper") sleepStart = end.Value.AddMinutes(family.Preferences.DiaperToSleepMinutes);
            if (sleepStart >= until || until > now || Overlaps(entries, sleepStart, until, source.Id)) continue;
            result.Add(new(new CareEntry { Kind = "sleep", StartedAt = sleepStart, EndedAt = until,
                ConfirmedFromEstimate = true }, "설정한 대기 시간 이후부터 다음 기록(없으면 현재)까지의 추정 수면입니다. 실제 잠든 시간과 깬 시간을 확인해주세요."));
        }
        return result.OrderByDescending(x => x.Entry.StartedAt).ToArray();
    }

    public static int SleepMinutesOnDay(IEnumerable<CareEntry> entries, DateTime day, TimeSpan offset)
    {
        var start = new DateTimeOffset(DateTime.SpecifyKind(day.Date, DateTimeKind.Unspecified), offset);
        var end = start.AddDays(1);
        var spans = entries.Where(e => e.Kind == "sleep" && e.EndedAt.HasValue && e.StartedAt < end && e.EndedAt > start)
            .Select(e => (Start: e.StartedAt > start ? e.StartedAt : start, End: e.EndedAt!.Value < end ? e.EndedAt.Value : end))
            .OrderBy(e => e.Start).ToArray();
        double total = 0; var cursor = start;
        foreach (var span in spans)
        {
            var from = span.Start > cursor ? span.Start : cursor;
            if (span.End > from) total += (span.End - from).TotalMinutes;
            if (span.End > cursor) cursor = span.End;
        }
        return (int)Math.Round(total);
    }

    private static bool Overlaps(IEnumerable<CareEntry> entries, DateTimeOffset start, DateTimeOffset end, string sourceId) =>
        entries.Any(e => e.Id != sourceId && e.StartedAt < end &&
            (e.EndedAt ?? (e.ExpectedDurationMinutes is { } minutes ? e.StartedAt.AddMinutes(minutes) :
                e.Kind is "sleep" or "feeding" or "burping" or "solids" ? DateTimeOffset.MaxValue : e.StartedAt)) > start);
}
