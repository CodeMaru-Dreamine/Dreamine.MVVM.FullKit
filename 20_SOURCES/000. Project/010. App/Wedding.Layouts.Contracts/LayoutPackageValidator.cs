using System.Text.RegularExpressions;

namespace Wedding.Layouts.Contracts;

public sealed record LayoutValidationError(string Path, string Code, string Message);

public sealed record LayoutValidationResult(IReadOnlyList<LayoutValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static LayoutValidationResult Success { get; } =
        new(Array.Empty<LayoutValidationError>());
}

public sealed record LayoutValidationLimits
{
    public int MaximumBlocks { get; init; } = 200;

    public int MaximumDepth { get; init; } = 12;

    public int MaximumChildrenPerBlock { get; init; } = 40;

    public int MaximumStyleTokens { get; init; } = 24;
}

public static class LayoutPackageValidator
{
    private static readonly Regex KeyPattern = new(
        "^[a-z0-9][a-z0-9-]{0,63}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex VersionPattern = new(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex BlockIdPattern = new(
        "^[a-z][a-z0-9-]{0,63}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex ColorPattern = new(
        "^#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly HashSet<LayoutBlockKind> StructuralKinds =
    [
        LayoutBlockKind.Page,
        LayoutBlockKind.Section,
        LayoutBlockKind.Container,
        LayoutBlockKind.Stack,
        LayoutBlockKind.Grid,
        LayoutBlockKind.Card,
        LayoutBlockKind.Hero,
    ];

    private static readonly HashSet<LayoutBlockKind> TextKinds =
    [
        LayoutBlockKind.Heading,
        LayoutBlockKind.Text,
        LayoutBlockKind.Button,
    ];

    private static readonly HashSet<LayoutBlockKind> ImageKinds =
    [
        LayoutBlockKind.Image,
        LayoutBlockKind.Hero,
        LayoutBlockKind.Gallery,
    ];

    private static readonly HashSet<LayoutBlockKind> ActionKinds =
    [
        LayoutBlockKind.Button,
        LayoutBlockKind.Navigation,
    ];

    private static readonly HashSet<LayoutBlockKind> SingletonKinds =
    [
        LayoutBlockKind.Hero,
        LayoutBlockKind.Gallery,
        LayoutBlockKind.Countdown,
        LayoutBlockKind.Calendar,
        LayoutBlockKind.Map,
        LayoutBlockKind.AccountList,
        LayoutBlockKind.ContactList,
        LayoutBlockKind.Guestbook,
        LayoutBlockKind.VideoGallery,
        LayoutBlockKind.Navigation,
    ];

    private static readonly HashSet<LayoutBlockKind> RequiredBindingKinds =
    [
        LayoutBlockKind.Image,
        LayoutBlockKind.Gallery,
        LayoutBlockKind.Countdown,
        LayoutBlockKind.Calendar,
        LayoutBlockKind.Map,
        LayoutBlockKind.AccountList,
        LayoutBlockKind.ContactList,
        LayoutBlockKind.Guestbook,
        LayoutBlockKind.VideoGallery,
    ];

    private static readonly IReadOnlyDictionary<LayoutBlockKind, HashSet<LayoutBindingKey>>
        AllowedBindings = new Dictionary<LayoutBlockKind, HashSet<LayoutBindingKey>>
        {
            [LayoutBlockKind.Page] = [LayoutBindingKey.None, LayoutBindingKey.Invitation],
            [LayoutBlockKind.Section] = [LayoutBindingKey.None, LayoutBindingKey.Invitation],
            [LayoutBlockKind.Container] = [LayoutBindingKey.None],
            [LayoutBlockKind.Stack] = [LayoutBindingKey.None],
            [LayoutBlockKind.Grid] = [LayoutBindingKey.None],
            [LayoutBlockKind.Card] = [LayoutBindingKey.None],
            [LayoutBlockKind.Hero] =
                [LayoutBindingKey.None, LayoutBindingKey.Invitation, LayoutBindingKey.HeroImage],
            [LayoutBlockKind.Heading] =
                [LayoutBindingKey.None, LayoutBindingKey.CoupleName, LayoutBindingKey.HeroTitle,
                    LayoutBindingKey.VenueName],
            [LayoutBlockKind.Text] =
                [LayoutBindingKey.None, LayoutBindingKey.Subtitle, LayoutBindingKey.WeddingDate,
                    LayoutBindingKey.WeddingTime, LayoutBindingKey.VenueAddress,
                    LayoutBindingKey.Story, LayoutBindingKey.Story2],
            [LayoutBlockKind.Image] = [LayoutBindingKey.HeroImage],
            [LayoutBlockKind.Gallery] = [LayoutBindingKey.Gallery],
            [LayoutBlockKind.Countdown] = [LayoutBindingKey.WeddingDate],
            [LayoutBlockKind.Calendar] = [LayoutBindingKey.Calendar, LayoutBindingKey.WeddingDate],
            [LayoutBlockKind.Map] = [LayoutBindingKey.Map, LayoutBindingKey.VenueAddress],
            [LayoutBlockKind.AccountList] = [LayoutBindingKey.Accounts],
            [LayoutBlockKind.ContactList] = [LayoutBindingKey.Contacts],
            [LayoutBlockKind.Guestbook] = [LayoutBindingKey.Guestbook],
            [LayoutBlockKind.VideoGallery] = [LayoutBindingKey.Videos],
            [LayoutBlockKind.Navigation] = [LayoutBindingKey.None],
            [LayoutBlockKind.Button] = [LayoutBindingKey.None],
            [LayoutBlockKind.Divider] = [LayoutBindingKey.None],
            [LayoutBlockKind.Spacer] = [LayoutBindingKey.None],
        };

    public static LayoutValidationResult Validate(
        LayoutPackage? package,
        LayoutValidationLimits? limits = null)
    {
        limits ??= new LayoutValidationLimits();
        var errors = new List<LayoutValidationError>();
        if (package is null)
        {
            errors.Add(new("$", "package.required", "A layout package is required."));
            return new(errors);
        }

        ValidateManifest(package.Manifest, errors);
        ValidateDefinition(package.Definition, limits, errors);
        return errors.Count == 0
            ? LayoutValidationResult.Success
            : new(errors.AsReadOnly());
    }

    private static void ValidateManifest(
        LayoutManifest? manifest,
        List<LayoutValidationError> errors)
    {
        if (manifest is null)
        {
            errors.Add(new("$.manifest", "manifest.required", "manifest is required."));
            return;
        }

        if (manifest.SchemaVersion != LayoutSchema.CurrentVersion)
        {
            errors.Add(new(
                "$.manifest.schemaVersion",
                "schema.unsupported",
                $"schemaVersion must be {LayoutSchema.CurrentVersion}."));
        }

        var key = manifest.Key?.Trim() ?? "";
        if (!KeyPattern.IsMatch(key)
            || !string.Equals(key, manifest.Key, StringComparison.Ordinal))
        {
            errors.Add(new(
                "$.manifest.key",
                "key.invalid",
                "key must be a canonical lowercase identifier."));
        }

        if (!VersionPattern.IsMatch(manifest.Version?.Trim() ?? "")
            || !string.Equals(manifest.Version?.Trim(), manifest.Version, StringComparison.Ordinal))
        {
            errors.Add(new(
                "$.manifest.version",
                "version.invalid",
                "version must be an exact semantic version."));
        }

        ValidatePlainText(
            "$.manifest.label",
            manifest.Label,
            1,
            80,
            errors);
        ValidatePlainText(
            "$.manifest.description",
            manifest.Description,
            1,
            500,
            errors);

        ValidateEnum(
            "$.manifest.tier",
            manifest.Tier,
            "tier.invalid",
            "tier is not supported.",
            errors);
    }

    private static void ValidateDefinition(
        LayoutDefinition? definition,
        LayoutValidationLimits limits,
        List<LayoutValidationError> errors)
    {
        if (definition is null)
        {
            errors.Add(new("$.definition", "definition.required", "definition is required."));
            return;
        }

        if (definition.Root is null)
        {
            errors.Add(new(
                "$.definition.root",
                "root.required",
                "A Page root block is required."));
            return;
        }

        if (definition.Root.Kind != LayoutBlockKind.Page)
        {
            errors.Add(new(
                "$.definition.root.kind",
                "root.kind",
                "The root block must have kind Page."));
        }

        var sectionOrder = definition.SectionOrder ?? Array.Empty<LayoutSectionKey>();
        if (sectionOrder.Count == 0)
        {
            errors.Add(new(
                "$.definition.sectionOrder",
                "sections.required",
                "sectionOrder must contain at least one section."));
        }
        else if (sectionOrder.Count > Enum.GetValues<LayoutSectionKey>().Length)
        {
            errors.Add(new(
                "$.definition.sectionOrder",
                "sections.limit",
                "sectionOrder contains too many entries."));
        }

        var duplicateSection = sectionOrder
            .GroupBy(x => x)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateSection is not null)
        {
            errors.Add(new(
                "$.definition.sectionOrder",
                "sections.duplicate",
                $"sectionOrder contains duplicate section '{duplicateSection.Key}'."));
        }

        if (!sectionOrder.Contains(LayoutSectionKey.Hero))
        {
            errors.Add(new(
                "$.definition.sectionOrder",
                "sections.hero",
                "sectionOrder must include Hero."));
        }

        ValidatePresentation(definition, errors);
        ValidateResponsive(
            "$.definition.responsive",
            definition.Responsive,
            errors);
        ValidateTokens(definition.StyleTokens, limits, errors);

        var state = new BlockValidationState(limits, errors);
        ValidateBlock(definition.Root, "$.definition.root", 1, state);
        foreach (var kind in SingletonKinds)
        {
            if (state.KindCounts.GetValueOrDefault(kind) > 1)
            {
                errors.Add(new(
                    "$.definition.root",
                    "blocks.singleton",
                    $"Block kind '{kind}' may appear at most once."));
            }
        }

        ValidateSectionAvailability(sectionOrder, state.Bindings, errors);
    }

