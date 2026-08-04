using System.Security.Cryptography;
using System.Text;

namespace PortfolioApp.Services;

/// <summary>Process-wide fixed-window guard for expensive password verification.</summary>
public sealed class PortfolioLoginRateLimiter
{
    private static readonly TimeSpan WindowLength = TimeSpan.FromMinutes(15);
    private readonly object _sync = new();
    private readonly Dictionary<string, AttemptWindow> _attempts = new(StringComparer.Ordinal);

    public bool TryBeginAttempt(string scope, string? remoteIpAddress, string subject, int permitLimit)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string key = BuildKey(scope, remoteIpAddress, subject);
        lock (_sync)
        {
            if (!_attempts.TryGetValue(key, out AttemptWindow? window) ||
                now - window.StartedAt >= WindowLength)
            {
                _attempts[key] = new AttemptWindow(now, 1);
                return true;
            }

            if (window.Count >= permitLimit) return false;
            window.Count++;
            return true;
        }
    }

    public void Reset(string scope, string? remoteIpAddress, string subject)
    {
        string key = BuildKey(scope, remoteIpAddress, subject);
        lock (_sync) _attempts.Remove(key);
    }

    private static string BuildKey(string scope, string? remoteIpAddress, string subject)
    {
        string material = string.Join('\n', scope.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(remoteIpAddress) ? "no-ip" : remoteIpAddress.Trim(),
            subject.Trim().ToLowerInvariant());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private sealed class AttemptWindow(DateTimeOffset startedAt, int count)
    {
        public DateTimeOffset StartedAt { get; } = startedAt;
        public int Count { get; set; } = count;
    }
}
