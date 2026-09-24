using GamePlatform.Components;
using System.Reflection;
using Xunit;

public class TrialArcadePresentationTests
{
    [Fact]
    public void TerritoryEntranceDoesNotReuseFortressArtwork()
    {
        var hub = new TrialArcade();
        var selection = typeof(TrialArcade).GetField("selectedGame", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var image = typeof(TrialArcade).GetProperty("FeatureImage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        selection.SetValue(hub, "territory");
        var territory = (string)image.GetValue(hub)!;
        selection.SetValue(hub, "fortress");
        Assert.NotEqual(image.GetValue(hub), territory);
        Assert.Equal("/images/maru-idle/territory/city-v8.png", territory);
    }
}
