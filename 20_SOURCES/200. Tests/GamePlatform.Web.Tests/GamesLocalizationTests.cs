using GamePlatform.Services;

namespace GamePlatform.Web.Tests;

public sealed class GamesLocalizationTests
{
    private static readonly string[] LocalizedLandingKeys =
    [
        "maru.tagline", "maru.description", "maru.intro", "maru.fact.idle",
        "maru.fact.party", "maru.fact.trials", "maru.fact.save", "maru.palace",
        "maru.caption", "maru.scroll", "maru.worldKicker", "maru.worldTitle",
        "maru.worldLead", "maru.mainKicker", "maru.main", "maru.mainDesc",
        "maru.play", "maru.growthKicker", "maru.growth", "maru.growthDesc",
        "maru.trialKicker", "maru.trials", "maru.trialsDesc", "maru.farmKicker",
        "maru.farm", "maru.farmDesc", "maru.ctaKicker", "maru.ready", "maru.enter"
    ];

    [Fact]
    public void MaruLanding_HasDedicatedCopyForEverySupportedLanguage()
    {
        var text = new GamesLocalization();
        text.SetLanguage("en");
        var english = LocalizedLandingKeys.ToDictionary(key => key, key => text[key]);

        foreach (var language in new[] { "es", "fr", "it", "pt", "ko", "ja", "zh-hans", "zh-hant", "vi" })
        {
            text.SetLanguage(language);

            foreach (var key in LocalizedLandingKeys)
            {
                Assert.False(string.IsNullOrWhiteSpace(text[key]));
                Assert.NotEqual(key, text[key]);
                Assert.NotEqual(english[key], text[key]);
            }
        }
    }
}
