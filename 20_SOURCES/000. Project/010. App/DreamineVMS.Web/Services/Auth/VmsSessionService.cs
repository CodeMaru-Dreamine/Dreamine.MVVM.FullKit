using System.Collections.Concurrent;
using DreamineVMS.Web.Models;

namespace DreamineVMS.Web.Services.Auth;

/// <summary>
/// \brief 로그인 세션 토큰을 관리하는 싱글톤 서비스입니다.
/// </summary>
public sealed class VmsSessionService
{
    private readonly ConcurrentDictionary<string, (VmsUser User, DateTimeOffset Expires)> _sessions = new();

    public string CreateSession(VmsUser user)
    {
        var token = Guid.NewGuid().ToString("N");
        _sessions[token] = (user, DateTimeOffset.UtcNow.AddDays(30));
        return token;
    }

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

    public void RemoveSession(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            _sessions.TryRemove(token, out _);
    }
}