    private static void ValidatePresentation(
        LayoutDefinition definition,
        List<LayoutValidationError> errors)
    {
        var presentationIsValid = ValidateEnum(
            "$.definition.presentation",
            definition.Presentation,
            "presentation.invalid",
            "presentation is not supported.",
            errors);

        var transition = definition.Transition;
        if (transition is null)
        {
            errors.Add(new(
                "$.definition.transition",
                "transition.required",
                "transition settings are required."));
            return;
        }

        var transitionIsValid = ValidateEnum(
            "$.definition.transition.kind",
            transition.Kind,
            "transition.kind.invalid",
            "transition kind is not supported.",
            errors);

        if (transition.DurationMilliseconds is < 150 or > 2000)
        {
            errors.Add(new(
                "$.definition.transition.durationMilliseconds",
                "transition.duration",
                "durationMilliseconds must be between 150 and 2000."));
        }

        if (!presentationIsValid || !transitionIsValid)
        {
            return;
        }

        var combinationIsValid = definition.Presentation switch
        {
            LayoutPresentationMode.Flow =>
                transition.Kind == LayoutTransitionKind.None,
            LayoutPresentationMode.FlipCard =>
                transition.Kind == LayoutTransitionKind.FlipCard,
            LayoutPresentationMode.PagedBook =>
                transition.Kind == LayoutTransitionKind.PageTurn,
            _ => false,
        };

        if (!combinationIsValid)
        {
            errors.Add(new(
                "$.definition.transition.kind",
                "transition.combination",
                $"transition '{transition.Kind}' is not valid for presentation '{definition.Presentation}'."));
        }
    }

