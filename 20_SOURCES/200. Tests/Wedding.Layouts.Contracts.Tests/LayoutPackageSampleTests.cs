using System.Text.Json;
using Wedding.Layouts.Contracts;
using Xunit;

namespace Wedding.Layouts.Contracts.Tests;

public sealed class LayoutPackageSampleTests
{
    private const string LayoutKey = "atelier-letter";

    [Theory]
    [InlineData("atelier-letter-1.0.0.json", "1.0.0")]
    [InlineData("atelier-letter-1.0.1.json", "1.0.1")]
    public async Task Editor_sample_is_strictly_readable_and_valid(
        string fileName,
        string expectedVersion)
    {
        var options = LayoutPackageJson.CreateOptions(indented: true);
        var json = await File.ReadAllTextAsync(SamplePath(fileName));
        var package = JsonSerializer.Deserialize<LayoutPackage>(json, options);

        Assert.NotNull(package);
        Assert.Equal(LayoutKey, package.Manifest.Key);
        Assert.Equal(expectedVersion, package.Manifest.Version);
        Assert.Equal(LayoutPresentationMode.FlipCard, package.Definition.Presentation);
        Assert.Equal(LayoutTransitionKind.FlipCard, package.Definition.Transition.Kind);
        Assert.Equal(10, package.Definition.SectionOrder.Count);
        Assert.Equal(10, package.Definition.Root.Children.Count);

        var validation = LayoutPackageValidator.Validate(package);
        Assert.True(
            validation.IsValid,
            string.Join(
                Environment.NewLine,
                validation.Errors.Select(
                    error => $"{error.Path} [{error.Code}] {error.Message}")));

        var canonical = LayoutPackageCanonicalizer.Canonicalize(package);
        var roundTripJson = JsonSerializer.Serialize(canonical, options);
        var roundTrip = JsonSerializer.Deserialize<LayoutPackage>(
            roundTripJson,
            options);

        Assert.NotNull(roundTrip);
        Assert.True(LayoutPackageValidator.Validate(roundTrip).IsValid);
    }

    [Fact]
    public async Task Patch_release_keeps_identity_and_adds_material_capabilities()
    {
        var firstEdition = await ReadSampleAsync(
            "atelier-letter-1.0.0.json");
        var refinedEdition = await ReadSampleAsync(
            "atelier-letter-1.0.1.json");
        var firstBlocks = Flatten(firstEdition.Definition.Root).ToArray();
        var refinedBlocks = Flatten(refinedEdition.Definition.Root).ToArray();

        Assert.Equal(firstEdition.Manifest.Key, refinedEdition.Manifest.Key);
        Assert.Equal(firstEdition.Manifest.Label, refinedEdition.Manifest.Label);
        Assert.Equal(firstEdition.Manifest.Tier, refinedEdition.Manifest.Tier);
        Assert.Equal(
            firstEdition.Definition.SectionOrder,
            refinedEdition.Definition.SectionOrder);
        Assert.DoesNotContain(
            firstBlocks,
            block => block.Kind == LayoutBlockKind.Countdown);
        Assert.Contains(
            refinedBlocks,
            block => block.Kind == LayoutBlockKind.Countdown
                && block.Binding == LayoutBindingKey.WeddingDate);
        Assert.Contains(
            refinedBlocks,
            block => block.Kind == LayoutBlockKind.Button
                && block.ActionSettings?.Action
                    == LayoutActionKind.ShareInvitation);
        Assert.Contains(
            refinedBlocks,
            block => block.Id == "story-card-primary"
                && block.Kind == LayoutBlockKind.Card);
        Assert.Contains(
            refinedBlocks,
            block => block.Id == "story-card-secondary"
                && block.Kind == LayoutBlockKind.Card);
    }

    private static async Task<LayoutPackage> ReadSampleAsync(string fileName)
    {
        var json = await File.ReadAllTextAsync(SamplePath(fileName));
        return JsonSerializer.Deserialize<LayoutPackage>(
                json,
                LayoutPackageJson.CreateOptions())
            ?? throw new InvalidDataException(
                $"Sample package '{fileName}' could not be deserialized.");
    }

    private static string SamplePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Samples", fileName);

    private static IEnumerable<LayoutBlock> Flatten(LayoutBlock root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }
}
