using System.Collections.Concurrent;
using DreamineVMS.Web.Models;

namespace DreamineVMS.Web.Services.Agent;

/// <summary>WPF 에이전트가 HLS를 푸시할 때 사용하는 단기 토큰을 관리합니다.</summary>
public sealed class AgentTokenService
{
    private readonly ConcurrentDictionary<string, (VmsUser User, DateTimeOffset Expires)> _tokens = new();

    public string Issue(VmsUser user)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = (user, DateTimeOffset.UtcNow.AddHours(24));
        return token;
    }

    public VmsUser? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (!_tokens.TryGetValue(token, out var entry)) return null;
        if (entry.Expires < DateTimeOffset.UtcNow) { _tokens.TryRemove(token, out _); return null; }
        return entry.User;
    }
}
