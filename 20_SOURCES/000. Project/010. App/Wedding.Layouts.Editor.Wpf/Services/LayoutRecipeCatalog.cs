using Wedding.Layouts.Contracts;

namespace Wedding.Layouts.Editor.Wpf.Services;

public static class LayoutRecipeCatalog
{
    public static LayoutBlock CreateSection(LayoutSectionKey section) =>
        section switch
        {
            LayoutSectionKey.Hero => new LayoutBlock
            {
                Id = "hero",
                Kind = LayoutBlockKind.Hero,
                Binding = LayoutBindingKey.Invitation,
                Variant = LayoutVisualVariant.Hero,
                ImageSettings = new LayoutImageSettings
                {
                    AspectRatio = LayoutImageAspectRatio.Portrait,
                    Fit = LayoutImageFit.Cover,
                    CornerRadius = LayoutCornerRadius.None,
                    AltText = "신랑 신부 대표 사진",
                },
                ContainerSettings = new LayoutContainerSettings
                {
                    Alignment = LayoutAlignment.Center,
                    Gap = LayoutGap.Small,
                },
                Children =
                [
                    Bound(
                        "couple-name",
                        LayoutBlockKind.Heading,
                        LayoutBindingKey.CoupleName),
                    Bound(
                        "wedding-date",
                        LayoutBlockKind.Text,
                        LayoutBindingKey.WeddingDate),
                ],
            },
            LayoutSectionKey.Invitation => Section(
                "invitation-section",
                LayoutBindingKey.Invitation,
                Bound(
                    "invitation-title",
                    LayoutBlockKind.Heading,
                    LayoutBindingKey.None,
                    "초대합니다"),
                Bound(
                    "invitation-message",
                    LayoutBlockKind.Text,
                    LayoutBindingKey.Subtitle)),
            LayoutSectionKey.Calendar => Section(
                "calendar-section",
                LayoutBindingKey.None,
                Bound(
                    "calendar",
                    LayoutBlockKind.Calendar,
                    LayoutBindingKey.Calendar)),
            LayoutSectionKey.Gallery => Section(
                "gallery-section",
                LayoutBindingKey.None,
                Bound(
                    "gallery",
                    LayoutBlockKind.Gallery,
                    LayoutBindingKey.Gallery)),
            LayoutSectionKey.Story => Section(
                "story-section",
                LayoutBindingKey.None,
                Bound(
                    "story",
                    LayoutBlockKind.Text,
                    LayoutBindingKey.Story)),
            LayoutSectionKey.Video => Section(
                "video-section",
                LayoutBindingKey.None,
                Bound(
                    "videos",
                    LayoutBlockKind.VideoGallery,
                    LayoutBindingKey.Videos)),
            LayoutSectionKey.Location => Section(
                "location-section",
                LayoutBindingKey.None,
                Bound(
                    "map",
                    LayoutBlockKind.Map,
                    LayoutBindingKey.Map)),
            LayoutSectionKey.Accounts => Section(
                "accounts-section",
                LayoutBindingKey.None,
                Bound(
                    "accounts",
                    LayoutBlockKind.AccountList,
                    LayoutBindingKey.Accounts)),
            LayoutSectionKey.Guestbook => Section(
                "guestbook-section",
                LayoutBindingKey.None,
                Bound(
                    "guestbook",
                    LayoutBlockKind.Guestbook,
                    LayoutBindingKey.Guestbook)),
            LayoutSectionKey.Contact => Section(
                "contact-section",
                LayoutBindingKey.None,
                Bound(
                    "contacts",
                    LayoutBlockKind.ContactList,
                    LayoutBindingKey.Contacts)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(section),
                section,
                "지원하지 않는 섹션입니다."),
        };

    public static bool TryGetSectionKey(
        LayoutBlock block,
        out LayoutSectionKey section)
    {
        var sections = GetSectionKeys(block);
        if (sections.Count > 0)
        {
            section = sections[0];
            return true;
        }

        section = default;
        return false;
    }

