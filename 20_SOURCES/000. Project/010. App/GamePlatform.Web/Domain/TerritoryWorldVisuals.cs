namespace GamePlatform.Domain;

/// <summary>Presentation-only assets. No skin changes a mission's combat power.</summary>
public static class TerritoryWorldVisuals
{
    public const int WorldSize = TerritoryRules.WorldColumns * 280;
    public static int X(int tile) => 140 + TerritoryRules.TileColumn(tile) * 280;
    public static int Y(int tile) => 140 + TerritoryRules.TileRow(tile) * 280;
    public static string MonsterName(int tile) => TerritoryRules.Terrain(tile) is "quarry" or "citadel" or "camp" ? "옥갑 수호수" : "그림자 늑대";
    public static string MonsterAsset(int tile) => TerritoryRules.Terrain(tile) is "quarry" or "citadel" or "camp"
        ? "/images/maru-idle/enemies/guards/stone-lion-v1.png" : "/images/maru-idle/enemies/guards/shadow-jackal-v1.png";
    public static string MarchAsset(TerritorySquad? squad) => !string.IsNullOrWhiteSpace(squad?.MarchAssetUrl)
        ? squad.MarchAssetUrl : PartyRules.AllCompanions.FirstOrDefault(h => h.HeroId == squad?.CommanderId)?.SpriteAssetUrl
            ?? PartyRules.AllCompanions[0].SpriteAssetUrl;
    public static int ResourceIndex(int tile) => TerritoryRules.Terrain(tile) switch { "forest" => 1, "quarry" => 2, _ => 0 };
}
