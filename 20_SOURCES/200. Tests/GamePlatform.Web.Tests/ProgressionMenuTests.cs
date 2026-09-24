using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class ProgressionMenuTests
{
    [Fact]
    public async Task CompanionRank_ContinuesPastTenAndConsumesExactShardCosts()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("limit-break", value =>
        {
            value.HighestClearedStage = 100;
            value.AttackLevel = 100;
            value.CompanionRanks["haejin"] = 10;
            value.CompanionShards["haejin"] = 168;
            value.CompanionShards["seolbi"] = 20;
        });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("limit-break");
        var before = initial.CompanionGrowth.Single(x => x.Hero.HeroId == "haejin");
        var first = await session.RankUpCompanionAsync("haejin");
        Assert.True(first.Accepted);
        var next = first.State.CompanionGrowth.Single(x => x.Hero.HeroId == "haejin");
        Assert.Equal(11, next.Rank);
        Assert.Equal(88, next.Shards);
        Assert.Equal(88, next.RankUpShardCost);
        Assert.Equal(before.AttackPercent + 3, next.AttackPercent);
        Assert.Equal(before.HealthPercent + 4, next.HealthPercent);
        Assert.Equal(before.DefensePercent + 2, next.DefensePercent);
        Assert.True(first.State.TrialTowerPartyPower > initial.TrialTowerPartyPower);
        var second = await session.RankUpCompanionAsync("haejin");
        Assert.True(second.Accepted);
        var restored = await SessionFactory.Create(store).InitializeAsync("limit-break");
        Assert.Equal(12, restored.CompanionGrowth.Single(x => x.Hero.HeroId == "haejin").Rank);
        Assert.Equal(0, restored.CompanionGrowth.Single(x => x.Hero.HeroId == "haejin").Shards);
        Assert.Equal(20, restored.CompanionShards["seolbi"]);
        Assert.False((await session.RankUpCompanionAsync("haejin")).Accepted);
    }

    [Fact]
    public void CompanionRank_ExtremeValuesCannotOverflowCostsOrPower()
    {
        Assert.Equal(80, CompanionProgressionRules.RankUpShardCost(10));
        Assert.Equal(88, CompanionProgressionRules.RankUpShardCost(11));
        Assert.Equal(int.MaxValue, CompanionProgressionRules.RankUpShardCost(int.MaxValue));
        Assert.False(CompanionProgressionRules.CanRankUp(int.MaxValue));
        var progress = new GameProgress();
        foreach (var hero in PartyRules.AllCompanions) progress.CompanionRanks[hero.HeroId] = int.MaxValue;
        var growth = CompanionProgressionRules.Resolve(progress, PartyRules.AllCompanions.First());
        Assert.Equal(int.MaxValue, growth.AttackPercent);
        Assert.Equal(int.MaxValue, growth.HealthPercent);
        var party = PartyRules.Resolve(10000, "yeonsu", null, long.MaxValue, null, progress);
        Assert.Equal(long.MaxValue, party.PartyAttackPower);
    }

    [Fact]
    public async Task SwordArtTraining_DeductsServerCostAndPersistsAttackBonus()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("sword-art", value => value.Gold = 1_000);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("sword-art");

        var trained = await session.UpgradeSwordArtAsync();
        var restored = await SessionFactory.Create(store).InitializeAsync("sword-art");

        Assert.Equal(1, trained.SwordArtLevel);
        Assert.Equal(820, trained.Gold);
        Assert.Equal(GameRules.SwordArtAttackBonus(1), trained.SwordArtAttackBonus);
        Assert.Equal(GameRules.AttackPower(1, 1, 0), trained.AttackPower);
        Assert.Equal(trained.SwordArtLevel, restored.SwordArtLevel);
    }

    [Fact]
    public async Task SwordAndSwordArtBatchGrowth_StopAtAffordableCount()
    {
        var swordStore = new InMemoryProgressStore();
        await swordStore.SeedAsync("sword-batch", value => value.Gold = 1_500);
        var swordSession = SessionFactory.Create(swordStore);
        await swordSession.InitializeAsync("sword-batch");

        var sword = await swordSession.UpgradeSwordAsync(10);

        Assert.Equal(4, sword.AttackLevel);
        Assert.Equal(240, sword.Gold);

        var artStore = new InMemoryProgressStore();
        await artStore.SeedAsync("art-batch", value => value.Gold = 1_500);
        var artSession = SessionFactory.Create(artStore);
        await artSession.InitializeAsync("art-batch");

        var art = await artSession.UpgradeSwordArtAsync(10);

        Assert.Equal(2, art.SwordArtLevel);
        Assert.Equal(600, art.Gold);
    }

    [Fact]
    public async Task SwordAndSwordArtBatchGrowth_ClampMaximumRequestToOneThousand()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("growth-cap", value => value.Gold = 1_000_000_000_000);
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("growth-cap");

        var sword = await session.UpgradeSwordAsync(5_000);
        var art = await session.UpgradeSwordArtAsync(5_000);

        Assert.Equal(initial.AttackLevel + 1_000, sword.AttackLevel);
        Assert.Equal(initial.SwordArtLevel + 1_000, art.SwordArtLevel);
    }

    [Fact]
    public async Task EquipmentTraining_DeductsServerCostAndPersistsAttackBonus()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("equipment", value => value.Gold = 1_000);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("equipment");

        var trained = await session.UpgradeEquipmentAsync();
        var restored = await SessionFactory.Create(store).InitializeAsync("equipment");

        Assert.Equal(1, trained.EquipmentLevel);
        Assert.Equal(740, trained.Gold);
        Assert.Equal(GameRules.EquipmentAttackBonus(1), trained.EquipmentAttackBonus);
        Assert.Equal(GameRules.AttackPower(1, 0, 1), trained.AttackPower);
        Assert.Equal(trained.EquipmentLevel, restored.EquipmentLevel);
        Assert.Equal(1, trained.WeaponEquipmentLevel);
        Assert.Equal(0, trained.ArmorEquipmentLevel);
        Assert.Equal(0, trained.JadeEquipmentLevel);
    }

    [Fact]
    public async Task InheritedEquipment_UpgradesOnlySelectedSlotAndPersistsIndependently()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("independent-equipment", value =>
        {
            value.Gold = 10_000;
            value.AttackLevel = 2;
            value.HighestClearedStage = 100;
            value.HighestSummonRewardedStage = 100;
            value.OwnedCompanionIds = ["haejin"];
            value.SelectedCompanionIds = ["haejin"];
        });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("independent-equipment");

        var armor = await session.UpgradeEquipmentAsync(InheritedEquipmentSlot.Armor, 1);
        var jade = await session.UpgradeEquipmentAsync(InheritedEquipmentSlot.Jade, 2);
        var restored = await SessionFactory.Create(store).InitializeAsync("independent-equipment");

        Assert.Equal(0, armor.WeaponEquipmentLevel);
        Assert.Equal(1, armor.ArmorEquipmentLevel);
        Assert.Equal(0, armor.JadeEquipmentLevel);
        Assert.Equal(initial.AttackPower, armor.AttackPower);
        Assert.True(armor.PlayerMaxHp > initial.PlayerMaxHp);
        Assert.Equal(0, jade.WeaponEquipmentLevel);
        Assert.Equal(1, jade.ArmorEquipmentLevel);
        Assert.Equal(2, jade.JadeEquipmentLevel);
        Assert.Equal(armor.PlayerMaxHp, jade.PlayerMaxHp);
        Assert.True(jade.AttackPower > armor.AttackPower);
        Assert.Equal(GameRules.JadeAssistBonusPercent(2), jade.JadeAssistBonusPercent);
        Assert.Equal(0, restored.WeaponEquipmentLevel);
        Assert.Equal(1, restored.ArmorEquipmentLevel);
        Assert.Equal(2, restored.JadeEquipmentLevel);
    }

    [Fact]
    public async Task EquipmentBatchTraining_StopsAtAffordableLevelAndPersists()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("equipment-batch", value => value.Gold = 10_000);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("equipment-batch");

        var trained = await session.UpgradeEquipmentAsync(10);
        var restored = await SessionFactory.Create(store).InitializeAsync("equipment-batch");

        Assert.Equal(4, trained.EquipmentLevel);
        Assert.Equal(2_200, trained.Gold);
        Assert.Equal(GameRules.EquipmentAttackBonus(4), trained.EquipmentAttackBonus);
        Assert.Equal(4, restored.EquipmentLevel);
    }

    [Fact]
    public async Task InsufficientGold_DoesNotAdvanceGrowth()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("poor", value => value.Gold = 100);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("poor");

        var swordArt = await session.UpgradeSwordArtAsync();
        var equipment = await session.UpgradeEquipmentAsync();

        Assert.Equal(0, swordArt.SwordArtLevel);
        Assert.Equal(0, equipment.EquipmentLevel);
        Assert.Equal(100, equipment.Gold);
    }

    [Fact]
    public async Task Codex_UsesHighestClearedStageForDiscoveryAndCompletion()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("codex", value =>
        {
            value.Stage = 16;
            value.HighestClearedStage = 15;
            value.EnemyHp = GameRules.EnemyMaxHp(16);
        });
        var session = SessionFactory.Create(store);

        var snapshot = await session.InitializeAsync("codex");

        Assert.True(snapshot.Codex[0].Completed);
        Assert.True(snapshot.Codex[1].Discovered);
        Assert.Equal(5, snapshot.Codex[1].ClearedStages);
        Assert.False(snapshot.Codex[2].Discovered);
    }

    [Fact]
    public async Task CompanionLevelUp_SpendsGoldAndPersistsIndependentLevel()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("companion-level", value =>
        {
            value.Gold = 10_000;
            value.AttackLevel = 3;
            value.HighestClearedStage = 100;
        });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("companion-level");
        var cost = initial.CompanionGrowth.Single(item => item.Hero.HeroId == "haejin").LevelUpCost;

        var result = await session.UpgradeCompanionLevelAsync("haejin");
        var restored = await SessionFactory.Create(store).InitializeAsync("companion-level");

        Assert.True(result.Accepted);
        Assert.Equal(10_000 - cost, result.State.Gold);
        Assert.Equal(2, restored.CompanionGrowth.Single(item => item.Hero.HeroId == "haejin").Level);
    }

    [Fact]
    public async Task CompanionLevel_CanExceedMainAttackLevel()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("companion-cap", value =>
        {
            value.AttackLevel = 2;
            value.Gold = 100_000;
            value.OwnedCompanionIds = ["haejin"];
            value.CompanionLevels["haejin"] = 2;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("companion-cap");

        var result = await session.UpgradeCompanionLevelAsync("haejin");

        Assert.True(result.Accepted);
        Assert.Equal(3, result.State.CompanionGrowth.Single(x => x.Hero.HeroId == "haejin").Level);
        Assert.True(result.State.Gold < 100_000);
    }

    [Fact]
    public async Task CompanionRankUp_ConsumesOnlyThatCompanionsShards()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("companion-rank", value =>
        {
            value.HighestClearedStage = 100;
            value.CompanionShards["haejin"] = 20;
            value.CompanionShards["seolbi"] = 11;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("companion-rank");

        var result = await session.RankUpCompanionAsync("haejin");

        Assert.True(result.Accepted);
        var growth = result.State.CompanionGrowth.Single(item => item.Hero.HeroId == "haejin");
        Assert.Equal(2, growth.Rank);
        Assert.Equal(12, growth.Shards);
        Assert.Equal(11, result.State.CompanionShards["seolbi"]);
    }

    [Fact]
    public async Task CompanionEquipment_ChangesPartyAttackAndPersistsPerHero()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("companion-equipment", value => value.HighestClearedStage = 100);
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("companion-equipment");

        var equipped = await session.ToggleCompanionEquipmentAsync("haejin", "spirit-weapon");
        var restored = await SessionFactory.Create(store).InitializeAsync("companion-equipment");

        Assert.True(equipped.Accepted);
        Assert.True(equipped.State.AttackPower > initial.AttackPower);
        var item = restored.CompanionGrowth.Single(value => value.Hero.HeroId == "haejin").Equipment.Single(value => value.Definition.ItemId == "spirit-weapon");
        Assert.Equal("/images/maru-idle/equipment/personal/weapon-spear-v1.png", item.Definition.AssetUrl);
        Assert.Equal(CompanionWeaponType.Spear, item.Definition.WeaponType);
    }

    [Fact]
    public async Task CompanionEquipmentInventory_SupportsUpgradeEquipSellAndCraft()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("equipment-inventory", value =>
        {
            value.HighestClearedStage = 100;
            value.CompanionUpgradeMaterials = 100;
        });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("equipment-inventory");
        var weapon = initial.CompanionEquipmentInventory.Single(item => item.Definition.ItemId == "spirit-weapon");

        var crafted = await session.CraftCompanionEquipmentAsync("haejin", "spirit-weapon");
        var craftedWeapon = crafted.State.CompanionEquipmentInventory
            .Where(item => item.Definition.ItemId == "spirit-weapon")
            .Single(item => item.Instance.InstanceId != weapon.Instance.InstanceId);
        var equipped = await session.ToggleCompanionEquipmentAsync("haejin", weapon.Instance.InstanceId);
        var upgraded = await session.UpgradeCompanionEquipmentAsync("haejin", weapon.Instance.InstanceId);
        var rejectedSale = await session.SellCompanionEquipmentAsync("haejin", weapon.Instance.InstanceId);
        await session.ToggleCompanionEquipmentAsync("haejin", weapon.Instance.InstanceId);
        var sold = await session.SellCompanionEquipmentAsync("haejin", weapon.Instance.InstanceId);

        Assert.True(crafted.Accepted);
        Assert.NotEqual(weapon.DisplayName, craftedWeapon.DisplayName);
        Assert.NotEqual(weapon.AttackPercent, craftedWeapon.AttackPercent);
        Assert.True(equipped.Accepted);
        Assert.True(upgraded.Accepted);
        Assert.Equal(1, upgraded.State.CompanionEquipmentInventory.Single(item => item.Instance.InstanceId == weapon.Instance.InstanceId).Instance.Level);
        Assert.Equal(75, upgraded.State.CompanionUpgradeMaterials);
        Assert.False(rejectedSale.Accepted);
        Assert.True(sold.Accepted);
        Assert.Equal(300, sold.State.Gold);
        Assert.Equal(75, sold.State.CompanionUpgradeMaterials);
        Assert.Equal(3, sold.State.CompanionEquipmentInventory.Count);
    }

    [Fact]
    public async Task CompanionEquipment_AllowsOnlyOneItemPerSlotAndOneOwnerPerInstance()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("equipment-slot-rule", value =>
        {
            value.HighestClearedStage = 100;
            value.CompanionUpgradeMaterials = 100;
        });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("equipment-slot-rule");
        var firstWeapon = initial.CompanionEquipmentInventory.Single(item => item.Definition.ItemId == "spirit-weapon");
        var sharedArmor = initial.CompanionEquipmentInventory.Single(item => item.Definition.Slot == CompanionEquipmentSlot.Armor);
        var crafted = await session.CraftCompanionEquipmentAsync("haejin", "spirit-weapon");
        var secondWeapon = crafted.State.CompanionEquipmentInventory
            .Where(item => item.Definition.ItemId == "spirit-weapon")
            .Single(item => item.Instance.InstanceId != firstWeapon.Instance.InstanceId);

        await session.ToggleCompanionEquipmentAsync("haejin", firstWeapon.Instance.InstanceId);
        var replaced = await session.ToggleCompanionEquipmentAsync("haejin", secondWeapon.Instance.InstanceId);
        await session.ToggleCompanionEquipmentAsync("haejin", sharedArmor.Instance.InstanceId);
        var moved = await session.ToggleCompanionEquipmentAsync("seolbi", sharedArmor.Instance.InstanceId);

        var haejin = moved.State.CompanionGrowth.Single(item => item.Hero.HeroId == "haejin");
        var seolbi = moved.State.CompanionGrowth.Single(item => item.Hero.HeroId == "seolbi");
        Assert.Single(
            replaced.State.CompanionGrowth.Single(item => item.Hero.HeroId == "haejin").Equipment,
            item => item.Definition.Slot == CompanionEquipmentSlot.Weapon);
        Assert.Equal(secondWeapon.Instance.InstanceId, haejin.Equipment.Single(item => item.Definition.Slot == CompanionEquipmentSlot.Weapon).Instance.InstanceId);
        Assert.DoesNotContain(haejin.Equipment, item => item.Definition.Slot == CompanionEquipmentSlot.Armor);
        Assert.Equal(sharedArmor.Instance.InstanceId, seolbi.Equipment.Single(item => item.Definition.Slot == CompanionEquipmentSlot.Armor).Instance.InstanceId);
    }

    [Fact]
    public async Task CompanionEquipment_RejectsWeaponThatDoesNotMatchHeroWeaponType()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("equipment-weapon-type", value =>
        {
            value.HighestClearedStage = 100;
            value.CompanionUpgradeMaterials = 100;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("equipment-weapon-type");
        var crafted = await session.CraftCompanionEquipmentAsync("seolbi", "crescent-bow");
        var bow = crafted.State.CompanionEquipmentInventory.Single(item => item.Definition.ItemId == "crescent-bow");

        var rejected = await session.ToggleCompanionEquipmentAsync("haejin", bow.Instance.InstanceId);
        var accepted = await session.ToggleCompanionEquipmentAsync("seolbi", bow.Instance.InstanceId);

        Assert.False(rejected.Accepted);
        Assert.Contains("창 계열", rejected.Message);
        Assert.True(accepted.Accepted);
        Assert.Equal(CompanionWeaponType.Bow,
            accepted.State.CompanionGrowth.Single(item => item.Hero.HeroId == "seolbi")
                .Equipment.Single(item => item.Definition.Slot == CompanionEquipmentSlot.Weapon).Definition.WeaponType);
    }

    [Fact]
    public async Task PremiumSkin_ConsumesServerCurrencyAndRestoresSelection()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("companion-skin", value => value.HighestClearedStage = 100);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("companion-skin");
        await session.GrantPremiumCurrencyForDevelopmentAsync(600);

        var purchased = await session.PurchaseOrSelectCompanionSkinAsync("haejin", "moon-regalia");
        var restored = await SessionFactory.Create(store).InitializeAsync("companion-skin");
        var growth = restored.CompanionGrowth.Single(item => item.Hero.HeroId == "haejin");

        Assert.True(purchased.Accepted);
        Assert.Equal(300, restored.PremiumCurrency);
        Assert.Equal("moon-regalia", growth.ActiveSkinId);
        Assert.Contains("moon-regalia", growth.OwnedSkinIds);
        Assert.Equal(growth.Hero.SpriteAssetUrl, growth.ActiveSkin.AssetUrl);
        Assert.Equal(growth.ActiveSkin.AssetUrl, restored.CompanionUnits.Single(unit => unit.UnitId == "haejin").AssetUrl);
    }
}
