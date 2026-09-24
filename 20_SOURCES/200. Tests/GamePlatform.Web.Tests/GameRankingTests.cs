using Dreamine.Identity;
using Dreamine.Identity.Models;
using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class GameRankingTests
{
    [Fact]
    public async Task Ranking_UsesOnlyConfirmedAccountsInSelectedChannel()
    {
        var progress = new InMemoryProgressStore();
        var settings = new InMemorySettingsStore();
        await progress.SeedAsync("low", value => { value.AttackLevel = 2; value.GameNickname = "저녁노을"; });
        await progress.SeedAsync("high", value => { value.AttackLevel = 9; value.GameNickname = "천명검주"; });
        await progress.SeedAsync("other-channel", value => value.AttackLevel = 50);
        await progress.SeedAsync("not-confirmed", value => value.AttackLevel = 100);
        await settings.SaveAsync("low", Confirmed("cheongun-1"));
        await settings.SaveAsync("high", Confirmed("cheongun-1"));
        await settings.SaveAsync("other-channel", Confirmed("baekya-2"));
        await settings.SaveAsync("not-confirmed", new GameSettings());

        var ranking = await new GameRankingService(progress, settings, new StubUserStore()).GetAsync("cheongun-1");

        Assert.Collection(ranking,
            first => { Assert.Equal(1, first.Rank); Assert.Equal("high", first.UserId); Assert.Equal("천명검주", first.DisplayName); },
            second => { Assert.Equal(2, second.Rank); Assert.Equal("low", second.UserId); Assert.Equal("저녁노을", second.DisplayName); });
        Assert.True(ranking[0].AttackPower > ranking[1].AttackPower);
    }

    [Fact]
    public async Task Ranking_UsesIdentityDisplayNameWhenLegacyAccountHasNoGameNickname()
    {
        var progress = new InMemoryProgressStore();
        var settings = new InMemorySettingsStore();
        await progress.SeedAsync("4", value => value.AttackLevel = 4);
        await settings.SaveAsync("4", Confirmed("cheongun-1"));
        var users = new StubUserStore();
        users.Users[4] = new AuthUser { Id = 4, DisplayName = "달빛검객" };

        var ranking = await new GameRankingService(progress, settings, users).GetAsync("cheongun-1");

        Assert.Single(ranking);
        Assert.Equal("달빛검객", ranking[0].DisplayName);
    }

    private static GameSettings Confirmed(string channelKey)
    {
        var value = new GameSettings();
        value.General.ChannelKey = channelKey;
        value.General.ChannelConfirmed = true;
        return value;
    }

    private sealed class InMemorySettingsStore : IGameSettingsStore
    {
        private readonly Dictionary<string, GameSettings> values = new(StringComparer.Ordinal);

        public Task<GameSettings?> FindAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(values.TryGetValue(userId, out var value) ? GameSettingsPolicy.Normalize(value) : null);

        public Task SaveAsync(string userId, GameSettings settings, CancellationToken cancellationToken = default)
        {
            values[userId] = GameSettingsPolicy.Normalize(settings);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUserStore : IUserStore
    {
        public Dictionary<long, AuthUser> Users { get; } = [];

        public Task<AuthUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.GetValueOrDefault(id));

        public Task<AuthUser> UpsertAsync(string provider, string providerKey, string email, string displayName, string avatarUrl, RegistrationConsent? registrationConsent = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AuthUser?> FindByProviderAsync(string provider, string providerKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AuthUser?> UpdateDisplayNameAsync(long id, string displayName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AuthUser?> ChangeLocalPasswordAsync(long id, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AuthUser> CreateLocalAsync(string email, string displayName, string password, RegistrationConsent registrationConsent, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AuthUser?> ValidateLocalAsync(string email, string password, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
