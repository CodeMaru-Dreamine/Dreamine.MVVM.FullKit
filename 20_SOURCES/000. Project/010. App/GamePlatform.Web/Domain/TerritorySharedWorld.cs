namespace GamePlatform.Domain;

public sealed class TerritorySharedWorld
{
    public Dictionary<string, TerritoryWorldMember> Members { get; set; } = new();
    public List<TerritoryRally> Rallies { get; set; } = [];
    public List<TerritoryRaid> Raids { get; set; } = [];
    public Dictionary<int, DateTime> BossRespawns { get; set; } = new();
}
public sealed class TerritoryWorldMember
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Tile { get; set; }
    public int Level { get; set; }
    public DateTime JoinedUtc { get; set; }
    public DateTime ShieldUntilUtc { get; set; }
    public HashSet<string> ShieldPurchaseIds { get; set; } = [];
}
public sealed class TerritoryRally
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string LeaderId { get; set; } = "";
    public int Boss { get; set; }
    public DateTime DepartsUtc { get; set; }
    public DateTime BattleEndsUtc { get; set; }
    public DateTime ReturnsUtc { get; set; }
    public Dictionary<string, string> Armies { get; set; } = new();
    public List<TerritorySquad> Squads { get; set; } = [];
    public int Power { get; set; }
    public int NpcSupportPower { get; set; }
    public bool SoloHunt { get; set; }
    public bool Resolved { get; set; }
    public bool Returned { get; set; }
    public bool Cancelled { get; set; }
    public bool Victory { get; set; }
}
public sealed class TerritoryRaid
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Attacker { get; set; } = "";
    public string Defender { get; set; } = "";
    public string MarchId { get; set; } = "";
    public DateTime BattleEndsUtc { get; set; }
    public DateTime ReturnsUtc { get; set; }
    public bool Resolved { get; set; }
    public bool Returned { get; set; }
    public bool Cancelled { get; set; }
    public bool Victory { get; set; }
    public int Defense { get; set; }
    public long[] Loot { get; set; } = [0, 0, 0];
}
public sealed record TerritoryWorldBoss(int Id, int Tile, string Name, int Power, string Asset, int Reward, string Kind = "boss", int Level = 1, int RespawnSeconds = 600)
{
    public bool RequiresRally => Kind == "boss";
    public string KindLabel => Kind == "boss" ? "집결 보스" : Kind == "elite" ? "정예" : "일반";
}

public static class TerritorySharedRules
{
    public const int PvpLevel = 10;
    public const int InitialShieldHours = 72;
    public static readonly (int Hours, int Coins)[] Shields = [(8, 50), (24, 120), (72, 300), (168, 600)];
    public static readonly TerritoryWorldBoss[] Bosses = TerritoryMonsterCatalog.Create();
    public static bool Managed(TerritoryMarch march) => march.Mission is "world-rally" or "world-raid";
    public static string Protection(TerritoryWorldMember m, DateTime now) => m.Level < PvpLevel ? "초보 보호 · Lv.10 미만" : m.ShieldUntilUtc > now ? "보호막 활성" : "공격 가능";
    public static string? AttackBlocked(TerritoryWorldMember a, TerritoryWorldMember d, DateTime now) => a.UserId == d.UserId ? "자신의 본성을 공격할 수 없습니다."
        : a.Level < PvpLevel ? "영주관 Lv.10부터 본성을 공격할 수 있습니다." : d.Level < PvpLevel ? "초보 보호 대상입니다."
        : d.ShieldUntilUtc > now ? "상대 본성의 보호막이 활성 상태입니다." : null;

