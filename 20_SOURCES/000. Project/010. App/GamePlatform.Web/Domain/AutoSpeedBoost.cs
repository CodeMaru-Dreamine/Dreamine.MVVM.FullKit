namespace GamePlatform.Domain;

/// <summary>AUTO 공격 속도를 정해진 시간 동안 높이는 보스 전리품 정의입니다.</summary>
public sealed record AutoSpeedBoostDefinition(
    string ItemKey,
    int Multiplier,
    TimeSpan Duration,
    string Name,
    long PriceC,
    string AssetUrl);

/// <summary>사용자가 보유한 AUTO 가속 부적과 수량입니다.</summary>
public sealed record AutoSpeedBoostInventoryEntry(
    AutoSpeedBoostDefinition Item,
    int Count);
