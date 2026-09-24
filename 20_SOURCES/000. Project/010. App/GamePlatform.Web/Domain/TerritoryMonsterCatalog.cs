namespace GamePlatform.Domain;

/// <summary>Stable encounter IDs: never reorder persisted bosses 0–2 or generated spawn slots.</summary>
public static class TerritoryMonsterCatalog
{
    public static TerritoryWorldBoss[] Create()
    {
        var result = new List<TerritoryWorldBoss> {
            new(0, 7, "청운 황야의 늑대왕", 1200, "/images/maru-idle/enemies/guards/shadow-jackal-v1.png", 300, "boss", 3),
            new(1, 13, "옥갑 산맥의 수호수", 6000, "/images/maru-idle/enemies/guards/stone-lion-v1.png", 800, "boss", 10),
            new(2, 17, "흑월의 고대 수호수", 24000, "/images/maru-idle/enemy-eclipse-lion.webp", 2000, "boss", 20)
        };
        (int X, int Y)[] slots = [(0,0),(2,0),(4,0),(1,2),(3,2),(0,4),(4,4),(2,4),(2,2)];
        for (var region = 0; region < 25; region++)
        for (var slot = 0; slot < slots.Length; slot++)
        {
            var x = region % 5 * 5 + slots[slot].X; var y = region / 5 * 5 + slots[slot].Y;
            var tile = TerritoryRules.TileAt(x,y);
            if (tile == 12 || result.Any(b => b.Tile == tile)) continue;
            var distance = Math.Abs(region % 5 - 2) + Math.Abs(region / 5 - 2);
            var level = 1 + distance * 4 + slot % 3;
            var kind = slot == 8 ? "boss" : slot >= 6 ? "elite" : "normal";
            var species = Species(x,y,slot);
            var multiplier = kind == "boss" ? 5 : kind == "elite" ? 2 : 1;
            var power = (120 + level * level * 45) * multiplier;
            var reward = (40 + level * 20) * multiplier;
            result.Add(new(result.Count,tile,species.Name + (kind == "boss" ? " 군주" : kind == "elite" ? " 우두머리" : ""),power,
                $"/images/maru-idle/{species.Asset}.webp",reward,kind,level,kind == "normal" ? 180 : kind == "elite" ? 300 : 600));
        }
        // Append infill spawns after the original catalog so active encounter IDs stay stable.
        (int X, int Y)[] infill = [(1,1),(3,1),(1,3),(3,3)];
        for (var region=0;region<25;region++)
        for (var slot=0;slot<infill.Length;slot++)
        {
            var x=region%5*5+infill[slot].X; var y=region/5*5+infill[slot].Y;
            var tile=TerritoryRules.TileAt(x,y);
            var level=1+(Math.Abs(region%5-2)+Math.Abs(region/5-2))*4+slot%3;
            var species=Species(x,y,slot);
            result.Add(new(result.Count,tile,species.Name,120+level*level*45,$"/images/maru-idle/{species.Asset}.webp",40+level*20,"normal",level,180));
        }
        return result.ToArray();
    }
    private static (string Name,string Asset) Species(int x,int y,int slot) => y < 5 ? slot % 2 == 0 ? ("빙각 야크","enemy-frost-yak") : ("운봉 영양","enemy-cloud-antelope")
        : y >= 20 && x < 15 ? slot % 2 == 0 ? ("호박 갑충","enemy-amber-scarab") : ("화염 살쾡이","enemy-cinder-cat")
        : x >= 20 ? slot % 2 == 0 ? ("비안 백로","enemy-rain-heron") : ("청염 도롱뇽","enemy-blue-salamander")
        : y >= 15 ? slot % 2 == 0 ? ("균열 포식자","enemy-rift-crawler") : ("일식 사자","enemy-eclipse-lion")
        : slot % 2 == 0 ? ("금령 여우","enemy-bell-fox") : ("운봉 영양","enemy-cloud-antelope");
}
