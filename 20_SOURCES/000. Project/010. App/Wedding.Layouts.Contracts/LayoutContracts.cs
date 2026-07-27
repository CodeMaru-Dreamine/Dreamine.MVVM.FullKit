using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wedding.Layouts.Contracts;

public static class LayoutSchema
{
    public const int CurrentVersion = 1;
}

public enum LayoutTier
{
    Free = 0,
    Premium = 1,
}

/// <summary>
/// Describes how the root page's direct children are presented. Flow preserves
/// the normal document layout; the paged modes are interpreted by a host
/// renderer without introducing executable package content.
/// </summary>
public enum LayoutPresentationMode
{
    Flow = 0,
    FlipCard = 1,
    PagedBook = 2,
}

/// <summary>
/// Selects the safe, built-in transition used between declarative pages.
/// </summary>
public enum LayoutTransitionKind
{
    None = 0,
    FlipCard = 1,
    PageTurn = 2,
}

public enum LayoutBlockKind
{
    Page = 0,
    Section = 1,
    Container = 2,
    Stack = 3,
    Grid = 4,
    Card = 5,
    Hero = 10,
    Heading = 11,
    Text = 12,
    Image = 13,
    Gallery = 14,
    Countdown = 15,
    Calendar = 16,
    Map = 17,
    AccountList = 18,
    ContactList = 19,
    Guestbook = 20,
    VideoGallery = 21,
    Navigation = 22,
    Button = 23,
    Divider = 24,
    Spacer = 25,
}

public enum LayoutBindingKey
{
    None = 0,
    Invitation = 1,
    CoupleName = 2,
    HeroTitle = 3,
    Subtitle = 4,
    WeddingDate = 5,
    WeddingTime = 6,
    VenueName = 7,
    VenueAddress = 8,
    Story = 9,
    Story2 = 10,
    HeroImage = 11,
    Gallery = 12,
    Accounts = 13,
    Contacts = 14,
    Guestbook = 15,
    Videos = 16,
    Map = 17,
    Calendar = 18,
}

public enum LayoutSectionKey
{
    Hero = 0,
    Invitation = 1,
    Calendar = 2,
    Gallery = 3,
    Story = 4,
    Video = 5,
    Location = 6,
    Accounts = 7,
    Guestbook = 8,
    Contact = 9,
}

public enum LayoutStyleToken
{
    PrimaryColor = 0,
    SecondaryColor = 1,
    AccentColor = 2,
    BackgroundColor = 3,
    SurfaceColor = 4,
    TextColor = 5,
    MutedTextColor = 6,
    BorderColor = 7,
    ButtonBackgroundColor = 8,
    ButtonTextColor = 9,
    NavigationBackgroundColor = 10,
    NavigationTextColor = 11,
}

public enum LayoutAlignment
{
    Start = 0,
    Center = 1,
    End = 2,
    Stretch = 3,
}

public enum LayoutJustification
{
    Start = 0,
    Center = 1,
    End = 2,
    SpaceBetween = 3,
    SpaceAround = 4,
}

public enum LayoutGap
{
    None = 0,
    ExtraSmall = 1,
    Small = 2,
    Medium = 3,
    Large = 4,
    ExtraLarge = 5,
}

public enum LayoutVisualVariant
{
    Default = 0,
    Muted = 1,
    Accent = 2,
    Outlined = 3,
    Elevated = 4,
    Hero = 5,
}

public enum LayoutTextSize
{
    Small = 0,
    Body = 1,
    Lead = 2,
    Title = 3,
    Display = 4,
}

public enum LayoutTextWeight
{
    Regular = 0,
    Medium = 1,
    Semibold = 2,
    Bold = 3,
}

public enum LayoutImageAspectRatio
{
    Auto = 0,
    Square = 1,
    Portrait = 2,
    Landscape = 3,
    Wide = 4,
}

public enum LayoutImageFit
{
    Contain = 0,
    Cover = 1,
}

public enum LayoutCornerRadius
{
    None = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    Round = 4,
}

public enum LayoutActionKind
{
    None = 0,
    ScrollToSection = 1,
    OpenMap = 2,
    OpenRsvp = 3,
    CopyAccount = 4,
    CallGroom = 5,
    CallBride = 6,
    ShareInvitation = 7,
}

public sealed record LayoutPackage
{
    public LayoutManifest Manifest { get; init; } = new();

    public LayoutDefinition Definition { get; init; } = new();
}

/// <summary>
/// Author-controlled package metadata. Approval and active-version state are
/// deliberately not part of this contract.
/// </summary>
public sealed record LayoutManifest
{
    public int SchemaVersion { get; init; } = LayoutSchema.CurrentVersion;

    public string Key { get; init; } = "";

    public string Version { get; init; } = "";

    public string Label { get; init; } = "";

    public string Description { get; init; } = "";

    /// <summary>
    /// Schema-v1 compatibility snapshot only. The authoring package does not
    /// grant or change product access. The server-owned LayoutKey definition
    /// policy is the sole authority for Free/Premium classification.
    /// </summary>
    public LayoutTier Tier { get; init; }
}

