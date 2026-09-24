using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class RegionCatalogTests
{
    private readonly RegionCatalog _catalog = RegionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "regions.json"));

    [Fact]
    public void Stage10And11_SelectDifferentRegionsAndBossOnlyAt10()
    {
        var stage10 = _catalog.Resolve(10);
        var stage11 = _catalog.Resolve(11);

        Assert.Equal("silver-reed-marsh", stage10.Region.RegionId);
        Assert.True(stage10.IsBoss);
        Assert.Equal("boss", stage10.LightingKey);
        Assert.Equal("hollow-bell-sanctum", stage11.Region.RegionId);
        Assert.False(stage11.IsBoss);
        Assert.NotEqual("boss", stage11.LightingKey);
        Assert.NotEqual(stage10.EnemyAssetUrl, stage11.EnemyAssetUrl);
    }

    [Fact]
    public void StageAfter100_ExtendsFinalRegionWithTenStageBossCycle()
    {
        Assert.Equal("eclipsed-gold-palace", _catalog.Resolve(101).Region.RegionId);
        Assert.False(_catalog.Resolve(101).IsBoss);
        Assert.True(_catalog.Resolve(110).IsBoss);
    }

    [Fact]
    public void BlueWakeBoss_UsesDedicatedBackgroundAndMonsterAssets()
    {
        var normalStage = _catalog.Resolve(63);
        var bossStage = _catalog.Resolve(70);

        Assert.False(normalStage.IsBoss);
        Assert.True(bossStage.IsBoss);
        Assert.NotEqual(normalStage.BackgroundUrl, bossStage.BackgroundUrl);
        Assert.NotEqual(normalStage.EnemyAssetUrl, bossStage.EnemyAssetUrl);
        Assert.EndsWith("/bluewake-hollows-boss.webp", bossStage.BackgroundUrl);
        Assert.EndsWith("/boss-blue-wake.webp", bossStage.EnemyAssetUrl);
    }

    [Fact]
    public void GapOrOverlap_FailsCatalogValidation()
    {
        var first = Region("one", 1, 10);
        var gap = Region("two", 12, 20);
        var overlap = Region("three", 10, 20);

        Assert.Throws<InvalidOperationException>(() => new RegionCatalog([first, gap]));
        Assert.Throws<InvalidOperationException>(() => new RegionCatalog([first, overlap]));
    }

    [Fact]
    public void FallbackAndMobileWidthContracts_ArePresent()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "fallback.webp")));
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "app.css"));
        Assert.Contains("width: min(100vw, calc(100dvh * 9 / 16))", css);
        Assert.Contains("height: 100dvh", css);
        Assert.Contains("@media (max-width: 767px)", css);
        Assert.Contains("@container (max-width: 480px)", css);
        Assert.Contains("@container (max-width: 680px)", css);
        Assert.Contains(".mi-currencies small { display: none; }", css);
        Assert.DoesNotContain(".mi-engagement-line", css);
        Assert.Contains("min-height: 100dvh", css);
    }

    private static RegionDefinition Region(string id, int start, int end) => new()
    {
        RegionId = id, Name = id, StartStage = start, EndStage = end,
        BackgroundAssetKey = id, BgmAssetKey = "bgm", BossBgmAssetKey = "boss-bgm",
        AmbientSoundAssetKey = "ambient", AmbientEffectKey = "mist",
        EnemyPoolId = "pool", BossId = "boss", PrimaryColor = "#000", AccentColor = "#fff"
    };
}
