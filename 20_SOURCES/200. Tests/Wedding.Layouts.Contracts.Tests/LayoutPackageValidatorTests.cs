using System.Text.Json;
using System.Text.Json.Nodes;
using Wedding.Layouts.Contracts;
using Xunit;

namespace Wedding.Layouts.Contracts.Tests;

public sealed class LayoutPackageValidatorTests
{
    [Fact]
    public void Valid_declarative_package_is_accepted()
    {
        var result = LayoutPackageValidator.Validate(CreateValidPackage());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Duplicate_singleton_blocks_are_rejected()
    {
        var package = CreateValidPackage();
        package = package with
        {
            Definition = package.Definition with
            {
                Root = package.Definition.Root with
                {
                    Children =
                    [
                        .. package.Definition.Root.Children,
                        new LayoutBlock
                        {
                            Id = "second-hero",
                            Kind = LayoutBlockKind.Hero,
                            Binding = LayoutBindingKey.Invitation,
                        },
                    ],
                },
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(result.Errors, error => error.Code == "blocks.singleton");
    }

    [Fact]
    public void Section_order_requires_matching_bound_content()
    {
        var package = CreateValidPackage();
        package = package with
        {
            Definition = package.Definition with
            {
                SectionOrder =
                [
                    LayoutSectionKey.Hero,
                    LayoutSectionKey.Invitation,
                    LayoutSectionKey.Story,
                ],
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(
            result.Errors,
            error => error.Code == "sections.content"
                && error.Message.Contains("Story", StringComparison.Ordinal));
    }

    [Fact]
    public void Url_and_markup_like_text_are_rejected()
    {
        var package = CreateValidPackage();
        var invitationSection = package.Definition.Root.Children[1];
        package = package with
        {
            Definition = package.Definition with
            {
                Root = package.Definition.Root with
                {
                    Children =
                    [
                        package.Definition.Root.Children[0],
                        invitationSection with
                        {
                            Children =
                            [
                                invitationSection.Children[0] with
                                {
                                    Text = "<script src=\"https://example.test/x.js\">",
                                },
                            ],
                        },
                    ],
                },
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(result.Errors, error => error.Code == "text.unsafe");
    }

    [Fact]
    public void Excessive_block_depth_is_rejected()
    {
        var child = new LayoutBlock
        {
            Id = "deep-leaf",
            Kind = LayoutBlockKind.Divider,
            Binding = LayoutBindingKey.None,
        };
        for (var depth = 0; depth < 13; depth++)
        {
            child = new LayoutBlock
            {
                Id = $"deep-{depth}",
                Kind = LayoutBlockKind.Container,
                Binding = LayoutBindingKey.None,
                Children = [child],
            };
        }

        var package = CreateValidPackage();
        package = package with
        {
            Definition = package.Definition with
            {
                Root = package.Definition.Root with
                {
                    Children = [.. package.Definition.Root.Children, child],
                },
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(result.Errors, error => error.Code == "blocks.depth");
    }

    [Fact]
    public void Unknown_json_members_are_rejected()
    {
        const string json =
            """
            {
              "manifest": {
                "schemaVersion": 1,
                "key": "safe-layout",
                "version": "1.0.0",
                "label": "Safe",
                "description": "Safe package",
                "tier": "Free",
                "rawHtml": "<script></script>"
              },
              "definition": {}
            }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<LayoutPackage>(
                json,
                LayoutPackageJson.CreateOptions()));
    }

    [Fact]
    public void Integer_json_values_are_rejected_for_every_contract_enum()
    {
        var validJson = JsonSerializer.Serialize(
            CreateValidPackage(),
            LayoutPackageJson.CreateOptions());
        var cases = new (string Name, Action<JsonObject> Mutate)[]
        {
            ("tier", root => Manifest(root)["tier"] = 0),
            ("presentation", root => Definition(root)["presentation"] = 0),
            ("transition kind", root => Transition(root)["kind"] = 0),
            ("block kind", root => RootBlock(root)["kind"] = 0),
            ("binding", root => RootBlock(root)["binding"] = 1),
            ("section", root => SectionOrder(root)[0] = 0),
            ("style token", root => StyleToken(root)["token"] = 0),
            ("variant", root => RootBlock(root)["variant"] = 0),
            ("text size", root => TextSettings(root)["size"] = 1),
            ("text weight", root => TextSettings(root)["weight"] = 0),
            ("text alignment", root => TextSettings(root)["alignment"] = 0),
            ("image aspect", root => ImageSettings(root)["aspectRatio"] = 0),
            ("image fit", root => ImageSettings(root)["fit"] = 1),
            ("corner radius", root => ImageSettings(root)["cornerRadius"] = 2),
            ("container gap", root => ContainerSettings(root)["gap"] = 3),
            ("container alignment", root => ContainerSettings(root)["alignment"] = 3),
            ("container justification", root => ContainerSettings(root)["justification"] = 0),
            ("action", root => ActionSettings(root)["action"] = 1),
            ("action target", root => ActionSettings(root)["targetSection"] = 1),
            ("responsive alignment", root => Mobile(root)["alignment"] = 3),
            ("responsive gap", root => Mobile(root)["gap"] = 3),
        };

        foreach (var testCase in cases)
        {
            var root = JsonNode.Parse(validJson)!.AsObject();
            testCase.Mutate(root);

            var exception = Record.Exception(() =>
                JsonSerializer.Deserialize<LayoutPackage>(
                    root.ToJsonString(),
                    LayoutPackageJson.CreateOptions()));

            Assert.True(
                exception is JsonException,
                $"{testCase.Name} accepted an integer JSON value.");
        }

        static JsonObject Manifest(JsonObject root) =>
            root["manifest"]!.AsObject();

        static JsonObject Definition(JsonObject root) =>
            root["definition"]!.AsObject();

        static JsonObject RootBlock(JsonObject root) =>
            Definition(root)["root"]!.AsObject();

        static JsonObject Transition(JsonObject root) =>
            Definition(root)["transition"]!.AsObject();

        static JsonArray SectionOrder(JsonObject root) =>
            Definition(root)["sectionOrder"]!.AsArray();

        static JsonObject StyleToken(JsonObject root) =>
            Definition(root)["styleTokens"]![0]!.AsObject();

        static JsonObject TextBlock(JsonObject root) =>
            RootBlock(root)["children"]![1]!["children"]![0]!.AsObject();

        static JsonObject TextSettings(JsonObject root)
        {
            var settings = new JsonObject
            {
                ["size"] = "Body",
                ["weight"] = "Regular",
                ["alignment"] = "Start",
                ["maxLines"] = 0,
            };
            TextBlock(root)["textSettings"] = settings;
            return settings;
        }

        static JsonObject ImageSettings(JsonObject root)
        {
            var settings = new JsonObject
            {
                ["aspectRatio"] = "Auto",
                ["fit"] = "Cover",
                ["cornerRadius"] = "Medium",
                ["altText"] = "",
            };
            RootBlock(root)["imageSettings"] = settings;
            return settings;
        }

        static JsonObject ContainerSettings(JsonObject root)
        {
            var settings = new JsonObject
            {
                ["columns"] = 1,
                ["gap"] = "Medium",
                ["alignment"] = "Stretch",
                ["justification"] = "Start",
                ["wrap"] = false,
            };
            RootBlock(root)["containerSettings"] = settings;
            return settings;
        }

        static JsonObject ActionSettings(JsonObject root)
        {
            var settings = new JsonObject
            {
                ["action"] = "ScrollToSection",
                ["targetSection"] = "Invitation",
            };
            TextBlock(root)["actionSettings"] = settings;
            return settings;
        }

        static JsonObject Mobile(JsonObject root) =>
            Definition(root)["responsive"]!["mobile"]!.AsObject();
    }

    [Fact]
    public void Undefined_programmatic_enum_values_are_rejected_at_every_nested_level()
    {
        var package = CreateValidPackage();
        var root = package.Definition.Root;
        var hero = root.Children[0];
        var invitation = root.Children[1];
        var text = invitation.Children[0];
        package = package with
        {
            Manifest = package.Manifest with { Tier = (LayoutTier)999 },
            Definition = package.Definition with
            {
                Presentation = (LayoutPresentationMode)999,
                Transition = package.Definition.Transition with
                {
                    Kind = (LayoutTransitionKind)999,
                },
                SectionOrder = [LayoutSectionKey.Hero, (LayoutSectionKey)999],
                StyleTokens =
                [
                    new LayoutStyleTokenValue
                    {
                        Token = (LayoutStyleToken)999,
                        Value = "#AA7755",
                    },
                ],
                Responsive = package.Definition.Responsive with
                {
                    Mobile = package.Definition.Responsive.Mobile with
                    {
                        Alignment = (LayoutAlignment)999,
                        Gap = (LayoutGap)999,
                    },
                },
                Root = root with
                {
                    Variant = (LayoutVisualVariant)999,
                    ContainerSettings = new LayoutContainerSettings
                    {
                        Gap = (LayoutGap)999,
                        Alignment = (LayoutAlignment)999,
                        Justification = (LayoutJustification)999,
                    },
                    Children =
                    [
                        hero with
                        {
                            ImageSettings = new LayoutImageSettings
                            {
                                AspectRatio = (LayoutImageAspectRatio)999,
                                Fit = (LayoutImageFit)999,
                                CornerRadius = (LayoutCornerRadius)999,
                            },
                        },
                        invitation with
                        {
                            Children =
                            [
                                text with
                                {
                                    Binding = (LayoutBindingKey)999,
                                    TextSettings = new LayoutTextSettings
                                    {
                                        Size = (LayoutTextSize)999,
                                        Weight = (LayoutTextWeight)999,
                                        Alignment = (LayoutAlignment)999,
                                    },
                                    ActionSettings = new LayoutActionSettings
                                    {
                                        Action = (LayoutActionKind)999,
                                        TargetSection = (LayoutSectionKey)999,
                                    },
                                },
                            ],
                        },
                    ],
                },
            },
        };

        var paths = LayoutPackageValidator.Validate(package).Errors
            .Select(error => error.Path)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("$.manifest.tier", paths);
        Assert.Contains("$.definition.presentation", paths);
        Assert.Contains("$.definition.transition.kind", paths);
        Assert.Contains("$.definition.sectionOrder", paths);
        Assert.Contains("$.definition.styleTokens[0].token", paths);
        Assert.Contains("$.definition.responsive.mobile.alignment", paths);
        Assert.Contains("$.definition.responsive.mobile.gap", paths);
        Assert.Contains("$.definition.root.variant", paths);
        Assert.Contains("$.definition.root.containerSettings.gap", paths);
        Assert.Contains("$.definition.root.containerSettings.alignment", paths);
        Assert.Contains("$.definition.root.containerSettings.justification", paths);
        Assert.Contains("$.definition.root.children[0].imageSettings.aspectRatio", paths);
        Assert.Contains("$.definition.root.children[0].imageSettings.fit", paths);
        Assert.Contains("$.definition.root.children[0].imageSettings.cornerRadius", paths);
        Assert.Contains("$.definition.root.children[1].children[0].binding", paths);
        Assert.Contains("$.definition.root.children[1].children[0].textSettings.size", paths);
        Assert.Contains("$.definition.root.children[1].children[0].textSettings.weight", paths);
        Assert.Contains("$.definition.root.children[1].children[0].textSettings.alignment", paths);
        Assert.Contains("$.definition.root.children[1].children[0].actionSettings.action", paths);
        Assert.Contains("$.definition.root.children[1].children[0].actionSettings.targetSection", paths);
    }

    [Theory]
    [InlineData(LayoutPresentationMode.Flow, LayoutTransitionKind.FlipCard)]
    [InlineData(LayoutPresentationMode.FlipCard, LayoutTransitionKind.None)]
    [InlineData(LayoutPresentationMode.PagedBook, LayoutTransitionKind.FlipCard)]
    public void Incompatible_presentation_transition_pairs_are_rejected(
        LayoutPresentationMode presentation,
        LayoutTransitionKind transition)
    {
        var package = CreateValidPackage();
        package = package with
        {
            Definition = package.Definition with
            {
                Presentation = presentation,
                Transition = package.Definition.Transition with
                {
                    Kind = transition,
                },
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(result.Errors, error => error.Code == "transition.combination");
    }

    [Theory]
    [InlineData(149)]
    [InlineData(2001)]
    public void Unsafe_transition_duration_is_rejected(int durationMilliseconds)
    {
        var package = CreateValidPackage();
        package = package with
        {
            Definition = package.Definition with
            {
                Transition = package.Definition.Transition with
                {
                    DurationMilliseconds = durationMilliseconds,
                },
            },
        };

        var result = LayoutPackageValidator.Validate(package);

        Assert.Contains(result.Errors, error => error.Code == "transition.duration");
    }

    [Fact]
    public void Transition_settings_round_trip_and_survive_canonicalization()
    {
        var package = CreateValidPackage();
        package = package with
        {
            Definition = package.Definition with
            {
                Presentation = LayoutPresentationMode.FlipCard,
                Transition = new LayoutTransitionDefinition
                {
                    Kind = LayoutTransitionKind.FlipCard,
                    DurationMilliseconds = 720,
                    EnableSwipe = false,
                    EnableKeyboard = true,
                    ShowNavigation = true,
                },
            },
        };

        var json = JsonSerializer.Serialize(
            package,
            LayoutPackageJson.CreateOptions());
        var roundTripped = JsonSerializer.Deserialize<LayoutPackage>(
            json,
            LayoutPackageJson.CreateOptions());
        var canonical = LayoutPackageCanonicalizer.Canonicalize(roundTripped!);

        Assert.Equal(LayoutPresentationMode.FlipCard, canonical.Definition.Presentation);
        Assert.Equal(LayoutTransitionKind.FlipCard, canonical.Definition.Transition.Kind);
        Assert.Equal(720, canonical.Definition.Transition.DurationMilliseconds);
        Assert.False(canonical.Definition.Transition.EnableSwipe);
        Assert.True(LayoutPackageValidator.Validate(canonical).IsValid);
    }

    private static LayoutPackage CreateValidPackage() =>
        new()
        {
            Manifest = new LayoutManifest
            {
                SchemaVersion = LayoutSchema.CurrentVersion,
                Key = "safe-layout",
                Version = "1.0.0",
                Label = "Safe layout",
                Description = "A safe declarative layout.",
                Tier = LayoutTier.Free,
            },
            Definition = new LayoutDefinition
            {
                SectionOrder =
                [
                    LayoutSectionKey.Hero,
                    LayoutSectionKey.Invitation,
                ],
                StyleTokens =
                [
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.PrimaryColor,
                        Value = "#AA7755",
                    },
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
                            Children =
                            [
                                new LayoutBlock
                                {
                                    Id = "couple-name",
                                    Kind = LayoutBlockKind.Heading,
                                    Binding = LayoutBindingKey.CoupleName,
                                },
                            ],
                        },
                        new LayoutBlock
                        {
                            Id = "invitation-section",
                            Kind = LayoutBlockKind.Section,
                            Binding = LayoutBindingKey.Invitation,
                            Children =
                            [
                                new LayoutBlock
                                {
                                    Id = "invitation-copy",
                                    Kind = LayoutBlockKind.Text,
                                    Binding = LayoutBindingKey.Subtitle,
                                },
                            ],
                        },
                    ],
                },
            },
        };
}
