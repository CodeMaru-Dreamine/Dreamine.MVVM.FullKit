using Wedding.Common;
using WeddingPlatform.Models;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class LayoutVersionPolicyTests
{
    [Fact]
    public void New_design_settings_pin_the_selected_version_by_default()
    {
        var settings = new DesignSettings();

        Assert.False(settings.FollowActiveLayoutVersion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Applying_layout_always_pins_the_selected_release(
        bool previousFollowActive)
    {
        var config = new TenantConfig
        {
            InvitationStyle = WeddingLayoutKeys.OnePage,
            DesignSettings = new DesignSettings
            {
                LayoutKey = WeddingLayoutKeys.OnePage,
                LayoutVersion = WeddingLayoutVersion.Initial,
                FollowActiveLayoutVersion = previousFollowActive,
            },
        };
        var option = new WeddingLayoutOption(
            WeddingLayoutMode.Unknown,
            "업로드 레이아웃",
            "버전 고정 정책 테스트",
            WeddingLayoutTier.Free,
            true,
            "w-layout-package",
            true,
            ["hero", "info"])
        {
            CatalogKey = "sample-layout",
            Version = "1.0.2",
        };

        InvitationDesignCatalog.ApplyLayoutSelection(config, option);

        Assert.Equal("sample-layout", config.DesignSettings.LayoutKey);
        Assert.Equal("1.0.2", config.DesignSettings.LayoutVersion);
        Assert.False(config.DesignSettings.FollowActiveLayoutVersion);
        Assert.Equal(WeddingLayoutMode.Unknown, config.DesignSettings.LayoutMode);
        Assert.Equal(WeddingLayoutKeys.OnePage, config.InvitationStyle);
    }

    [Fact]
    public void Applying_historical_release_turns_active_following_into_exact_pin()
    {
        var config = new TenantConfig
        {
            DesignSettings = new DesignSettings
            {
                LayoutKey = "sample-layout",
                LayoutVersion = "1.0.2",
                FollowActiveLayoutVersion = true,
            },
        };
        var option = new WeddingLayoutOption(
            WeddingLayoutMode.Unknown,
            "업로드 레이아웃",
            "과거 버전 고정 테스트",
            WeddingLayoutTier.Free,
            true,
            "w-layout-package",
            true,
            ["hero"])
        {
            CatalogKey = "sample-layout",
            Version = "1.0.0",
        };

        InvitationDesignCatalog.ApplyLayoutSelection(config, option);

        Assert.Equal("1.0.0", config.DesignSettings.LayoutVersion);
        Assert.False(config.DesignSettings.FollowActiveLayoutVersion);
    }

    [Fact]
    public void Different_tenants_can_pin_different_releases_of_the_same_layout()
    {
        var firstTenant = new TenantConfig();
        var secondTenant = new TenantConfig();
        var version100 = CreateOption("1.0.0");
        var version101 = CreateOption("1.0.1");

        InvitationDesignCatalog.ApplyLayoutSelection(firstTenant, version100);
        InvitationDesignCatalog.ApplyLayoutSelection(secondTenant, version101);

        Assert.Equal("sample-layout", firstTenant.DesignSettings.LayoutKey);
        Assert.Equal("1.0.0", firstTenant.DesignSettings.LayoutVersion);
        Assert.False(firstTenant.DesignSettings.FollowActiveLayoutVersion);
        Assert.Equal("sample-layout", secondTenant.DesignSettings.LayoutKey);
        Assert.Equal("1.0.1", secondTenant.DesignSettings.LayoutVersion);
        Assert.False(secondTenant.DesignSettings.FollowActiveLayoutVersion);

        // Auto-follow remains an explicit per-tenant opt-in and does not alter
        // the other tenant's exact release selection.
        secondTenant.DesignSettings.FollowActiveLayoutVersion = true;
        Assert.False(firstTenant.DesignSettings.FollowActiveLayoutVersion);
        Assert.True(secondTenant.DesignSettings.FollowActiveLayoutVersion);
    }

    private static WeddingLayoutOption CreateOption(string version) =>
        new(
            WeddingLayoutMode.Unknown,
            "업로드 레이아웃",
            "테넌트별 버전 고정 테스트",
            WeddingLayoutTier.Free,
            true,
            "w-layout-package",
            true,
            ["hero"])
        {
            CatalogKey = "sample-layout",
            Version = version,
        };
}