    public static void Register(TerritorySharedWorld world, GameProgress p, DateTime now)
    {
        if (!world.Members.TryGetValue(p.UserId, out var member))
        {
            var occupied = world.Members.Values.Select(m => m.Tile).Concat(Bosses.Select(b => b.Tile)).ToHashSet();
            var tile = Enumerable.Range(0, TerritoryRules.WorldTileCount).OrderBy(TerritoryRules.Distance).FirstOrDefault(t => !occupied.Contains(t), -1);
            // Growing wildlife must not reduce the world's existing castle capacity.
            // If open settlement lots are full, retire an unreserved spawn under the new castle.
            if (tile < 0)
            {
                var reserved = world.Members.Values.Select(m => m.Tile).Concat(world.Rallies.Where(r => !r.Returned).Select(r => Bosses[r.Boss].Tile)).ToHashSet();
                tile = Enumerable.Range(0,TerritoryRules.WorldTileCount).OrderBy(TerritoryRules.Distance).FirstOrDefault(t => !reserved.Contains(t),-1);
            }
            if (tile < 0) throw new InvalidOperationException("서버 월드의 본성 배치 공간이 가득 찼습니다.");
            member = new() { UserId = p.UserId, Tile = tile, JoinedUtc = now, ShieldUntilUtc = now.AddHours(InitialShieldHours) };
            world.Members.Add(p.UserId, member);
        }
        member.Name = string.IsNullOrWhiteSpace(p.GameNickname) ? "이름 미설정" : p.GameNickname;
        member.Level = p.Territory.Buildings[0];
    }

    public static string? Reserve(GameProgress p, TerritoryFormation? formation, string mission, int tile, DateTime now, DateTime returns, out TerritoryMarch? march)
    {
        march = null;
        var s = p.Territory;
        var invalid = TerritoryRules.ValidateFormation(s, p, formation);
        if (invalid is not null) return invalid;
        if (TerritoryRules.ActiveMarches(s).Count() >= TerritoryRules.MarchSlots(s)) return "출정 슬롯이 부족합니다.";
        if (s.Research is not null) return "전군 훈련 중에는 출정할 수 없습니다.";
        if (s.Food < 10) return "출정 식량 10이 필요합니다.";
        if (mission == "world-rally" && s.MonsterEnergy < MonsterEnergyRules.AttackCost) return "몬스터 공격 에너지가 부족합니다.";
        var squads = TerritoryRules.ResolveSquads(p, formation!);
        var preview = TerritoryRules.PreviewBattle(s, tile, squads);
        march = new() { Mission = mission, Tile = tile, Squads = squads, Power = preview.EffectivePower, BasePower = preview.BasePower,
            StartedUtc = now, ReturnsUtc = returns, Slot = formation!.Slot > 0 ? formation.Slot : Enumerable.Range(1, TerritoryRules.MarchSlots(s)).First(n => !TerritoryRules.ActiveMarches(s).Any(m => m.Slot == n)) };
        foreach (var squad in squads) { s.Troops[squad.TroopType] -= squad.Count; march.Troops[squad.TroopType] = squad.Count; }
        s.Food -= 10;
        if (mission == "world-rally") s.MonsterEnergy -= MonsterEnergyRules.AttackCost;
        if (s.March is null) s.March = march; else s.AdditionalMarches.Add(march);
        return null;
    }

