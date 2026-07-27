using System.Reflection;
using WeddingPlatform.Models;
using WeddingPlatform.ViewModels;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class AdminPreviewAccountTests
{
    [Fact]
    public void AccountPresentation_HidesLabelOnlyEditorRows()
    {
        Assert.False(AccountInfo.HasDisplayableContent(
            new AccountInfo { Label = "신랑", Name = "홍길동" }));
        Assert.True(AccountInfo.HasDisplayableContent(
            new AccountInfo { Label = "신랑", Account = "123-456" }));
        Assert.True(AccountInfo.HasDisplayableContent(
            new AccountInfo { Label = "신부", Phone = "010-0000-0000" }));
        Assert.True(AccountInfo.HasDisplayableContent(
            new AccountInfo { KakaoPayUrl = "https://qr.kakaopay.com/example" }));
    }

    [Fact]
    public void ApplyAdminPreviewAccounts_ReplacesPersistedSnapshotWithoutSharingReferences()
    {
        var viewModel = new WeddingInvitationViewModel(null!, null!, null!);
        var config = new TenantConfig
        {
            Slug = "preview-couple",
            Accounts =
            [
                new AccountInfo
                {
                    Label = "저장본",
                    Account = "old-account",
                },
            ],
        };
        SetConfig(viewModel, config);

        var draft = new AccountInfo
        {
            Label = "편집본",
            Account = "new-account",
        };
        viewModel.ApplyAdminPreviewAccounts(
        [
            draft,
            new AccountInfo { Label = "입력 중" },
        ]);
        draft.Account = "mutated-after-send";

        Assert.Equal(2, config.Accounts.Count);
        Assert.Equal("new-account", config.Accounts[0].Account);
        Assert.Single(viewModel.Accounts);
        Assert.Equal("편집본", viewModel.Accounts[0].Label);
    }

    [Fact]
    public void ApplyAdminPreviewAccounts_EnforcesAdminMaximum()
    {
        var viewModel = new WeddingInvitationViewModel(null!, null!, null!);
        var config = new TenantConfig { Slug = "preview-couple" };
        SetConfig(viewModel, config);
        var draft = Enumerable.Range(1, 12)
            .Select(index => new AccountInfo
            {
                Label = $"{index}번",
                Phone = $"010-0000-{index:0000}",
            })
            .ToArray();

        viewModel.ApplyAdminPreviewAccounts(draft);

        Assert.Equal(8, config.Accounts.Count);
        Assert.Equal(8, viewModel.Accounts.Count);
    }

    [Fact]
    public void ApplyAdminPreviewAccounts_LimitsTextAndRejectsUnsafePaymentUrl()
    {
        var viewModel = new WeddingInvitationViewModel(null!, null!, null!);
        var config = new TenantConfig { Slug = "preview-couple" };
        SetConfig(viewModel, config);

        viewModel.ApplyAdminPreviewAccounts(
        [
            new AccountInfo
            {
                Label = new string('L', 200),
                Account = "123",
                KakaoPayUrl = "javascript:alert(1)",
            },
        ]);

        Assert.Equal(80, config.Accounts[0].Label.Length);
        Assert.Empty(config.Accounts[0].KakaoPayUrl);
        Assert.Single(viewModel.Accounts);
        Assert.Empty(AccountInfo.NormalizePaymentUrl("data:text/html,bad"));
        Assert.Equal(
            "https://qr.kakaopay.com/example",
            AccountInfo.NormalizePaymentUrl(
                "https://qr.kakaopay.com/example"));
    }

    private static void SetConfig(
        WeddingInvitationViewModel viewModel,
        TenantConfig config)
    {
        var property = typeof(WeddingInvitationViewModel).GetProperty(
            nameof(WeddingInvitationViewModel.Config),
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property.SetValue(viewModel, config);
    }
}
