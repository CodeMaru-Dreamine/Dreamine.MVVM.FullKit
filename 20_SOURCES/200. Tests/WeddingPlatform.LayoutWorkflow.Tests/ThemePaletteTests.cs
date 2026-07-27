using WeddingPlatform.Models;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class ThemePaletteTests
{
    [Theory]
    [InlineData("#f7d6e0")]
    [InlineData("#2f6f4e")]
    [InlineData("#2457d6")]
    [InlineData("#ffffff")]
    [InlineData("#121212")]
    public void GeneratedPaletteMeetsThePromisedContrastTargets(string baseColor)
    {
        var palette = WeddingThemePaletteGenerator.Generate(baseColor);

        Assert.Equal(baseColor, palette.BaseColor, ignoreCase: true);
        Assert.All(
            new[]
            {
                palette.BaseColor,
                palette.Primary,
                palette.Dark,
                palette.Accent,
                palette.Text,
                palette.MutedText,
                palette.Background,
                palette.PanelBackground,
                palette.ButtonBackground,
                palette.ButtonText,
                palette.Border,
            },
            color => Assert.Matches("^#[0-9a-f]{6}$", color));

        Assert.True(
            WeddingThemePaletteGenerator.ContrastRatio(
                palette.Text,
                palette.Background) >= 7.0);
        Assert.True(
            WeddingThemePaletteGenerator.ContrastRatio(
                palette.MutedText,
                palette.Background) >= 4.5);
        Assert.True(
            WeddingThemePaletteGenerator.ContrastRatio(
                palette.ButtonText,
                palette.ButtonBackground) >= 4.5);
        Assert.True(
            WeddingThemePaletteGenerator.ContrastRatio(
                palette.Primary,
                palette.PanelBackground) >= 3.0);
        Assert.True(
            WeddingThemePaletteGenerator.ContrastRatio(
                palette.Border,
                palette.PanelBackground) >= 3.0);
    }

    [Fact]
    public void LegacyFiveColorThemeKeepsItsPreviousFallbackTokens()
    {
        var legacy = new CustomWeddingThemeSettings
        {
            Primary = "#6b8f71",
            Dark = "#2d4a32",
            Text = "#304d35",
            Background = "#d9e8dd",
            PanelBackground = "#eef6f0",
        };

        var palette = WeddingThemePaletteGenerator.ResolveForRendering(legacy);

        Assert.Equal(legacy.Primary, palette.BaseColor);
        Assert.Equal(legacy.Primary, palette.Accent);
        Assert.Equal(legacy.Text, palette.MutedText);
        Assert.Equal(legacy.Primary, palette.ButtonBackground);
        Assert.Equal("#ffffff", palette.ButtonText);
        Assert.Equal(legacy.Primary + "52", palette.Border);
    }

    [Fact]
    public void AutoGenerationPopulatesLegacyAndExtendedDtoFields()
    {
        var settings = new CustomWeddingThemeSettings();

        WeddingThemePaletteGenerator.ApplyGenerated(settings, "#d9688a");

        Assert.Equal("#d9688a", settings.BaseColor);
        Assert.Equal(
            WeddingThemePaletteGenerator.Generate("#d9688a").Primary,
            settings.Primary);
        Assert.False(string.IsNullOrWhiteSpace(settings.Accent));
        Assert.False(string.IsNullOrWhiteSpace(settings.MutedText));
        Assert.False(string.IsNullOrWhiteSpace(settings.ButtonBackground));
        Assert.False(string.IsNullOrWhiteSpace(settings.ButtonText));
        Assert.False(string.IsNullOrWhiteSpace(settings.Border));
    }
}
