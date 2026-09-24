using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;

public sealed class GameSettingsTests
{
    [Fact]
    public void Muted_ForcesEveryChannelToZero()
    {
        Assert.Equal(0d, GameSettingsPolicy.OutputVolume(100, 100, muted: true));
        Assert.Equal(0d, GameSettingsPolicy.OutputVolume(80, 65, muted: true));
        Assert.Equal(0d, GameSettingsPolicy.OutputVolume(35, 90, muted: true));
    }

    [Fact]
    public void Master80AndBgm50_ProducesFortyPercentOutput()
    {
        Assert.Equal(.4d, GameSettingsPolicy.OutputVolume(80, 50, muted: false), precision: 6);
    }

    [Fact]
    public void RegionAndBoss_ResolveDifferentMusic()
    {
        var resolver = CreateResolver();
        var region = resolver.Resolve(63);
        var boss = resolver.Resolve(70);

        Assert.Equal("bgm-ironfang-battle", region.Track.AssetKey);
        Assert.False(region.IsBoss);
        Assert.Equal("bgm-steel-relic-boss", boss.Track.AssetKey);
        Assert.True(boss.IsBoss);
    }

    [Fact]
    public async Task SavedServerSettings_WinOverGuestDraftAndSurviveReload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"maru-settings-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new GameSettingsStore(new GameOptions { DatabasePath = path });
            var service = new GameSettingsService(store);
            var saved = new GameSettings();
            saved.Sound.MasterVolume = 37;
            saved.Display.Quality = GameQuality.Low;
            saved.General.ChannelKey = "baekya-2";
            saved.General.ChannelConfirmed = true;
            saved.General.ReadMailKeys = ["trial-arcade-open"];
            await service.SaveAsync("settings-user", saved);

            var guest = new GameSettings();
            guest.Sound.MasterVolume = 99;
            var restored = await service.LoadAsync("settings-user", guest);

            Assert.Equal(37, restored.Sound.MasterVolume);
            Assert.Equal(GameQuality.Low, restored.Display.Quality);
            Assert.Equal("baekya-2", restored.General.ChannelKey);
            Assert.True(restored.General.ChannelConfirmed);
            Assert.Contains("trial-arcade-open", restored.General.ReadMailKeys);
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { /* SQLite 공급자 풀은 테스트 프로세스 종료 시 정리됩니다. */ }
        }
    }

    [Fact]
    public void AudioMetadata_ContainsProjectOriginAndValidLoops()
    {
        var catalog = AudioTrackCatalog.Load(Path.Combine(AppContext.BaseDirectory, "audio-tracks.json"));
        var track = catalog.Get("bgm-ironfang-battle");

        Assert.Contains("Suno", track.ComposerOrSource);
        Assert.Contains("license", track.License, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".ogg", track.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(88.5, track.LoopEnd, precision: 3);
        Assert.Contains(".ogg", track.SourceUrl);
    }

    [Fact]
    public void CombatAudioMetadata_HasBoundedVoicesAndPlayableSources()
    {
        var catalog = AudioEffectCatalog.Load(Path.Combine(AppContext.BaseDirectory, "audio-effects.json"));
        var normal = catalog.Get("sword-impact-normal");
        var critical = catalog.Get("sword-impact-critical");

        Assert.EndsWith(".ogg", normal.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(normal.MaxVoices, 1, 4);
        Assert.True(normal.MinimumIntervalMilliseconds > 0);
        Assert.True(critical.VolumeNormalization > normal.VolumeNormalization);
        Assert.Contains("Steel Against Stone", normal.Source);
    }

    [Fact]
    public void CombatAudioDirector_SeparatesNormalCriticalAndEnemyFeedback()
    {
        var director = new CombatAudioDirector();

        Assert.Equal("sword-impact-normal", director.ResolvePlayerAttack(false, false).ImpactEffectKey);
        Assert.Equal("sword-impact-critical", director.ResolvePlayerAttack(true, false).ImpactEffectKey);
        Assert.Equal("enemy-defeat", director.ResolveEnemyAttack(true).ImpactEffectKey);
    }

    [Fact]
    public void DefaultMix_KeepsBattleEffectsAboveBackgroundMusic()
    {
        var sound = new GameSettings().Sound;

        Assert.Equal(80, sound.MasterVolume);
        Assert.Equal(40, sound.BgmVolume);
        Assert.Equal(80, sound.SfxVolume);
        Assert.Equal(55, sound.UiVolume);
    }

    [Fact]
    public void PowerSaving_TogglesStaySynchronizedAfterNormalization()
    {
        var fromGeneral = new GameSettings();
        fromGeneral.General.PowerSavingEnabled = true;
        var normalizedGeneral = GameSettingsPolicy.Normalize(fromGeneral);

        var fromDisplay = new GameSettings();
        fromDisplay.Display.PowerSaving = true;
        var normalizedDisplay = GameSettingsPolicy.Normalize(fromDisplay);

        Assert.True(normalizedGeneral.General.PowerSavingEnabled);
        Assert.True(normalizedGeneral.Display.PowerSaving);
        Assert.True(normalizedDisplay.General.PowerSavingEnabled);
        Assert.True(normalizedDisplay.Display.PowerSaving);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(30)]
    public void ScreenSaverTimeout_PreservesSupportedValues(int minutes)
    {
        var settings = new GameSettings();
        settings.General.ScreenSaverTimeoutMinutes = minutes;

        Assert.Equal(minutes, GameSettingsPolicy.Normalize(settings).General.ScreenSaverTimeoutMinutes);
    }

    [Fact]
    public void ScreenSaverTimeout_FallsBackToThreeMinutes()
    {
        var settings = new GameSettings();
        settings.General.ScreenSaverTimeoutMinutes = 999;

        Assert.Equal(3, GameSettingsPolicy.Normalize(settings).General.ScreenSaverTimeoutMinutes);
    }

    [Fact]
    public void AdminNoticeAndMailReadKeys_SurviveNormalization()
    {
        var noticeId = Guid.NewGuid().ToString("N");
        var mailId = Guid.NewGuid().ToString("N");
        var settings = new GameSettings();
        settings.General.ReadMailKeys = [$"admin-notice:{noticeId}", $"admin-mail:{mailId}", "admin-mail:not-a-message"];

        var normalized = GameSettingsPolicy.Normalize(settings);

        Assert.Contains($"admin-notice:{noticeId}", normalized.General.ReadMailKeys);
        Assert.Contains($"admin-mail:{mailId}", normalized.General.ReadMailKeys);
        Assert.DoesNotContain("admin-mail:not-a-message", normalized.General.ReadMailKeys);
    }

    private static RegionAudioResolver CreateResolver() => new(
        RegionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "regions.json")),
        AudioTrackCatalog.Load(Path.Combine(AppContext.BaseDirectory, "audio-tracks.json")));
}
