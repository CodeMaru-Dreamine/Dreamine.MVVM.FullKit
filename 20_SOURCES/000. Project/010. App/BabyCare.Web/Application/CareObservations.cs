using BabyCare.Domain;
namespace BabyCare.Application;

public sealed record DiaperColor(string Id, string Label, string Hex);
public static class CareObservations
{
    public static readonly DiaperColor[] StoolColors = [new("yellow", "노랑", "#d6ae36"), new("brown", "갈색", "#956444"), new("green", "초록", "#66864e"), new("red", "빨강", "#b44444"), new("black", "검정", "#303633"), new("pale", "흰색 / 회백색", "#e5e3d7")];
    public static readonly DiaperColor[] UrineColors = [new("pale-yellow", "연노랑", "#eee4a6"), new("yellow", "노랑", "#d6ae36"), new("dark-yellow", "진한 노랑", "#b78228"), new("pink-red", "분홍 / 빨강", "#cc777a"), new("brown", "갈색", "#956444")];
    public static bool HasStool(CareEntry e) => e.Kind == "diaper" && e.Detail is "dirty" or "mixed";
    public static bool HasUrine(CareEntry e) => e.Kind == "diaper" && e.Detail is "wet" or "mixed";
    public static CareEntry Prepare(CareEntry e, bool defaults) => e with {
        StoolColor = HasStool(e) ? e.StoolColor ?? (defaults ? "yellow" : null) : null,
        UrineColor = HasUrine(e) ? e.UrineColor ?? (defaults ? "pale-yellow" : null) : null,
        StoolColorConfirmed = HasStool(e) && e.StoolColorConfirmed,
        UrineColorConfirmed = HasUrine(e) && e.UrineColorConfirmed
    };
    public static TimeSpan? FeedingInterval(CareEntry entry, IEnumerable<CareEntry> entries)
    {
        if (entry.Kind != "feeding") return null;
        var previous = entries.Where(e => e.Kind == "feeding" && e.Id != entry.Id && e.StartedAt <= entry.StartedAt)
            .OrderByDescending(e => e.StartedAt).FirstOrDefault();
        return previous is null ? null : entry.StartedAt - previous.StartedAt;
    }
    public static string? Advice(bool stool, string? color) => (stool, color) switch {
        (true, "red" or "pale") => "이 대변 색은 소아과에 빠르게 상담해주세요. 음식·약의 영향일 수도 있지만 색만으로 판단하지 마세요.",
        (true, "black") => "출생 직후 며칠의 검은 태변은 흔합니다. 태변 시기가 지난 검은 대변이나 시기가 불확실하면 소아과에 빠르게 상담해주세요.",
        (false, "pink-red") => "생후 첫 주에는 요산 결정으로 분홍빛 흔적이 생길 수 있습니다. 지속되거나 혈액이 의심되면 소아과에 상담해주세요.",
        (false, "brown") => "갈색 소변은 소아과에 상담해주세요. 색만으로 원인을 판단할 수 없습니다.",
        (false, "dark-yellow") => "소변이 진하고 젖은 기저귀가 줄거나 잘 먹지 못하면 소아과에 상담해주세요.",
        _ => null
    };
}
