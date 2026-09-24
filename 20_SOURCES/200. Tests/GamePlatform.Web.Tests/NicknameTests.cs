using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class NicknameTests
{
    [Fact]
    public async Task FirstChange_IsFree_AndSecondChangeUsesPremiumCurrency()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("nickname-owner", value => value.PremiumCurrency = 250);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("nickname-owner");

        var first = await session.ChangeNicknameAsync("달빛 검객");
        var second = await session.ChangeNicknameAsync("청운 검객");

        Assert.True(first.Accepted);
        Assert.Equal(0, first.ChargedPremiumCurrency);
        Assert.Equal(250, first.State.PremiumCurrency);
        Assert.Equal("달빛 검객", first.State.GameNickname);
        Assert.Equal(1, first.State.NicknameChangeCount);
        Assert.True(second.Accepted);
        Assert.Equal(GameNicknameRules.PaidChangeCost, second.ChargedPremiumCurrency);
        Assert.Equal(150, second.State.PremiumCurrency);
        Assert.Equal("청운 검객", second.State.GameNickname);
        Assert.Equal(2, second.State.NicknameChangeCount);
    }

    [Fact]
    public async Task PaidChange_WithInsufficientCurrency_KeepsNicknameAndFreeChangeHistory()
    {
        var store = new InMemoryProgressStore();
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("poor-owner");
        await session.ChangeNicknameAsync("첫 이름");

        var rejected = await session.ChangeNicknameAsync("두번째 이름");

        Assert.False(rejected.Accepted);
        Assert.Equal("첫 이름", rejected.State.GameNickname);
        Assert.Equal(1, rejected.State.NicknameChangeCount);
        Assert.Equal(0, rejected.State.PremiumCurrency);
    }

    [Fact]
    public async Task DuplicateNickname_IsRejectedAcrossAccounts()
    {
        var store = new InMemoryProgressStore();
        var first = SessionFactory.Create(store);
        var second = SessionFactory.Create(store);
        await first.InitializeAsync("first-owner");
        await second.InitializeAsync("second-owner");
        await first.ChangeNicknameAsync("천명검주");

        var duplicate = await second.ChangeNicknameAsync(" 천명검주 ");

        Assert.False(duplicate.Accepted);
        Assert.Contains("사용 중", duplicate.Message);
        Assert.Empty(duplicate.State.GameNickname);
        Assert.Equal(0, duplicate.State.NicknameChangeCount);
    }

    [Theory]
    [InlineData("운영자")]
    [InlineData("한")]
    [InlineData("검객!")]
    [InlineData("열두자를넘어가는아주긴닉네임")]
    public void InvalidNickname_IsRejected(string nickname)
    {
        Assert.False(GameNicknameRules.TryValidate(nickname, out _, out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
    }
}
