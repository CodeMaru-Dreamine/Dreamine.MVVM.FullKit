using System.Reflection;
using GamePlatform.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

public class TerritoryMapPresentationTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object? Field(object component, string name) => component.GetType().GetField(name, Private)!.GetValue(component);
    private static void Set(object component, string name, object value) => component.GetType().GetField(name, Private)!.SetValue(component, value);
    private static void Call(object component, string name, params object[] args) => component.GetType().GetMethod(name, Private)!.Invoke(component, args);

    [Fact]
    public void PersonalSelectionOpensIntelligenceNotAnImmediateDispatch()
    {
        var component = new TerritoryWorld();
        Call(component, "SetTab", "map");
        Call(component, "SelectMapTile", 7);
        Assert.Equal(7, Field(component, "selectedTile"));
        Assert.Equal(true, Field(component, "mapDetailOpen"));
        Assert.Null(Field(component, "formationMission"));
        Call(component, "CloseMapDetail");
        Assert.Equal(false, Field(component, "mapDetailOpen"));
        Assert.Equal(7, Field(component, "selectedTile"));
    }

    [Fact]
    public void LeavingPersonalMapClearsTransientPanels()
    {
        var component = new TerritoryWorld();
        Call(component, "SelectMapTile", 8);
        Set(component, "personalMapExpanded", true);
        Set(component, "mapJobsOpen", true);
        Call(component, "SetTab", "storage");
        Assert.Equal("storage", Field(component, "tab"));
        foreach (var name in new[] { "mapDetailOpen", "personalMapExpanded", "mapJobsOpen" }) Assert.Equal(false, Field(component, name));
    }

    [Fact]
    public void SharedMonsterAndCastleSelectionRemainOnTheMap()
    {
        var component = new TerritoryCoopWorld();
        Call(component, "SelectMapMonster", 2);
        Assert.Equal("map", Field(component, "view"));
        Assert.Equal(true, Field(component, "mapMonsterOpen"));
        Assert.Null(Field(component, "intent"));
        Call(component, "SelectPlayer", "player-2");
        Assert.Equal("map", Field(component, "view"));
        Assert.Equal(false, Field(component, "mapMonsterOpen"));
        Assert.Equal(true, Field(component, "mapPlayerOpen"));
        Call(component, "SetView", "hunting");
        Assert.Equal(false, Field(component, "mapPlayerOpen"));
    }

    [Fact]
    public void EscapeDismissesDetailsBeforeLeavingExpandedMap()
    {
        var component = new TerritoryCoopWorld();
        Set(component, "mapExpanded", true);
        Call(component, "SelectMapMonster", 0);
        Call(component, "MapKeyDown", new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(false, Field(component, "mapMonsterOpen"));
        Assert.Equal(true, Field(component, "mapExpanded"));
        Call(component, "MapKeyDown", new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(false, Field(component, "mapExpanded"));
    }
}