/// <summary>
/// Server-owned access classification for one stable LayoutKey. This policy is
/// shared by every immutable release of that key; package uploads and version
/// activation never grant authority to change it.
/// </summary>
public sealed record LayoutDefinitionPolicy
{
    public const int SupportedSchemaVersion = 1;

    public int SchemaVersion { get; init; } = SupportedSchemaVersion;

    public string LayoutKey { get; init; } = "";

    public LayoutTier Tier { get; init; }

    public string ClassifiedBy { get; init; } = "";

    public DateTimeOffset ClassifiedAtUtc { get; init; }

    public string Reason { get; init; } = "";

    public long Revision { get; init; } = 1;
}

/// <summary>
/// Minimal read-only projection that authoring tools may retrieve from the
/// server. Administrative classification details such as the actor and reason
/// deliberately remain server-private.
/// </summary>
public sealed record LayoutDefinitionPolicyStatus
{
    public string LayoutKey { get; init; } = "";

    public LayoutTier Tier { get; init; }

    public long Revision { get; init; }

    public bool IsBuiltIn { get; init; }
}

public sealed record LayoutDefinition
{
    public LayoutBlock Root { get; init; } = new();

    public LayoutPresentationMode Presentation { get; init; } =
        LayoutPresentationMode.Flow;

    public LayoutTransitionDefinition Transition { get; init; } = new();

    public IReadOnlyList<LayoutSectionKey> SectionOrder { get; init; } =
        Array.Empty<LayoutSectionKey>();

    public IReadOnlyList<LayoutStyleTokenValue> StyleTokens { get; init; } =
        Array.Empty<LayoutStyleTokenValue>();

    public LayoutResponsiveSettings Responsive { get; init; } = new();
}

/// <summary>
/// A constrained transition policy shared by the WPF authoring tool and Web
/// renderer. Only predefined motion primitives are allowed.
/// </summary>
public sealed record LayoutTransitionDefinition
{
    public LayoutTransitionKind Kind { get; init; } = LayoutTransitionKind.None;

    public int DurationMilliseconds { get; init; } = 650;

    public bool EnableSwipe { get; init; } = true;

    public bool EnableKeyboard { get; init; } = true;

    public bool ShowNavigation { get; init; } = true;
}

/// <summary>
/// A recursive, declarative block. It contains only typed settings and fixed
/// binding/action identifiers; executable markup, CSS, script and URLs have no field.
/// </summary>
public sealed record LayoutBlock
{
    public string Id { get; init; } = "";

    public LayoutBlockKind Kind { get; init; }

    public LayoutBindingKey Binding { get; init; }

    public string Text { get; init; } = "";

    public LayoutVisualVariant Variant { get; init; }

    public LayoutTextSettings? TextSettings { get; init; }

    public LayoutImageSettings? ImageSettings { get; init; }

    public LayoutContainerSettings? ContainerSettings { get; init; }

    public LayoutActionSettings? ActionSettings { get; init; }

    public LayoutResponsiveSettings Responsive { get; init; } = new();

    public IReadOnlyList<LayoutBlock> Children { get; init; } =
        Array.Empty<LayoutBlock>();
}

public sealed record LayoutStyleTokenValue
{
    public LayoutStyleToken Token { get; init; }

    public string Value { get; init; } = "";
}

public sealed record LayoutTextSettings
{
    public LayoutTextSize Size { get; init; } = LayoutTextSize.Body;

    public LayoutTextWeight Weight { get; init; } = LayoutTextWeight.Regular;

    public LayoutAlignment Alignment { get; init; } = LayoutAlignment.Start;

    public int MaxLines { get; init; }
}

public sealed record LayoutImageSettings
{
    public LayoutImageAspectRatio AspectRatio { get; init; }

    public LayoutImageFit Fit { get; init; } = LayoutImageFit.Cover;

    public LayoutCornerRadius CornerRadius { get; init; } = LayoutCornerRadius.Medium;

    public string AltText { get; init; } = "";
}

public sealed record LayoutContainerSettings
{
    public int Columns { get; init; } = 1;

    public LayoutGap Gap { get; init; } = LayoutGap.Medium;

    public LayoutAlignment Alignment { get; init; } = LayoutAlignment.Stretch;

    public LayoutJustification Justification { get; init; } = LayoutJustification.Start;

    public bool Wrap { get; init; }
}

public sealed record LayoutActionSettings
{
    public LayoutActionKind Action { get; init; }

    public LayoutSectionKey? TargetSection { get; init; }
}

public sealed record LayoutResponsiveSettings
{
    public LayoutBreakpointSettings Mobile { get; init; } = new();

    public LayoutBreakpointSettings Tablet { get; init; } = new();

    public LayoutBreakpointSettings Desktop { get; init; } = new();
}

public sealed record LayoutBreakpointSettings
{
    public bool Hidden { get; init; }

    public int Columns { get; init; } = 1;

    public int ColumnSpan { get; init; } = 1;

    public LayoutAlignment Alignment { get; init; } = LayoutAlignment.Stretch;

    public LayoutGap Gap { get; init; } = LayoutGap.Medium;
}

public static class LayoutPackageJson
{
    public static JsonSerializerOptions CreateOptions(bool indented = false) =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            WriteIndented = indented,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 32,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        };
}
