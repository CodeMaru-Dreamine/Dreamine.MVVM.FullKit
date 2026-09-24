using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class TerritoryWorldTests
{
    private static readonly DateTime Now = new(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);
    [Fact]
    public void TrainingCannotExceedBarracks_AndCostsAreNotSpentWhenBlocked()
    {
        var state = new TerritoryState();
        Assert.False(TerritoryRules.Execute(state, "train", 0, 1, Now).Success);
        Assert.Equal(600, state.Food);
        Assert.Equal(400, state.Stone);
        Assert.Null(state.Research);
        state.Buildings[4] = 2;
        Assert.True(TerritoryRules.Execute(state, "train", 0, 1, Now).Success);
        Assert.Equal(1, TerritoryRules.EffectiveTraining(state));
        TerritoryRules.Settle(state, Now.AddSeconds(90));
        Assert.Equal(2, TerritoryRules.EffectiveTraining(state));
        Assert.False(TerritoryRules.Execute(state, "train", 0, 1, Now.AddSeconds(90)).Success);
    }

    [Fact]
    public void LegacyTrainingPreservedButOnlyBarracksLevelAffectsPower()
    {
        var state = new TerritoryState { Training = 8 };
        TerritoryRules.Normalize(state);
        Assert.Equal(8, state.Training);
        Assert.Equal(1, TerritoryRules.EffectiveTraining(state));
        Assert.Equal(204, TerritoryRules.ArmyPower(state));
        var preview = TerritoryRules.PreviewBattle(state, 7, [new() { TroopType = 0, Count = 10 }]);
        Assert.Equal(120, preview.BasePower);
        state.Buildings[4] = 3;
        Assert.Equal(3, TerritoryRules.EffectiveTraining(state));
        Assert.Equal(272, TerritoryRules.ArmyPower(state));
        state.Research = new() { Kind = "train", TargetLevel = 9, StartedUtc = Now, CompletesUtc = Now.AddSeconds(1) };
        TerritoryRules.Settle(state, Now.AddSeconds(2));
        Assert.Equal(9, state.Training);
        Assert.Equal(3, TerritoryRules.EffectiveTraining(state));
        Assert.Null(state.Research);
    }

    [Fact]
    public void BoostKeepsPositionContinuousAndSpeedsUpRemainingJourney()
    {
        var state = new TerritoryState { March = new() { Tile = 7, Mission = "scout", Troops = [5, 0, 0], StartedUtc = Now, ReturnsUtc = Now.AddSeconds(90) } };
        var before = TerritoryRules.MarchProgress(state.March, Now.AddSeconds(30));
        Assert.True(TerritoryRules.Execute(state, "boost", 0, 1, Now.AddSeconds(30)).Success);
        Assert.Equal(before, TerritoryRules.MarchProgress(state.March!, Now.AddSeconds(30)), 10);
        Assert.Equal(2d / 3, TerritoryRules.MarchProgress(state.March!, Now.AddSeconds(45)), 10);
        Assert.Equal(1, TerritoryRules.MarchProgress(state.March!, Now.AddSeconds(60)));
        var loaded = TerritoryRules.Clone(state);
        Assert.Equal(before, loaded.March!.JourneyProgress);
        Assert.Equal(Now.AddSeconds(30), loaded.March.JourneyUpdatedUtc);
        Assert.Equal(2d / 3, TerritoryRules.MarchProgress(loaded.March, Now.AddSeconds(45)), 10);
    }

    [Fact]
    public void LegacyMarchHasFiniteVisualPositionAndFallbackCharacter()
    {
        var march = new TerritoryMarch { Tile = 7, ReturnsUtc = Now.AddSeconds(90) };
        Assert.Equal(Now, TerritoryRules.MarchVisualAnchor(march));
        Assert.Equal(.5, TerritoryRules.MarchProgress(march, Now.AddSeconds(45)));
        Assert.StartsWith("/images/", TerritoryWorldVisuals.MarchAsset(null));
        Assert.Equal(3500, TerritoryWorldVisuals.X(12));
        Assert.Equal(3500, TerritoryWorldVisuals.Y(12));
    }

    [Fact]
    public void MarchAppearanceIsResolvedFromOwnedSkinNotCallerAsset()
    {
        var p = new GameProgress { OwnedCompanionIds = ["haejin"] };
        var requested = new TerritoryFormation { Squads = [new() { TroopType = 0, Count = 8, CommanderId = "haejin", MarchAssetUrl = "https://untrusted.invalid/sprite.png", MarchSkinId = "forged" }] };
        var baseline = TerritoryRules.ResolveSquads(p, requested).Single();
        Assert.NotEqual("forged", baseline.MarchSkinId);
        Assert.StartsWith("/images/", baseline.MarchAssetUrl);
        Assert.True(TerritoryRules.Execute(p.Territory, "conquer", 7, 1, Now, p, requested).Success);
        var saved = TerritoryRules.Clone(p.Territory).March!.Squads.Single();
        Assert.Equal(baseline.MarchAssetUrl, saved.MarchAssetUrl);
        Assert.Equal(baseline.MarchSkinId, saved.MarchSkinId);
        Assert.Equal(baseline.CommandBonus, saved.CommandBonus);
    }
}
