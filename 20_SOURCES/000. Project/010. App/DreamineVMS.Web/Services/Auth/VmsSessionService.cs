using System.Collections.Concurrent;
using DreamineVMS.Web.Models;

namespace DreamineVMS.Web.Services.Auth;

/// <summary>
/// \if KO
/// <para>\brief 로그인 세션 토큰을 관리하는 싱글톤 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms session service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsSessionService
{
    /// <summary>
    /// \if KO
    /// <para>sessions 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sessions value.</para>
    /// \endif
    /// </summary>
    private readonly ConcurrentDictionary<string, (VmsUser User, DateTimeOffset Expires)> _sessions = new();

    /// <summary>
    /// \if KO
    /// <para>Session 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the session value.</para>
    /// \endif
    /// </summary>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>VmsUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Session 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the create session operation.</para>
    /// \endif
    /// </returns>
    public string CreateSession(VmsUser user)
    {
        var token = Guid.NewGuid().ToString("N");
        _sessions[token] = (user, DateTimeOffset.UtcNow.AddDays(30));
        return token;
    }

    /// <summary>
    /// \if KO
    /// <para>Token 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the token value.</para>
    /// \endif
    /// </summary>
    /// <param name="token">
    /// \if KO
    /// <para>token에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Validate Token 작업에서 생성한 <c>VmsUser?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser?</c> result produced by the validate token operation.</para>
    /// \endif
    /// </returns>
    public VmsUser? ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (!_sessions.TryGetValue(token, out var entry)) return null;
        if (entry.Expires < DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }
        return entry.User;
    }

    /// <summary>
    /// \if KO
    /// <para>Session 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the session item.</para>
    /// \endif
    /// </summary>
    /// <param name="token">
    /// \if KO
    /// <para>token에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for token.</para>
    /// \endif
    /// </param>
    public void RemoveSession(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            _sessions.TryRemove(token, out _);
    }
}
