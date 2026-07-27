using System.Reflection;
using System.Text.Json;
using Wedding.Layouts.Contracts;
using WeddingPlatform.Blazor.Components;
using Xunit;

namespace WeddingPlatform.LayoutWorkflow.Tests;

public sealed class PageTurnContractTests
{
    [Fact]
    public void Paged_book_page_turn_configuration_round_trips_as_platform_neutral_json()
    {
        var package = CreatePagedBookPackage();

        var json = JsonSerializer.Serialize(
            package,
            LayoutPackageJson.CreateOptions(indented: true));
        var roundTripped = JsonSerializer.Deserialize<LayoutPackage>(
            json,
            LayoutPackageJson.CreateOptions());

        Assert.NotNull(roundTripped);
        Assert.True(LayoutPackageValidator.Validate(roundTripped).IsValid);

        var canonical = LayoutPackageCanonicalizer.Canonicalize(roundTripped);
        Assert.Equal(
            LayoutPresentationMode.PagedBook,
            canonical.Definition.Presentation);
        Assert.Equal(
            LayoutTransitionKind.PageTurn,
            canonical.Definition.Transition.Kind);
        Assert.Equal(720, canonical.Definition.Transition.DurationMilliseconds);
        Assert.True(canonical.Definition.Transition.EnableSwipe);
        Assert.True(canonical.Definition.Transition.EnableKeyboard);
        Assert.True(canonical.Definition.Transition.ShowNavigation);

        // Motion completion, input locking and reduced-motion behavior are
        // mandatory renderer policies, not executable package content.
        Assert.DoesNotContain("animationend", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(LayoutPresentationMode.Flow)]
    [InlineData(LayoutPresentationMode.FlipCard)]
    public void Page_turn_can_only_be_paired_with_paged_book(
        LayoutPresentationMode presentation)
    {
        var package = CreatePagedBookPackage() with
        {
            Definition = CreatePagedBookPackage().Definition with
            {
                Presentation = presentation,
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(
            result.Errors,
            error => error.Code == "transition.combination");
    }

    [Fact]
    public void Built_in_photo_book_consumes_the_shared_page_turn_definition()
    {
        var property = typeof(PhotoBookInvitationLayout).GetProperty(
            "PhotoBookTransition",
            BindingFlags.NonPublic | BindingFlags.Static);

        var transition = Assert.IsType<LayoutTransitionDefinition>(
            property?.GetValue(null));

        Assert.Equal(LayoutTransitionKind.PageTurn, transition.Kind);
        Assert.InRange(transition.DurationMilliseconds, 150, 2000);
        Assert.True(transition.EnableSwipe);
        Assert.True(transition.EnableKeyboard);
        Assert.True(transition.ShowNavigation);
    }

    private static LayoutPackage CreatePagedBookPackage() =>
        new()
        {
            Manifest = new LayoutManifest
            {
                SchemaVersion = LayoutSchema.CurrentVersion,
                Key = "page-turn-test",
                Version = "1.0.0",
                Label = "Page turn test",
                Description = "A platform-neutral paged book transition.",
                Tier = LayoutTier.Premium,
            },
            Definition = new LayoutDefinition
            {
                Presentation = LayoutPresentationMode.PagedBook,
                Transition = new LayoutTransitionDefinition
                {
                    Kind = LayoutTransitionKind.PageTurn,
                    DurationMilliseconds = 720,
                    EnableSwipe = true,
                    EnableKeyboard = true,
                    ShowNavigation = true,
                },
                SectionOrder =
                [
                    LayoutSectionKey.Hero,
                    LayoutSectionKey.Invitation,
                ],
                Root = new LayoutBlock
                {
                    Id = "page",
                    Kind = LayoutBlockKind.Page,
                    Binding = LayoutBindingKey.Invitation,
                    Children =
                    [
                        new LayoutBlock
                        {
                            Id = "hero",
                            Kind = LayoutBlockKind.Hero,
                            Binding = LayoutBindingKey.Invitation,
                        },
                        new LayoutBlock
                        {
                            Id = "invitation-section",
                            Kind = LayoutBlockKind.Section,
                            Binding = LayoutBindingKey.Invitation,
                        },
                    ],
                },
            },
        };
}
