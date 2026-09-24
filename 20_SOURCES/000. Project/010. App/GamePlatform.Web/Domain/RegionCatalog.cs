using System.Text.Json;

namespace GamePlatform.Domain;

/// <summary>스테이지 경계와 지역 콘텐츠를 검증하고 조회합니다.</summary>
public sealed class RegionCatalog
{
    private readonly IReadOnlyList<RegionDefinition> _regions;

    /// <summary>검증된 지역 목록으로 카탈로그를 만듭니다.</summary>
    public RegionCatalog(IEnumerable<RegionDefinition> regions)
    {
        _regions = regions.OrderBy(region => region.StartStage).ToArray();
        Validate(_regions);
    }

    /// <summary>JSON 파일에서 검증된 지역 카탈로그를 읽습니다.</summary>
    public static RegionCatalog Load(string path)
    {
        var json = File.ReadAllText(path);
        var regions = JsonSerializer.Deserialize<List<RegionDefinition>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("지역 콘텐츠가 비어 있습니다.");
        return new RegionCatalog(regions);
    }

    /// <summary>현재 스테이지에 맞는 지역, 조명, 적과 프리로드 대상을 조회합니다.</summary>
    public RegionStageView Resolve(int stage)
    {
        var safeStage = Math.Max(1, stage);
        var configuredMax = _regions[^1].EndStage;
        var lookupStage = Math.Min(safeStage, configuredMax);
        var region = _regions.First(item => lookupStage >= item.StartStage && lookupStage <= item.EndStage);
        var localIndex = Math.Max(1, ((safeStage - region.StartStage) % 10) + 1);
        var isBoss = GameRules.IsBossStage(safeStage);
        var lighting = isBoss ? "boss" : localIndex <= 3 ? "day" : localIndex <= 6 ? "sunset" : "night";
        var nextRegion = _regions.FirstOrDefault(item => item.StartStage == region.EndStage + 1);
        var shouldPreloadNext = nextRegion is not null && safeStage >= region.EndStage - 1;
        var preload = shouldPreloadNext
            ? AssetUrl(nextRegion!.BackgroundAssetKey)
            : AssetUrl(region.BackgroundAssetKey);
        var enemyName = isBoss
            ? BossName(region.BossId)
            : EnemyName(region.EnemyPoolId, safeStage);
        var enemyAsset = isBoss && !string.IsNullOrWhiteSpace(region.BossAssetKey)
            ? CharacterAssetUrl(region.BossAssetKey)
            : EnemyAssetUrl(region.EnemyPoolId, hit: false);
        var enemyHitAsset = isBoss && !string.IsNullOrWhiteSpace(region.BossAssetKey)
            ? CharacterAssetUrl(region.BossAssetKey)
            : EnemyAssetUrl(region.EnemyPoolId, hit: true);
        var backgroundKey = isBoss && !string.IsNullOrWhiteSpace(region.BossBackgroundAssetKey)
            ? region.BossBackgroundAssetKey
            : region.BackgroundAssetKey;

        return new RegionStageView(
            region,
            lighting,
            isBoss,
            enemyName,
            enemyAsset,
            enemyHitAsset,
            AssetUrl(backgroundKey),
            preload,
            isBoss ? 2 : 0,
            null);
    }

    /// <summary>최고 클리어 스테이지를 기준으로 지역 도감 진행 정보를 만듭니다.</summary>
    public IReadOnlyList<CodexEntry> CreateCodex(int highestClearedStage) =>
        _regions.Select(region =>
        {
            var totalStages = region.EndStage - region.StartStage + 1;
            var clearedStages = Math.Clamp(highestClearedStage - region.StartStage + 1, 0, totalStages);
            return new CodexEntry(
                region.RegionId,
                region.Name,
                region.StartStage,
                region.EndStage,
                highestClearedStage >= region.StartStage - 1,
                highestClearedStage >= region.EndStage,
                clearedStages,
                totalStages,
                EnemyName(region.EnemyPoolId, region.StartStage));
        }).ToArray();

    private static string AssetUrl(string key) => $"/images/maru-idle/regions/{key}.webp";

    private static string CharacterAssetUrl(string key) => $"/images/maru-idle/{key}.webp";

    private static string EnemyAssetUrl(string poolId, bool hit)
    {
        if (poolId == "reed-watchers")
            return hit ? "/images/maru-idle/guardian-hit.webp" : "/images/maru-idle/guardian-idle.webp";

        var key = poolId switch
        {
            "bell-ruins" => "enemy-bell-fox",
            "ember-gorge" => "enemy-cinder-cat",
            "frost-rampart" => "enemy-frost-yak",
            "rain-harbor" => "enemy-rain-heron",
            "dusk-reliquary" => "enemy-amber-scarab",
            "azure-hollows" => "enemy-blue-salamander",
            "cloud-court" => "enemy-cloud-antelope",
            "rift-march" => "enemy-rift-crawler",
            _ => "enemy-eclipse-lion"
        };
        return $"/images/maru-idle/{key}.webp";
    }

    private static string EnemyName(string poolId, int stage) => poolId switch
    {
        "reed-watchers" => stage % 2 == 0 ? "물안개 사슴" : "갈대 그림자",
        "bell-ruins" => stage % 2 == 0 ? "종각 수호수" : "기와 등짐승",
        "ember-gorge" => stage % 2 == 0 ? "홍엽 표범" : "불씨 산양",
        "frost-rampart" => stage % 2 == 0 ? "설벽 들소" : "서리 매",
        "rain-harbor" => stage % 2 == 0 ? "물결 갑주어" : "우산 해오라기",
        "dusk-reliquary" => stage % 2 == 0 ? "사구 파수꾼" : "유리 전갈",
        "azure-hollows" => stage % 2 == 0 ? "청화 석충" : "등불 맥",
        "cloud-court" => stage % 2 == 0 ? "운해 기린" : "바람 학",
        "rift-march" => stage % 2 == 0 ? "균열 갑각" : "잿빛 추적자",
        _ => stage % 2 == 0 ? "일식 근위수" : "황금 그림자"
    };

    private static string BossName(string bossId) => bossId switch
    {
        "marsh-crown" => "갈대왕 관수",
        "silent-bell" => "침묵의 종지기",
        "red-horn" => "홍각 산주",
        "white-gate" => "백문 성수",
        "tide-judge" => "조수 판관",
        "glass-sun" => "유리 태양수",
        "blue-wake" => "청혼 맥주",
        "sky-seal" => "천인 봉수",
        "rift-heart" => "균심 거수",
        _ => "일식 황정수"
    };

    private static void Validate(IReadOnlyList<RegionDefinition> regions)
    {
        if (regions.Count == 0) throw new InvalidOperationException("지역 콘텐츠가 비어 있습니다.");
        var expectedStart = 1;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in regions)
        {
            if (string.IsNullOrWhiteSpace(region.RegionId) || !ids.Add(region.RegionId))
                throw new InvalidOperationException("지역 ID가 비어 있거나 중복되었습니다.");
            if (region.StartStage != expectedStart || region.EndStage < region.StartStage)
                throw new InvalidOperationException($"지역 {region.RegionId}의 스테이지 범위가 비거나 겹칩니다.");
            if (new[] { region.Name, region.BackgroundAssetKey, region.AmbientEffectKey, region.EnemyPoolId, region.BossId,
                        region.BgmAssetKey, region.BossBgmAssetKey, region.AmbientSoundAssetKey }.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"지역 {region.RegionId}에 필수 콘텐츠 키가 없습니다.");
            expectedStart = region.EndStage + 1;
        }
    }
}