    public static (bool Success, string Message) Execute(TerritorySharedWorld world, IDictionary<string, GameProgress> players,
        string actorId, string action, string? target, int bossId, TerritoryFormation? formation, DateTime now)
    {
        var actor = players[actorId];
        Register(world, actor, now);
        Settle(world, players, now);
        if (action == "load") return (true, "");
        if (action == "shield-buy")
        {
            if (bossId < 0 || bossId >= Shields.Length || !Guid.TryParse(target, out _)) return (false, "구매 요청을 확인하세요.");
            var member = world.Members[actorId];
            if (member.ShieldPurchaseIds.Contains(target!)) return (true, "이미 처리한 보호막 구매입니다.");
            if (world.Raids.Any(r => r.Attacker == actorId && !r.Returned)) return (false, "본성 공격 부대가 귀환한 뒤 보호막을 사용할 수 있습니다.");
            var product = Shields[bossId];
            if (actor.PremiumCurrency < product.Coins) return (false, $"CodeMaru C {product.Coins}가 필요합니다.");
            actor.PremiumCurrency -= product.Coins;
            member.ShieldUntilUtc = (member.ShieldUntilUtc > now ? member.ShieldUntilUtc : now).AddHours(product.Hours);
            member.ShieldPurchaseIds.Add(target!);
            return (true, $"보호막 {product.Hours}시간 연장 · C {product.Coins} 사용");
        }
        if (action is "rally-create" or "hunt")
        {
            if (bossId < 0 || bossId >= Bosses.Length) return (false, "집결 대상을 확인하세요.");
            var monster = Bosses[bossId];
            if ((action == "rally-create") != monster.RequiresRally) return (false, "일반·정예는 단독 토벌, 보스는 집결로 출정하세요.");
            if (world.Members.Values.Any(m => m.Tile == monster.Tile)) return (false, "본성이 배치된 위치입니다. 다른 사냥터를 선택하세요.");
            if (world.Rallies.Any(r => r.Boss == bossId && !r.Returned)) return (false, "해당 보스를 향한 집결이 이미 있습니다.");
            if (world.BossRespawns.GetValueOrDefault(bossId) > now) return (false, "보스 재출현을 기다려 주세요.");
            var departure = action == "hunt" ? now : now.AddSeconds(60);
            var rally = new TerritoryRally { LeaderId = actorId, Boss = bossId, SoloHunt = action == "hunt", DepartsUtc = departure, BattleEndsUtc = departure.AddSeconds(70), ReturnsUtc = departure.AddSeconds(130) };
            var error = Reserve(actor, formation, "world-rally", Bosses[bossId].Tile, now, rally.ReturnsUtc, out var march);
            if (error is not null) return (false, error);
            rally.Armies.Add(actorId, march!.Id); rally.Power = march.Power; rally.Squads.AddRange(march.Squads);
            world.Rallies.Add(rally);
            return (true, rally.SoloHunt ? $"{monster.Name} 단독 토벌 출정 · 에너지 10 사용 · 같은 서버에 토벌 대상 예약" : "집결을 열었습니다. 60초 동안 유저를 모집하며, 혼자 남으면 NPC 지원군 1부대가 자동 합류합니다.");
        }
        if (action is "rally-join" or "rally-cancel")
        {
            var rally = world.Rallies.FirstOrDefault(r => r.Id == target);
            if (rally is null || rally.SoloHunt || rally.Resolved || now >= rally.DepartsUtc) return (false, "모집이 끝난 집결입니다.");
            if (action == "rally-cancel")
            {
                if (rally.LeaderId != actorId) return (false, "집결장만 취소할 수 있습니다.");
                rally.Cancelled = rally.Resolved = true; rally.ReturnsUtc = now; Settle(world, players, now);
                return (true, "집결을 취소하고 모든 부대를 복귀시켰습니다.");
            }
            if (rally.Armies.ContainsKey(actorId)) return (false, "이미 참여한 집결입니다.");
            if (rally.Armies.Count >= 5) return (false, "집결 정원은 5명입니다.");
            var error = Reserve(actor, formation, "world-rally", Bosses[rally.Boss].Tile, now, rally.ReturnsUtc, out var march);
            if (error is not null) return (false, error);
            rally.Armies.Add(actorId, march!.Id); rally.Power += march.Power; rally.Squads.AddRange(march.Squads);
            return (true, "집결에 합류했습니다. 부대와 대장은 귀환까지 다른 출정에 사용할 수 없습니다.");
        }
        if (action == "raid")
        {
            if (target is null || !world.Members.TryGetValue(target, out var defender) || !players.ContainsKey(target)) return (false, "같은 서버의 본성을 선택하세요.");
            var blocked = AttackBlocked(world.Members[actorId], defender, now);
            if (blocked is not null) return (false, blocked);
            if (world.Raids.Any(r => r.Attacker == actorId && !r.Returned)) return (false, "기존 본성 공격 부대의 귀환을 기다려 주세요.");
            var raid = new TerritoryRaid { Attacker = actorId, Defender = target, BattleEndsUtc = now.AddSeconds(70), ReturnsUtc = now.AddSeconds(130) };
            var error = Reserve(actor, formation, "world-raid", defender.Tile, now, raid.ReturnsUtc, out var march);
            if (error is not null) return (false, error);
            raid.MarchId = march!.Id; world.Members[actorId].ShieldUntilUtc = now;
            world.Raids.Add(raid);
            Report(players[target].Territory, $"{world.Members[actorId].Name}의 본성 공격 부대가 접근 중입니다.");
            return (true, "본성 공격 출정 · 자신의 보호막이 해제되었습니다. 도착 시 초보 보호와 보호막을 다시 확인합니다.");
        }
        return (false, "지원하지 않는 월드 명령입니다.");
    }

