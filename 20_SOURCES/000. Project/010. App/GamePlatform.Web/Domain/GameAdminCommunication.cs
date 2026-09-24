namespace GamePlatform.Domain;

public enum GameAdminMessageKind
{
    Announcement,
    Mail,
    Whisper
}

public enum GameAdminMessageTarget
{
    Individual,
    AllPlayers
}

public enum GameAdminMailAttachmentKind
{
    None,
    PremiumCurrency,
    Gold,
    HeroSummonTickets
}

/// <summary>운영자가 게임 이용자에게 발송한 공지·우편·귓속말입니다.</summary>
public sealed record GameAdminMessage(
    string MessageId,
    string Administrator,
    GameAdminMessageKind Kind,
    GameAdminMessageTarget Target,
    string? TargetUserId,
    string Title,
    string Text,
    GameAdminMailAttachmentKind AttachmentKind,
    long AttachmentAmount,
    DateTime CreatedUtc);
