using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;
using Microsoft.Data.Sqlite;

namespace GamePlatform.Web.Tests;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task AutoUnlockAndSetting_PersistAcrossStoreReopen()
    {
        var directory = Path.Combine(Path.GetTempPath(), "codemaru-game-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "game.db");
        var options = new GameOptions { DatabasePath = path };
        try
        {
            using (var first = new GameProgressStore(options))
            {
                await first.LoadOrCreateAsync("persistent-player");
                await first.MutateAsync("persistent-player", value =>
                {
                    value.Stage = 16;
                    value.GameNickname = "서버 검객";
                    value.NicknameChangeCount = 2;
                    value.HighestClearedStage = 15;
                    value.AutoAttackUnlocked = true;
                    value.AutoAttackEnabled = true;
                    value.SwordArtLevel = 4;
                    value.WeaponEquipmentLevel = 3;
                    value.ArmorEquipmentLevel = 2;
                    value.JadeEquipmentLevel = 1;
                    value.HighestTrialTowerFloor = 42;
                    value.TrialTowerChallengeTickets = 3;
                    value.TrialTowerTicketUpdatedUtc = new DateTime(2026, 8, 28, 1, 2, 3, DateTimeKind.Utc);
                    value.Arena.Rating = 1_432;
                    value.Arena.AttackWins = 12;
                    value.Arena.DailyDateKey = "2026-09-04";
                    value.Arena.DailyChallenges = 4;
                    value.Arena.PurchasedTickets = 7;
                    value.Arena.ReplaySpeed = 2;
                    value.Arena.MatchmakingRevision = 4;
                    value.CompanionRanks["haejin"] = 12;
                    value.CompanionShards["haejin"] = 96;
                    value.ActiveHeroId = "yeonsu";
                    value.SelectedCompanionIds = ["haejin"];
                    value.AutoBoostInventory["auto-x5-30m"] = 2;
                    value.ActiveAutoSpeedMultiplier = 2;
                    value.AutoSpeedBoostEndsUtc = DateTime.UtcNow.AddMinutes(10);
                    value.DailyMissionDateKey = DailyMissionPolicy.TodayKey(DateTime.UtcNow);
                    value.TotalDefeated = 10;
                    value.DailyDefeatedBaseline = 3;
                    value.DailyEquipmentUpgradeCount = 2;
                    value.ClaimedDailyMissionKeys = ["advance-10"];
                    value.SpiritFarmLevel = 3;
                    value.SpiritFarmPlots =
                    [
                        new SpiritFarmPlotState
                        {
                            PlotIndex = 0,
                            CropKey = "moon-orchid",
                            PlantedUtc = DateTime.UtcNow.AddMinutes(-7),
                            ReadyUtc = DateTime.UtcNow.AddMinutes(-2),
                            OwnerFertilized = true
                        }
                    ];
                    value.SpiritFarmMeta.SeedPacks["advanced"] = 2;
                    value.SpiritFarmMeta.CropSeeds["dragon-root"] = 3;
                    value.SpiritFarmMeta.FertilizerCount = 7;
                    value.SpiritFarmMeta.VerticalRackLevel = 2;
                    value.SpiritFarmMeta.HouseTier = 2;
                    value.SpiritFarmMeta.Soils =
                    [
                        new SpiritFarmSoilState { PlotIndex = 0, Vitality = 64, LastCropKey = "moon-orchid", ConsecutiveCrops = 3 }
                    ];
                    return true;
                });
            }

            using var second = new GameProgressStore(options);
            var restored = await second.LoadOrCreateAsync("persistent-player");

            Assert.Equal(15, restored.Progress.HighestClearedStage);
            Assert.Equal("서버 검객", restored.Progress.GameNickname);
            Assert.Equal(2, restored.Progress.NicknameChangeCount);
            Assert.True(restored.Progress.AutoAttackUnlocked);
            Assert.True(restored.Progress.AutoAttackEnabled);
            Assert.Equal(4, restored.Progress.SwordArtLevel);
            Assert.Equal(3, restored.Progress.EquipmentLevel);
            Assert.Equal(3, restored.Progress.WeaponEquipmentLevel);
            Assert.Equal(2, restored.Progress.ArmorEquipmentLevel);
            Assert.Equal(1, restored.Progress.JadeEquipmentLevel);
            Assert.Equal(42, restored.Progress.HighestTrialTowerFloor);
            Assert.Equal(3, restored.Progress.TrialTowerChallengeTickets);
            Assert.Equal(new DateTime(2026, 8, 28, 1, 2, 3, DateTimeKind.Utc), restored.Progress.TrialTowerTicketUpdatedUtc);
            Assert.Equal(1_432, restored.Progress.Arena.Rating);
            Assert.Equal(12, restored.Progress.Arena.AttackWins);
            Assert.Equal("2026-09-04", restored.Progress.Arena.DailyDateKey);
            Assert.Equal(4, restored.Progress.Arena.DailyChallenges);
            Assert.Equal(7, restored.Progress.Arena.PurchasedTickets);
            Assert.Equal(2, restored.Progress.Arena.ReplaySpeed);
            Assert.Equal(4, restored.Progress.Arena.MatchmakingRevision);
            Assert.Equal(12, restored.Progress.CompanionRanks["haejin"]);
            Assert.Equal(96, restored.Progress.CompanionShards["haejin"]);
            Assert.Equal("yeonsu", restored.Progress.ActiveHeroId);
            Assert.Contains("haejin", restored.Progress.SelectedCompanionIds);
            Assert.Equal(2, restored.Progress.AutoBoostInventory["auto-x5-10m"]);
            Assert.Equal(2, restored.Progress.ActiveAutoSpeedMultiplier);
            Assert.NotNull(restored.Progress.AutoSpeedBoostEndsUtc);
            Assert.Equal(3, restored.Progress.DailyDefeatedBaseline);
            Assert.Equal(2, restored.Progress.DailyEquipmentUpgradeCount);
            Assert.Contains("advance-10", restored.Progress.ClaimedDailyMissionKeys);
            Assert.Equal(3, restored.Progress.SpiritFarmLevel);
            var restoredPlot = Assert.Single(restored.Progress.SpiritFarmPlots, plot => plot.PlotIndex == 0);
            Assert.Equal("moon-orchid", restoredPlot.CropKey);
            Assert.NotEqual(default, restoredPlot.PlantedUtc);
            Assert.NotEqual(default, restoredPlot.ReadyUtc);
            Assert.True(restoredPlot.ReadyUtc <= DateTime.UtcNow);
            Assert.True(restoredPlot.OwnerFertilized);
            Assert.Equal(2, restored.Progress.SpiritFarmMeta.SeedPacks["advanced"]);
            Assert.Equal(3, restored.Progress.SpiritFarmMeta.CropSeeds["dragon-root"]);
            Assert.Equal(7, restored.Progress.SpiritFarmMeta.FertilizerCount);
            Assert.Equal(2, restored.Progress.SpiritFarmMeta.VerticalRackLevel);
            Assert.Equal(2, restored.Progress.SpiritFarmMeta.HouseTier);
            Assert.Equal(64, restored.Progress.SpiritFarmMeta.Soils.Single(soil => soil.PlotIndex == 0).Vitality);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
