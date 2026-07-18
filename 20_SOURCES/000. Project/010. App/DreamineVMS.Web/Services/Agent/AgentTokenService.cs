using System.Collections.Concurrent;
using DreamineVMS.Web.Models;

namespace DreamineVMS.Web.Services.Agent;

/// <summary>
/// \if KO
/// <para>WPF 에이전트가 HLS를 푸시할 때 사용하는 단기 토큰을 관리합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates agent token service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AgentTokenService
{
    /// <summary>
    /// \if KO
    /// <para>tokens 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tokens value.</para>
    /// \endif
    /// </summary>
    private readonly ConcurrentDictionary<string, (VmsUser User, DateTimeOffset Expires)> _tokens = new();

    /// <summary>
    /// \if KO
    /// <para>Issue 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether issue.</para>
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
    /// <para>Issue 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the issue operation.</para>
    /// \endif
    /// </returns>
    public string Issue(VmsUser user)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = (user, DateTimeOffset.UtcNow.AddHours(24));
        return token;
    }

    /// <summary>
    /// \if KO
    /// <para>값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the value.</para>
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
    /// <para>Validate 작업에서 생성한 <c>VmsUser?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser?</c> result produced by the validate operation.</para>
    /// \endif
    /// </returns>
    public VmsUser? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (!_tokens.TryGetValue(token, out var entry)) return null;
        if (entry.Expires < DateTimeOffset.UtcNow) { _tokens.TryRemove(token, out _); return null; }
        return entry.User;
    }
}
