namespace Codemaru.Models;

/// <summary>
/// \if KO
/// <para>\brief CardHybrid 세션의 현재 사용자입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card hybrid user functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>인증 자체는 상위 OAuth 계층 (Cookies + Google/Naver) 이 담당하고 CardHybrid 세션은 그 결과를 이 값으로 표현합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed record CardHybridUser(
    string Id,
    string Email,
    string DisplayName,
    DateTime SignedInAt)
{
    /// <summary>
    /// \if KO
    /// <para>\brief 로그인되지 않은 익명 사용자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the guest value.</para>
    /// \endif
    /// </summary>
    public static CardHybridUser Guest { get; } = new(
        Id: "guest",
        Email: string.Empty,
        DisplayName: "Guest",
        SignedInAt: DateTime.MinValue);

    /// <summary>
    /// \if KO
    /// <para>\brief 이 사용자가 익명(Guest) 인지 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is guest value.</para>
    /// \endif
    /// </summary>
    public bool IsGuest =>
        string.Equals(Id, Guest.Id, StringComparison.OrdinalIgnoreCase);
}
