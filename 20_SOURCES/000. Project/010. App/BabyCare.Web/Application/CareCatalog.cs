using System.Text.Json;
using BabyCare.Domain;

namespace BabyCare.Application;

// One catalog drives the manual editor, validation and AI vocabulary.
public sealed class CareCatalog
{
    public IReadOnlyList<CareKind> Kinds { get; }
    public CareCatalog(string path)
    {
        Kinds = JsonSerializer.Deserialize<CareKind[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("기록 종류 설정이 없습니다.");
        if (Kinds.Count == 0 || Kinds.Select(x => x.Id).Distinct().Count() != Kinds.Count)
            throw new InvalidOperationException("기록 종류 ID가 비어 있거나 중복되었습니다.");
    }
    public CareKind Get(string id) => Kinds.FirstOrDefault(x => x.Id == id) ?? throw new CareException("지원하지 않는 기록 종류입니다.");
    public void Validate(CareEntry entry)
    {
        var kind = Get(entry.Kind);
        if (entry.FeedingMode is not ("" or "formula" or "breast" or "expressed") ||
            (entry.Kind != "feeding" && entry.FeedingMode != "") ||
            entry.BreastSide is not ("" or "left" or "right" or "both") ||
            (entry.BreastSide != "" && entry.Kind != "pumping" && !(entry.Kind == "feeding" && entry.FeedingMode == "breast")) ||
            (entry.FeedingMode == "breast" && entry.AmountMl is not null) ||
            (entry.AmountGrams is not null && (entry.Kind != "solids" || entry.AmountGrams is < 0 or > 2000)))
            throw new CareException("수유 종류·방향과 이유식 양을 확인해주세요.");
        if (entry.PhotoIds is null || entry.PhotoIds.Count > 3 || entry.PhotoIds.Distinct().Count() != entry.PhotoIds.Count || entry.PhotoIds.Any(id => !BabyCare.Infrastructure.CarePhotos.ValidId(id)))
            throw new CareException("기록에는 사진을 최대 3장 첨부할 수 있어요.");
        if (entry.LeftAmountMl is < 0 or > 2000 || entry.RightAmountMl is < 0 or > 2000 ||
            (entry.Kind != "pumping" && (entry.LeftAmountMl is not null || entry.RightAmountMl is not null || entry.PumpMethod.Length > 0)) ||
            (entry.Kind == "pumping" && entry.PumpMethod is not ("" or "electric" or "manual" or "hand")))
            throw new CareException("유축 용량과 방법을 확인해주세요.");
        if (entry.StartedAt == default || entry.StartedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new CareException("시작 시각을 확인해주세요. 미래 기록은 저장할 수 없습니다.");
        if (entry.EndedAt is { } end && (end < entry.StartedAt || end > DateTimeOffset.UtcNow.AddMinutes(5)))
            throw new CareException("종료 시각은 시작 이후이며 현재 시각 이전이어야 합니다.");
        if (!kind.HasDuration && entry.EndedAt is not null) throw new CareException("이 기록은 종료 시각을 사용하지 않습니다.");
        if (entry.AmountMl is < 0 or > 2000 || (!kind.HasAmount && entry.AmountMl is not null))
            throw new CareException("섭취량은 수유 기록에 0~2000mL로 입력해주세요.");
        if (entry.ExpectedDurationMinutes is not null &&
            (entry.Kind != "feeding" || entry.ExpectedDurationMinutes is < 1 or > 180 || entry.EndedAt is not null))
            throw new CareException("수유 예상 시간은 종료 시각 없이 1~180분으로 입력해주세요.");
        if (entry.Kind == "temperature" && (entry.TemperatureC is null or < 30 or > 45))
            throw new CareException("체온은 30~45°C 범위의 측정값을 입력해주세요.");
        if (entry.Kind != "temperature" && entry.TemperatureC is not null)
            throw new CareException("체온은 체온 기록에만 입력해주세요.");
        if (entry.Kind == "diaper" && entry.Detail is not ("wet" or "dirty" or "mixed"))
            throw new CareException("기저귀 종류를 선택해주세요.");
        if ((entry.StoolColor is not null && (!CareObservations.HasStool(entry) || !CareObservations.StoolColors.Any(c => c.Id == entry.StoolColor))) ||
            (entry.UrineColor is not null && (!CareObservations.HasUrine(entry) || !CareObservations.UrineColors.Any(c => c.Id == entry.UrineColor))) ||
            (entry.StoolColorConfirmed && entry.StoolColor is null) || (entry.UrineColorConfirmed && entry.UrineColor is null))
            throw new CareException("색상 선택을 확인해주세요.");
        if (entry.NoteTranslations is null || entry.NoteTranslations.Count > 10 || entry.NoteTranslations.Any(p =>
            !CareLanguages.Codes.Contains(p.Key) || p.Value?.Length > 2000))
            throw new CareException("처리하지 못했습니다. 입력 내용을 유지했으니 다시 시도해주세요.");
        if (entry.Note.Length > 1000 || entry.Detail.Length > 80) throw new CareException("메모가 너무 깁니다.");
    }
}
