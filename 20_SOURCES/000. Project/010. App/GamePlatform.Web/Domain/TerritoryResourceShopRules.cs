namespace GamePlatform.Domain;

public static class TerritoryResourceShopRules
{
    public static readonly string[] Names = ["식량", "목재", "석재"];
    public static readonly (int Amount, int Coins)[] Packs = [(1000, 10), (10000, 90)];
    public static long Balance(TerritoryState s, int resource) => resource switch { 0 => s.Food, 1 => s.Wood, _ => s.Stone };
    public static (bool Success, string Message) Buy(TerritoryState s, GameProgress? player, int resource, int pack, string? requestId)
    {
        if (player is null || resource is < 0 or > 2 || pack < 0 || pack >= Packs.Length || !Guid.TryParse(requestId, out _)) return (false, "자원 구매 요청을 확인하세요.");
        var receipt = $"{resource}:{pack}";
        if (s.ResourcePurchaseReceipts.TryGetValue(requestId!, out var previous))
            return previous == receipt ? (true, "이미 완료된 자원 구매입니다. 추가 차감하지 않았습니다.") : (false, "다른 상품에 사용된 구매 번호입니다.");
        var product = Packs[pack];
        if (player.PremiumCurrency < product.Coins) return (false, "CodeMaru C가 부족합니다.");
        if (TerritoryRules.StorageCapacity(s) - Balance(s, resource) < product.Amount) return (false, "창고 여유가 부족합니다. 자원을 사용하거나 창고를 확장하세요. 코인은 차감되지 않았습니다.");
        player.PremiumCurrency -= product.Coins;
        if (resource == 0) s.Food += product.Amount; else if (resource == 1) s.Wood += product.Amount; else s.Stone += product.Amount;
        s.ResourcePurchaseReceipts.Add(requestId!, receipt);
        var message = $"{Names[resource]} {product.Amount:N0} 구매 · C {product.Coins} 사용";
        s.Reports.Insert(0, message); s.Reports = s.Reports.Take(12).ToList();
        return (true, message);
    }
}
