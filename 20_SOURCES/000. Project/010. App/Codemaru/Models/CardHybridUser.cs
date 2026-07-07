namespace Codemaru.Models;

/// <summary>
/// \brief CardHybrid 세션의 현재 사용자입니다.
/// </summary>
/// <remarks>
/// 인증 자체는 상위 OAuth 계층 (Cookies + Google/Naver) 이 담당하고
/// CardHybrid 세션은 그 결과를 이 값으로 표현합니다.
/// </remarks>
public sealed record CardHybridUser(
    string Id,
    string Email,
    string DisplayName,
    DateTime SignedInAt)
{
    /// <summary>\brief 로그인되지 않은 익명 사용자입니다.</summary>
    public static CardHybridUser Guest { get; } = new(
        Id: "guest",
        Email: string.Empty,
        DisplayName: "Guest",
        SignedInAt: DateTime.MinValue);

    /// <summary>\brief 이 사용자가 익명(Guest) 인지 여부입니다.</summary>
    public bool IsGuest =>
        string.Equals(Id, Guest.Id, StringComparison.OrdinalIgnoreCase);
}
