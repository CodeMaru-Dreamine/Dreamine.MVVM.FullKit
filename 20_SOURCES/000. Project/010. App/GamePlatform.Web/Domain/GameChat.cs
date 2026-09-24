namespace GamePlatform.Domain;

/// <summary>게임 월드 채팅의 한 메시지입니다.</summary>
public sealed record GameChatMessage(
    string MessageId,
    string ChannelKey,
    string UserId,
    string DisplayName,
    string Text,
    DateTime SentUtc);

/// <summary>월드 채팅 메시지 등록 결과입니다.</summary>
public sealed record GameChatPostResult(
    bool Accepted,
    GameChatMessage? Message,
    string? RejectionReason);
