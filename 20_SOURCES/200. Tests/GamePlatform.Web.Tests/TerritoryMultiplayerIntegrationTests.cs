using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;
public class TerritoryMultiplayerIntegrationTests
{
    [Fact]
    public async Task SeparateSessionsShareRallyButOtherServerCannotJoinAndPurchaseRetriesAreAtomic()
    {
        var path=Path.Combine(Path.GetTempPath(),$"territory-multiplayer-{Guid.NewGuid():N}.db");
        try
        {
            using var store=new GameProgressStore(new GameOptions{DatabasePath=path});
            using var settings=new GameSettingsStore(new GameOptions{DatabasePath=path});
            var time=new MutableTimeProvider(new DateTimeOffset(2026,9,6,0,0,0,TimeSpan.Zero));
            var sessions=new List<GameSession>();
            foreach(var id in new[]{"a","b","c"})
            {
                await settings.SaveAsync(id,new GameSettings{General=new(){ChannelConfirmed=true,ChannelKey=id=="c"?"baekya-1":"cheongun-1"}});
                var session=new GameSession(store,new FixedRollSource(50),SessionFactory.Regions,time,settingsStore:settings);
                await session.InitializeAsync(id);
                await store.MutateAsync(id,p=>{p.GameNickname=id;p.PremiumCurrency=1000;p.Territory.Buildings=[10,1,1,1,5,10,5];p.Territory.Troops=[100,0,0];p.Territory.Food=10000;p.Territory.Training=5;return true;});
                sessions.Add(session);await session.SharedWorldActionAsync();
            }
            var hero=(await sessions[0].LoadTerritoryCommandersAsync())[0].Id;
            var formation=new TerritoryFormation{Squads=[new(){TroopType=0,Count=100,CommanderId=hero}]};
            var opened=await sessions[0].SharedWorldActionAsync("rally-create",formation:formation);
            Assert.True(opened.Success);var rally=opened.World.Rallies.Single().Id;
            Assert.Single((await sessions[1].SharedWorldActionAsync()).World.Rallies);
            Assert.False((await sessions[2].SharedWorldActionAsync("rally-join",rally,formation:formation)).Success);
            var joined=await Task.WhenAll(sessions[1].SharedWorldActionAsync("rally-join",rally,formation:formation),sessions[1].SharedWorldActionAsync("rally-join",rally,formation:formation));
            Assert.Single(joined,r=>r.Success);
            var request=Guid.NewGuid().ToString("N");
            await Task.WhenAll(sessions[0].SharedWorldActionAsync("shield-buy",request),sessions[0].SharedWorldActionAsync("shield-buy",request));
            Assert.Equal(950,(await store.LoadOrCreateAsync("a")).Progress.PremiumCurrency);
            time.Advance(TimeSpan.FromSeconds(191));
            var returned=await sessions[1].SharedWorldActionAsync();
            Assert.True(returned.World.Rallies.Single().Returned);
            Assert.Null((await store.LoadOrCreateAsync("a")).Progress.Territory.March);
            Assert.Null((await store.LoadOrCreateAsync("b")).Progress.Territory.March);
            Assert.Equal(90,(await store.LoadOrCreateAsync("a")).Progress.Territory.Troops[0]);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path); }
    }
}
