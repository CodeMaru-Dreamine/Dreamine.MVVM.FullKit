namespace GamePlatform.Domain;

public static partial class TerritoryRules
{
    public const int WorldColumns = 25;
    public const int WorldTileCount = WorldColumns * WorldColumns;
    // IDs 0–24 remain the original starting province. Never renumber saved claims or marches.
    private static readonly (int Column, int Row)[] WorldCoordinates = CreateWorldCoordinates();
    private static readonly Dictionary<(int Column, int Row), int> WorldIds = WorldCoordinates
        .Select((position, id) => (position, id)).ToDictionary(x => x.position, x => x.id);
    private static (int, int)[] CreateWorldCoordinates()
    {
        var cells = new List<(int, int)>();
        for (var row = 10; row < 15; row++)
            for (var column = 10; column < 15; column++) cells.Add((column, row));
        for (var row = 0; row < WorldColumns; row++)
            for (var column = 0; column < WorldColumns; column++)
                if (column is < 10 or > 14 || row is < 10 or > 14) cells.Add((column, row));
        return cells.ToArray();
    }
    public static int TileColumn(int tile) => WorldCoordinates[tile].Column;
    public static int TileRow(int tile) => WorldCoordinates[tile].Row;
    public static int TileAt(int column, int row) => WorldIds.GetValueOrDefault((column, row), -1);
    public static bool IsRegionalCapital(int tile) => tile >= 25 && TileColumn(tile) % 5 == 2 && TileRow(tile) % 5 == 2;
    public static string RegionName(int tile)
    {
        string[] names = ["설령 북원", "빙하 요새", "북천 설산", "은빛 고원", "폭풍 해안",
            "비취 삼림", "청죽 산림", "운무 협곡", "옥룡 산맥", "벽해 동부",
            "황혼 서역", "천수 분지", "청운 변경", "동명 평야", "낙조 해안",
            "적월 황야", "홍련 산지", "금빛 곡창", "남천 수림", "비룡 습지",
            "흑사 사막", "잿빛 폐허", "남방 변경", "월영 밀림", "망각의 끝"];
        return names[TileRow(tile) / 5 * 5 + TileColumn(tile) / 5];
    }
}
