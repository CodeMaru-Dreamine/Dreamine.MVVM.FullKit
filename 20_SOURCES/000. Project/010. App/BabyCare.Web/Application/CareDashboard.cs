using BabyCare.Domain;

namespace BabyCare.Application;

public static class CareDashboard
{
    public static (int Days, int Months)? Age(DateOnly? birth, DateOnly on)
    {
        if (birth is not { } b || b > on) return null;
        var months = (on.Year - b.Year) * 12 + on.Month - b.Month;
        if (b.AddMonths(months) > on) months--;
        return (on.DayNumber - b.DayNumber, months);
    }

    public static string SleepReference(int? months) => months switch
    {
        null => "생년월일을 입력하면 월령별 참고 정보를 볼 수 있어요.",
        < 4 => "4개월 미만은 수면 편차가 커요. 신생아는 하루 약 16~17시간 자기도 해요.",
        < 12 => "하루 12~16시간 · 낮잠 포함 (4~11개월)",
        < 36 => "하루 11~14시간 · 낮잠 포함 (1~2세)",
        < 72 => "하루 10~13시간 · 낮잠 포함 (3~5세)",
        _ => "연령에 맞는 수면 정보는 참고 자료를 확인해주세요."
    };

    public static double? AverageInterval(IEnumerable<CareEntry> entries, string kind)
    {
        var times = entries.Where(e => e.Kind == kind).Select(e => e.StartedAt).Order().ToArray();
        return times.Length < 2 ? null : (times[^1] - times[0]).TotalMinutes / (times.Length - 1);
    }

    public static void Validate(CarePreferences p, DateOnly today)
    {
        if (p.AvatarPhotoId is not null && !BabyCare.Infrastructure.CarePhotos.ValidId(p.AvatarPhotoId))
            throw new CareException("사진은 JPG·PNG·WebP, 장당 8MB까지 첨부할 수 있어요.");
        if (p.BirthDate > today || p.BirthDate < today.AddYears(-20) ||
            p.DailyMilkGoalMl is < 1 or > 3000 || p.DiaperCheckMinutes is < 15 or > 720)
            throw new CareException("생년월일과 목표 설정을 확인해주세요.");
    }
}