    private static void ValidateTokens(
        IReadOnlyList<LayoutStyleTokenValue>? tokens,
        LayoutValidationLimits limits,
        List<LayoutValidationError> errors)
    {
        tokens ??= Array.Empty<LayoutStyleTokenValue>();
        if (tokens.Count > limits.MaximumStyleTokens)
        {
            errors.Add(new(
                "$.definition.styleTokens",
                "tokens.limit",
                $"No more than {limits.MaximumStyleTokens} style tokens are allowed."));
        }

        var seen = new HashSet<LayoutStyleToken>();
        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            var path = $"$.definition.styleTokens[{index}]";
            if (token is null)
            {
                errors.Add(new(path, "token.required", "A style token is required."));
                continue;
            }

            if (!Enum.IsDefined(token.Token))
            {
                errors.Add(new($"{path}.token", "token.name", "The style token is not allowed."));
            }
            else if (!seen.Add(token.Token))
            {
                errors.Add(new($"{path}.token", "token.duplicate", "The style token is duplicated."));
            }

            if (!ColorPattern.IsMatch(token.Value?.Trim() ?? ""))
            {
                errors.Add(new(
                    $"{path}.value",
                    "token.value",
                    "A style token value must be #RRGGBB or #RRGGBBAA."));
            }
        }
    }

    private static void ValidateBlock(
        LayoutBlock? block,
        string path,
        int depth,
        BlockValidationState state)
    {
        if (block is null)
        {
            state.Errors.Add(new(path, "block.required", "A block is required."));
            return;
        }

        state.BlockCount++;
        if (state.BlockCount > state.Limits.MaximumBlocks)
        {
            if (!state.BlockLimitReported)
            {
                state.Errors.Add(new(
                    path,
                    "blocks.limit",
                    $"A layout may contain at most {state.Limits.MaximumBlocks} blocks."));
                state.BlockLimitReported = true;
            }
            return;
        }

        if (depth > state.Limits.MaximumDepth)
        {
            state.Errors.Add(new(
                path,
                "blocks.depth",
                $"A layout block tree may be at most {state.Limits.MaximumDepth} levels deep."));
            return;
        }

        var kindIsDefined = Enum.IsDefined(block.Kind);
        if (!kindIsDefined)
        {
            state.Errors.Add(new($"{path}.kind", "block.kind", "The block kind is not supported."));
        }

        var bindingIsDefined = Enum.IsDefined(block.Binding);
        if (!bindingIsDefined)
        {
            state.Errors.Add(new(
                $"{path}.binding",
                "block.binding",
                "The block binding is not supported."));
        }

        ValidateEnum(
            $"{path}.variant",
            block.Variant,
            "block.variant",
            "The visual variant is not supported.",
            state.Errors);

        if (kindIsDefined && bindingIsDefined)
        {
            state.Bindings.Add((block.Kind, block.Binding));
        }

        if (kindIsDefined)
        {
            state.KindCounts[block.Kind] =
                state.KindCounts.GetValueOrDefault(block.Kind) + 1;
        }

        if (!BlockIdPattern.IsMatch(block.Id?.Trim() ?? "")
            || !string.Equals(block.Id?.Trim(), block.Id, StringComparison.Ordinal))
        {
            state.Errors.Add(new(
                $"{path}.id",
                "block.id",
                "A block id must be a canonical lowercase identifier."));
        }
        else if (!state.BlockIds.Add(block.Id!))
        {
            state.Errors.Add(new($"{path}.id", "block.id.duplicate", "Block ids must be unique."));
        }

        if (kindIsDefined
            && bindingIsDefined
            && (!AllowedBindings.TryGetValue(block.Kind, out var bindings)
                || !bindings.Contains(block.Binding)))
        {
            state.Errors.Add(new(
                $"{path}.binding",
                "block.binding",
                $"Binding '{block.Binding}' is not supported by block kind '{block.Kind}'."));
        }
        else if (kindIsDefined
                 && bindingIsDefined
                 && RequiredBindingKinds.Contains(block.Kind)
                 && block.Binding == LayoutBindingKey.None)
        {
            state.Errors.Add(new(
                $"{path}.binding",
                "block.binding.required",
                $"Block kind '{block.Kind}' requires a fixed data binding."));
        }

        ValidatePlainText($"{path}.text", block.Text, 0, 200, state.Errors);
        ValidateTypedSettings(block, path, state.Errors);
        ValidateResponsive($"{path}.responsive", block.Responsive, state.Errors);

        var children = block.Children ?? Array.Empty<LayoutBlock>();
        if (children.Count > state.Limits.MaximumChildrenPerBlock)
        {
            state.Errors.Add(new(
                $"{path}.children",
                "children.limit",
                $"A block may have at most {state.Limits.MaximumChildrenPerBlock} children."));
        }

        if (kindIsDefined
            && !StructuralKinds.Contains(block.Kind)
            && children.Count > 0)
        {
            state.Errors.Add(new(
                $"{path}.children",
                "children.notAllowed",
                $"Block kind '{block.Kind}' cannot contain child blocks."));
        }

        for (var index = 0;
             index < Math.Min(children.Count, state.Limits.MaximumChildrenPerBlock);
             index++)
        {
            ValidateBlock(children[index], $"{path}.children[{index}]", depth + 1, state);
        }
    }

    private static void ValidateTypedSettings(
        LayoutBlock block,
        string path,
        List<LayoutValidationError> errors)
    {
        if (block.TextSettings is not null)
        {
            ValidateEnum(
                $"{path}.textSettings.size",
                block.TextSettings.Size,
                "settings.text.size",
                "The text size is not supported.",
                errors);
            ValidateEnum(
                $"{path}.textSettings.weight",
                block.TextSettings.Weight,
                "settings.text.weight",
                "The text weight is not supported.",
                errors);
            ValidateEnum(
                $"{path}.textSettings.alignment",
                block.TextSettings.Alignment,
                "settings.text.alignment",
                "The text alignment is not supported.",
                errors);
        }

        if (block.TextSettings is not null && !TextKinds.Contains(block.Kind))
        {
            errors.Add(new(
                $"{path}.textSettings",
                "settings.text",
                $"Text settings are not valid for block kind '{block.Kind}'."));
        }
        else if (block.TextSettings is { MaxLines: < 0 or > 20 })
        {
            errors.Add(new(
                $"{path}.textSettings.maxLines",
                "settings.maxLines",
                "maxLines must be between 0 and 20."));
        }

        if (block.ImageSettings is not null)
        {
            ValidateEnum(
                $"{path}.imageSettings.aspectRatio",
                block.ImageSettings.AspectRatio,
                "settings.image.aspectRatio",
                "The image aspect ratio is not supported.",
                errors);
            ValidateEnum(
                $"{path}.imageSettings.fit",
                block.ImageSettings.Fit,
                "settings.image.fit",
                "The image fit is not supported.",
                errors);
            ValidateEnum(
                $"{path}.imageSettings.cornerRadius",
                block.ImageSettings.CornerRadius,
                "settings.image.cornerRadius",
                "The image corner radius is not supported.",
                errors);
        }

        if (block.ImageSettings is not null && !ImageKinds.Contains(block.Kind))
        {
            errors.Add(new(
                $"{path}.imageSettings",
                "settings.image",
                $"Image settings are not valid for block kind '{block.Kind}'."));
        }
        else if (block.ImageSettings is not null)
        {
            ValidatePlainText(
                $"{path}.imageSettings.altText",
                block.ImageSettings.AltText,
                0,
                160,
                errors);
        }

        if (block.ContainerSettings is not null)
        {
            ValidateEnum(
                $"{path}.containerSettings.gap",
                block.ContainerSettings.Gap,
                "settings.container.gap",
                "The container gap is not supported.",
                errors);
            ValidateEnum(
                $"{path}.containerSettings.alignment",
                block.ContainerSettings.Alignment,
                "settings.container.alignment",
                "The container alignment is not supported.",
                errors);
            ValidateEnum(
                $"{path}.containerSettings.justification",
                block.ContainerSettings.Justification,
                "settings.container.justification",
                "The container justification is not supported.",
                errors);
        }

        if (block.ContainerSettings is not null && !StructuralKinds.Contains(block.Kind))
        {
            errors.Add(new(
                $"{path}.containerSettings",
                "settings.container",
                $"Container settings are not valid for block kind '{block.Kind}'."));
        }
        else if (block.ContainerSettings is { Columns: < 1 or > 12 })
        {
            errors.Add(new(
                $"{path}.containerSettings.columns",
                "settings.columns",
                "columns must be between 1 and 12."));
        }

        var actionIsDefined = true;
        if (block.ActionSettings is not null)
        {
            actionIsDefined = ValidateEnum(
                $"{path}.actionSettings.action",
                block.ActionSettings.Action,
                "settings.action.kind",
                "The action is not supported.",
                errors);
            if (block.ActionSettings.TargetSection is { } targetSection)
            {
                ValidateEnum(
                    $"{path}.actionSettings.targetSection",
                    targetSection,
                    "settings.action.target",
                    "The target section is not supported.",
                    errors);
            }
        }

        if (block.ActionSettings is not null && !ActionKinds.Contains(block.Kind))
        {
            errors.Add(new(
                $"{path}.actionSettings",
                "settings.action",
                $"Action settings are not valid for block kind '{block.Kind}'."));
        }
        else if (block.ActionSettings is not null)
        {
            if (actionIsDefined
                && block.ActionSettings.Action == LayoutActionKind.ScrollToSection
                && block.ActionSettings.TargetSection is null)
            {
                errors.Add(new(
                    $"{path}.actionSettings.targetSection",
                    "settings.action.target",
                    "ScrollToSection requires a targetSection."));
            }

            if (actionIsDefined
                && block.ActionSettings.Action != LayoutActionKind.ScrollToSection
                && block.ActionSettings.TargetSection is not null)
            {
                errors.Add(new(
                    $"{path}.actionSettings.targetSection",
                    "settings.action.target",
                    "targetSection is only valid for ScrollToSection."));
            }
        }
    }

    private static void ValidateResponsive(
        string path,
        LayoutResponsiveSettings? responsive,
        List<LayoutValidationError> errors)
    {
        if (responsive is null)
        {
            errors.Add(new(path, "responsive.required", "Responsive settings are required."));
            return;
        }

        ValidateBreakpoint($"{path}.mobile", responsive.Mobile, errors);
        ValidateBreakpoint($"{path}.tablet", responsive.Tablet, errors);
        ValidateBreakpoint($"{path}.desktop", responsive.Desktop, errors);
    }

    private static void ValidateBreakpoint(
        string path,
        LayoutBreakpointSettings? breakpoint,
        List<LayoutValidationError> errors)
    {
        if (breakpoint is null)
        {
            errors.Add(new(path, "breakpoint.required", "Breakpoint settings are required."));
            return;
        }

        if (breakpoint.Columns is < 1 or > 12)
        {
            errors.Add(new(
                $"{path}.columns",
                "breakpoint.columns",
                "columns must be between 1 and 12."));
        }

        if (breakpoint.ColumnSpan is < 1 or > 12)
        {
            errors.Add(new(
                $"{path}.columnSpan",
                "breakpoint.columnSpan",
                "columnSpan must be between 1 and 12."));
        }

        ValidateEnum(
            $"{path}.alignment",
            breakpoint.Alignment,
            "breakpoint.alignment",
            "The breakpoint alignment is not supported.",
            errors);
        ValidateEnum(
            $"{path}.gap",
            breakpoint.Gap,
            "breakpoint.gap",
            "The breakpoint gap is not supported.",
            errors);
    }

    private static void ValidateSectionAvailability(
        IReadOnlyList<LayoutSectionKey> sections,
        IReadOnlySet<(LayoutBlockKind Kind, LayoutBindingKey Binding)> bindings,
        List<LayoutValidationError> errors)
    {
        static bool Has(
            IReadOnlySet<(LayoutBlockKind Kind, LayoutBindingKey Binding)> pairs,
            LayoutBlockKind kind,
            params LayoutBindingKey[] acceptedBindings) =>
            pairs.Any(pair =>
                pair.Kind == kind && acceptedBindings.Contains(pair.Binding));

        var available = new Dictionary<LayoutSectionKey, bool>
        {
            [LayoutSectionKey.Hero] = bindings.Any(pair =>
                pair.Kind == LayoutBlockKind.Hero),
            [LayoutSectionKey.Invitation] = Has(
                bindings,
                LayoutBlockKind.Section,
                LayoutBindingKey.Invitation),
            [LayoutSectionKey.Calendar] =
                Has(
                    bindings,
                    LayoutBlockKind.Calendar,
                    LayoutBindingKey.Calendar,
                    LayoutBindingKey.WeddingDate)
                || Has(
                    bindings,
                    LayoutBlockKind.Countdown,
                    LayoutBindingKey.WeddingDate),
            [LayoutSectionKey.Gallery] = Has(
                bindings,
                LayoutBlockKind.Gallery,
                LayoutBindingKey.Gallery),
            [LayoutSectionKey.Story] = Has(
                bindings,
                LayoutBlockKind.Text,
                LayoutBindingKey.Story,
                LayoutBindingKey.Story2),
            [LayoutSectionKey.Video] = Has(
                bindings,
                LayoutBlockKind.VideoGallery,
                LayoutBindingKey.Videos),
            [LayoutSectionKey.Location] = Has(
                bindings,
                LayoutBlockKind.Map,
                LayoutBindingKey.Map,
                LayoutBindingKey.VenueAddress),
            [LayoutSectionKey.Accounts] = Has(
                bindings,
                LayoutBlockKind.AccountList,
                LayoutBindingKey.Accounts),
            [LayoutSectionKey.Guestbook] = Has(
                bindings,
                LayoutBlockKind.Guestbook,
                LayoutBindingKey.Guestbook),
            [LayoutSectionKey.Contact] = Has(
                bindings,
                LayoutBlockKind.ContactList,
                LayoutBindingKey.Contacts),
        };

        foreach (var section in sections)
        {
            if (!Enum.IsDefined(section))
            {
                errors.Add(new(
                    "$.definition.sectionOrder",
                    "sections.unknown",
                    $"Section '{section}' is not supported."));
            }
            else if (!available[section])
            {
                errors.Add(new(
                    "$.definition.sectionOrder",
                    "sections.content",
                    $"Section '{section}' has no matching content block."));
            }
        }
    }

    private static void ValidatePlainText(
        string path,
        string? value,
        int minimumLength,
        int maximumLength,
        List<LayoutValidationError> errors)
    {
        var text = value ?? "";
        if (text.Length < minimumLength || text.Length > maximumLength)
        {
            errors.Add(new(
                path,
                "text.length",
                $"Text must be between {minimumLength} and {maximumLength} characters."));
            return;
        }

        if (text.Any(char.IsControl)
            || text.Contains('<', StringComparison.Ordinal)
            || text.Contains('>', StringComparison.Ordinal)
            || text.Contains("://", StringComparison.OrdinalIgnoreCase)
            || text.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("url(", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new(
                path,
                "text.unsafe",
                "HTML, CSS, script and URL-like text is not allowed."));
        }
    }

    private static bool ValidateEnum<TEnum>(
        string path,
        TEnum value,
        string code,
        string message,
        List<LayoutValidationError> errors)
        where TEnum : struct, Enum
    {
        if (Enum.IsDefined(value))
        {
            return true;
        }

        errors.Add(new(path, code, message));
        return false;
    }

    private sealed class BlockValidationState(
        LayoutValidationLimits limits,
        List<LayoutValidationError> errors)
    {
        public LayoutValidationLimits Limits { get; } = limits;

        public List<LayoutValidationError> Errors { get; } = errors;

        public HashSet<string> BlockIds { get; } = new(StringComparer.Ordinal);

        public HashSet<(LayoutBlockKind Kind, LayoutBindingKey Binding)> Bindings { get; } = [];

        public Dictionary<LayoutBlockKind, int> KindCounts { get; } = [];

        public int BlockCount { get; set; }

        public bool BlockLimitReported { get; set; }
    }
}

/// <summary>
/// Produces a normalized snapshot backed by read-only collections. Call it only
/// after <see cref="LayoutPackageValidator.Validate"/> succeeds.
/// </summary>
public static class LayoutPackageCanonicalizer
{
    public static LayoutPackage Canonicalize(LayoutPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        return new LayoutPackage
        {
            Manifest = package.Manifest with
            {
                Key = package.Manifest.Key.Trim(),
                Version = package.Manifest.Version.Trim(),
                Label = package.Manifest.Label.Trim(),
                Description = package.Manifest.Description.Trim(),
            },
            Definition = package.Definition with
            {
                Root = CanonicalizeBlock(package.Definition.Root),
                SectionOrder = Array.AsReadOnly(
                    package.Definition.SectionOrder.ToArray()),
                StyleTokens = Array.AsReadOnly(
                    package.Definition.StyleTokens
                        .Select(token => token with
                        {
                            Value = token.Value.Trim().ToUpperInvariant(),
                        })
                        .ToArray()),
            },
        };
    }

    private static LayoutBlock CanonicalizeBlock(LayoutBlock block) =>
        block with
        {
            Children = Array.AsReadOnly(
                block.Children.Select(CanonicalizeBlock).ToArray()),
        };
}