    public static IReadOnlyList<LayoutSectionKey> GetSectionKeys(
        LayoutBlock block)
    {
        var result = new List<LayoutSectionKey>();
        var seen = new HashSet<LayoutSectionKey>();
        foreach (var item in Enumerate(block))
        {
            if (TryGetDirectSectionKey(item, out var section)
                && seen.Add(section))
            {
                result.Add(section);
            }
        }

        return result;
    }

    public static bool IsCompositeSection(LayoutBlock block) =>
        GetSectionKeys(block).Count > 1;

    public static bool IsStructuralKind(LayoutBlockKind kind) =>
        kind is LayoutBlockKind.Page
            or LayoutBlockKind.Section
            or LayoutBlockKind.Container
            or LayoutBlockKind.Stack
            or LayoutBlockKind.Grid
            or LayoutBlockKind.Card
            or LayoutBlockKind.Hero;

    private static LayoutBlock Bound(
        string id,
        LayoutBlockKind kind,
        LayoutBindingKey binding,
        string text = "") =>
        new()
        {
            Id = id,
            Kind = kind,
            Binding = binding,
            Text = text,
        };

    private static LayoutBlock Section(
        string id,
        LayoutBindingKey binding,
        params LayoutBlock[] children) =>
        new()
        {
            Id = id,
            Kind = LayoutBlockKind.Section,
            Binding = binding,
            ContainerSettings = new LayoutContainerSettings(),
            Children = children,
        };

    private static bool TryGetDirectSectionKey(
        LayoutBlock block,
        out LayoutSectionKey section)
    {
        switch (block.Kind)
        {
            case LayoutBlockKind.Hero:
                section = LayoutSectionKey.Hero;
                return true;
            case LayoutBlockKind.Calendar:
                section = LayoutSectionKey.Calendar;
                return true;
            case LayoutBlockKind.Countdown
                when block.Binding == LayoutBindingKey.WeddingDate:
                section = LayoutSectionKey.Calendar;
                return true;
            case LayoutBlockKind.Gallery:
                section = LayoutSectionKey.Gallery;
                return true;
            case LayoutBlockKind.VideoGallery:
                section = LayoutSectionKey.Video;
                return true;
            case LayoutBlockKind.Map:
                section = LayoutSectionKey.Location;
                return true;
            case LayoutBlockKind.AccountList:
                section = LayoutSectionKey.Accounts;
                return true;
            case LayoutBlockKind.Guestbook:
                section = LayoutSectionKey.Guestbook;
                return true;
            case LayoutBlockKind.ContactList:
                section = LayoutSectionKey.Contact;
                return true;
        }

        switch (block.Binding)
        {
            case LayoutBindingKey.Invitation:
                section = LayoutSectionKey.Invitation;
                return true;
            case LayoutBindingKey.Calendar:
                section = LayoutSectionKey.Calendar;
                return true;
            case LayoutBindingKey.Gallery:
                section = LayoutSectionKey.Gallery;
                return true;
            case LayoutBindingKey.Story:
            case LayoutBindingKey.Story2:
                section = LayoutSectionKey.Story;
                return true;
            case LayoutBindingKey.Videos:
                section = LayoutSectionKey.Video;
                return true;
            case LayoutBindingKey.Map:
                section = LayoutSectionKey.Location;
                return true;
            case LayoutBindingKey.Accounts:
                section = LayoutSectionKey.Accounts;
                return true;
            case LayoutBindingKey.Guestbook:
                section = LayoutSectionKey.Guestbook;
                return true;
            case LayoutBindingKey.Contacts:
                section = LayoutSectionKey.Contact;
                return true;
        }

        if (block.Id.Contains(
                "invitation",
                StringComparison.OrdinalIgnoreCase))
        {
            section = LayoutSectionKey.Invitation;
            return true;
        }

        section = default;
        return false;
    }

    private static IEnumerable<LayoutBlock> Enumerate(LayoutBlock block)
    {
        yield return block;
        foreach (var child in block.Children)
        {
            foreach (var descendant in Enumerate(child))
            {
                yield return descendant;
            }
        }
    }
}