    public static void Settle(TerritorySharedWorld world, IDictionary<string, GameProgress> players, DateTime now)
    {
        var events = world.Rallies.Where(r => !r.Returned).SelectMany(r => new[] { r.DepartsUtc, r.BattleEndsUtc, r.ReturnsUtc })
            .Concat(world.Raids.Where(r => !r.Returned).SelectMany(r => new[] { r.BattleEndsUtc, r.ReturnsUtc }))
            .Where(t => t <= now).Append(now).Distinct().OrderBy(t => t).ToArray();
        foreach (var time in events)
        {
            foreach (var p in players.Values.Where(p => world.Members.TryGetValue(p.UserId, out var member) && member.JoinedUtc <= time))
            { TerritoryRules.Settle(p.Territory, time); Register(world, p, time); }
            SettleAt(world, players, time);
        }
    }

    private static void SettleAt(TerritorySharedWorld world, IDictionary<string, GameProgress> players, DateTime now)
    {
        foreach (var rally in world.Rallies.Where(r => !r.Returned))
        {
            if (!rally.SoloHunt && !rally.Resolved && now >= rally.DepartsUtc && rally.Armies.Count == 1 && rally.NpcSupportPower == 0)
            {
                // NPCs are explicit support, never fake player accounts or reward recipients.
                rally.NpcSupportPower = Math.Min(Bosses[rally.Boss].Power / 2, Math.Max(600, rally.Power));
                rally.Power += rally.NpcSupportPower;
                var leader = rally.Squads.FirstOrDefault();
                rally.Squads.Add(new TerritorySquad { TroopType = 0, Count = 30, CommanderId = "npc-cheongun-support", CommanderName = "청운 지원군 · NPC", MarchAssetUrl = leader?.MarchAssetUrl ?? "" });
            }
            if (!rally.Resolved && now >= rally.BattleEndsUtc) { rally.Resolved = true; rally.Victory = rally.Power >= Bosses[rally.Boss].Power; if (rally.Victory) world.BossRespawns[rally.Boss] = rally.BattleEndsUtc.AddSeconds(Bosses[rally.Boss].RespawnSeconds); }
            if (!rally.Resolved || now < rally.ReturnsUtc) continue;
            foreach (var (id, marchId) in rally.Armies)
            {
                if (!players.TryGetValue(id, out var p)) continue;
                var march = TerritoryRules.ActiveMarches(p.Territory).FirstOrDefault(m => m.Id == marchId);
                if (march is null) continue;
                if (rally.Cancelled) p.Territory.MonsterEnergy = Math.Min(MonsterEnergyRules.Maximum, p.Territory.MonsterEnergy + MonsterEnergyRules.AttackCost);
                if (!rally.Cancelled) Casualties(p.Territory, march, rally.Power, Bosses[rally.Boss].Power, rally.ReturnsUtc, Bosses[rally.Boss].Name, Bosses[rally.Boss].Name, Bosses[rally.Boss].Asset);
                if (rally.Victory) Reward(p.Territory, Bosses[rally.Boss].Reward);
                Return(p.Territory, march);
                Report(p.Territory, rally.Cancelled ? "집결 취소 · 부대 전원 복귀" : $"{Bosses[rally.Boss].Name} {(rally.SoloHunt ? "토벌" : "집결")} {(rally.Victory ? "승리 · 자원 보상 지급" : "패배")} · 부대 복귀");
            }
            rally.Returned = true;
        }
        foreach (var raid in world.Raids.Where(r => !r.Returned))
        {
            var a = players[raid.Attacker].Territory; var d = players[raid.Defender].Territory;
            var march = TerritoryRules.ActiveMarches(a).FirstOrDefault(m => m.Id == raid.MarchId);
            if (march is null) { raid.Returned = true; continue; }
            if (!raid.Resolved && now >= raid.BattleEndsUtc)
            {
                raid.Resolved = true;
                raid.Cancelled = AttackBlocked(world.Members[raid.Attacker], world.Members[raid.Defender], raid.BattleEndsUtc) is not null;
                if (!raid.Cancelled)
                {
                    raid.Defense = TerritoryRules.ArmyPower(d) + d.Buildings[0] * 100;
                    raid.Victory = march.BasePower >= raid.Defense;
                    var defenseMarch = new TerritoryMarch { Id = raid.Id, Slot = 1, Tile = world.Members[raid.Defender].Tile, Troops = d.Troops.ToArray(), ReturnsUtc = raid.BattleEndsUtc };
                    Casualties(d, defenseMarch, raid.Defense, march.BasePower, raid.BattleEndsUtc, "본성 방어", world.Members[raid.Attacker].Name, TerritoryWorldVisuals.MarchAsset(march.Squads.FirstOrDefault()));
                    d.Troops = defenseMarch.Troops;
                    if (raid.Victory)
                    {
                        raid.Loot = new[] { d.Food, d.Wood, d.Stone }.Select(n => Math.Min(500, n / 20)).ToArray();
                        d.Food -= raid.Loot[0]; d.Wood -= raid.Loot[1]; d.Stone -= raid.Loot[2];
                        world.Members[raid.Defender].ShieldUntilUtc = raid.BattleEndsUtc.AddHours(1);
                    }
                    Report(d, $"{world.Members[raid.Attacker].Name} 본성 공격 {(raid.Victory ? "방어 실패 · 1시간 회복 보호막" : "방어 성공")} · 약탈 {raid.Loot.Sum():N0}");
                }
            }
            if (!raid.Resolved || now < raid.ReturnsUtc) continue;
            if (!raid.Cancelled) Casualties(a, march, march.BasePower, raid.Defense, raid.ReturnsUtc, "본성 공격", world.Members[raid.Defender].Name, TerritoryRules.Commanders(players[raid.Defender]).FirstOrDefault()?.AssetUrl);
            a.Food = AddResource(a.Food, raid.Loot[0], a); a.Wood = AddResource(a.Wood, raid.Loot[1], a); a.Stone = AddResource(a.Stone, raid.Loot[2], a);
            Return(a, march); raid.Returned = true;
            Report(a, raid.Cancelled ? "상대 보호 상태로 공격 취소 · 전원 귀환" : $"본성 공격 {(raid.Victory ? "승리" : "패배")} · 약탈 {raid.Loot.Sum():N0} (창고 여유까지 수령) · 귀환");
        }
        world.Rallies = world.Rallies.Where(r => !r.Returned || r.ReturnsUtc > now.AddDays(-1)).ToList();
        world.Raids = world.Raids.Where(r => !r.Returned || r.ReturnsUtc > now.AddDays(-1)).ToList();
    }
    private static void Casualties(TerritoryState state, TerritoryMarch march, int attack, int defense, DateTime now, string? title = null, string? opponent = null, string? asset = null)
    {
        var record = TerritoryBattleRules.Snapshot(new TerritoryMarch { Id = march.Id, Tile = march.Tile, Slot = march.Slot, Troops = march.Troops.ToArray(), Squads = march.Squads, Power = attack, EnemyPower = Math.Max(1, defense), ReturnsUtc = now }, Math.Max(0, TerritoryRules.HospitalCapacity(state) - TerritoryRules.HospitalOccupied(state)));
        for (var i = 0; i < 3; i++) { march.Troops[i] -= record.Losses[i]; state.Wounded[i] += record.Wounded[i]; }
        record.EncounterTitle = title; record.OpponentName = opponent; record.OpponentAsset = asset;
        state.Battles.Insert(0, record); state.Battles = state.Battles.Take(10).ToList();
    }
    private static void Return(TerritoryState s, TerritoryMarch m)
    {
        if (s.March?.Id == m.Id) { s.March = s.AdditionalMarches.FirstOrDefault(); if (s.March is not null) s.AdditionalMarches.Remove(s.March); }
        else s.AdditionalMarches.Remove(m);
        for (var i = 0; i < 3; i++) s.Troops[i] = Math.Min(999, s.Troops[i] + m.Troops[i]);
    }
    private static long AddResource(long value, long reward, TerritoryState state) => value + Math.Min(Math.Max(0, TerritoryRules.StorageCapacity(state) - value), reward);
    private static void Reward(TerritoryState s, int amount) { s.Food = AddResource(s.Food, amount, s); s.Wood = AddResource(s.Wood, amount, s); s.Stone = AddResource(s.Stone, amount, s); }
    private static void Report(TerritoryState s, string message) { s.Reports.Insert(0, message); s.Reports = s.Reports.Take(12).ToList(); }
}
