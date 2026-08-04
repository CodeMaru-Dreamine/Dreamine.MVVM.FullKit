using System.Security.Cryptography;
using System.Text;

namespace PortfolioApp.Services;

/// <summary>
/// Applies a process-wide fixed-window limit to public contact submissions.
/// </summary>
public sealed class PortfolioContactRateLimiter
{
    private static readonly TimeSpan WindowLength = TimeSpan.FromMinutes(1);
    private const int ClientPermitLimit = 3;
    private const int TenantPermitLimit = 12;
    private const int MaximumTrackedClients = 10_000;

    private readonly object _sync = new();
    private readonly Dictionary<string, FixedWindow> _windows = new(StringComparer.Ordinal);
    private DateTimeOffset _lastCleanupAt = DateTimeOffset.MinValue;

    /// <summary>
    /// Attempts to consume one contact-submission permit for a tenant and client.
    /// </summary>
    public bool TryAcquire(
        string tenantSlug,
        string? remoteIpAddress,
        string? senderEmail,
        string? senderName,
        out TimeSpan retryAfter)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string clientKey = BuildClientKey(tenantSlug, remoteIpAddress, senderEmail, senderName);
        string tenantKey = HashKey($"{tenantSlug.Trim().ToLowerInvariant()}\ntenant");

        lock (_sync)
        {
            CleanupNoLock(now);

            bool clientAllowed = CanAcquireNoLock(clientKey, ClientPermitLimit, now, out TimeSpan clientRetryAfter);
            bool tenantAllowed = CanAcquireNoLock(tenantKey, TenantPermitLimit, now, out TimeSpan tenantRetryAfter);
            if (!clientAllowed || !tenantAllowed)
            {
                retryAfter = clientRetryAfter > tenantRetryAfter ? clientRetryAfter : tenantRetryAfter;
                return false;
            }

            AcquireNoLock(clientKey, now);
            AcquireNoLock(tenantKey, now);
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private bool CanAcquireNoLock(string key, int permitLimit, DateTimeOffset now, out TimeSpan retryAfter)
    {
        if (!_windows.TryGetValue(key, out FixedWindow? window) || now - window.StartedAt >= WindowLength)
        {
            retryAfter = TimeSpan.Zero;
            return true;
        }

        if (window.PermitCount < permitLimit)
        {
            retryAfter = TimeSpan.Zero;
            return true;
        }

        retryAfter = WindowLength - (now - window.StartedAt);
        return false;
    }

    private void AcquireNoLock(string key, DateTimeOffset now)
    {
        if (!_windows.TryGetValue(key, out FixedWindow? window) || now - window.StartedAt >= WindowLength)
        {
            _windows[key] = new FixedWindow(now, 1);
            return;
        }

        window.PermitCount++;
    }

    private void CleanupNoLock(DateTimeOffset now)
    {
        if (_windows.Count < 1_024 && now - _lastCleanupAt < WindowLength)
        {
            return;
        }

        foreach (string expiredKey in _windows
                     .Where(pair => now - pair.Value.StartedAt >= WindowLength)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _windows.Remove(expiredKey);
        }

        if (_windows.Count > MaximumTrackedClients)
        {
            foreach (string oldestKey in _windows
                         .OrderBy(pair => pair.Value.StartedAt)
                         .Take(_windows.Count - MaximumTrackedClients)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _windows.Remove(oldestKey);
            }
        }

        _lastCleanupAt = now;
    }

    private static string BuildClientKey(
        string tenantSlug,
        string? remoteIpAddress,
        string? senderEmail,
        string? senderName)
    {
        string tenant = tenantSlug.Trim().ToLowerInvariant();
        string address = string.IsNullOrWhiteSpace(remoteIpAddress) ? "no-ip" : remoteIpAddress.Trim();
        string sender = !string.IsNullOrWhiteSpace(senderEmail)
            ? $"email:{senderEmail.Trim().ToLowerInvariant()}"
            : $"name:{(senderName ?? string.Empty).Trim().ToLowerInvariant()}";

        // Include the sender fingerprint as well as the observed address. This
        // avoids treating every visitor behind an unconfigured reverse proxy as
        // one client, while the separate tenant-wide bucket still caps abuse.
        return HashKey($"{tenant}\nip:{address}\n{sender}");
    }

    private static string HashKey(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class FixedWindow(DateTimeOffset startedAt, int permitCount)
    {
        public DateTimeOffset StartedAt { get; } = startedAt;
        public int PermitCount { get; set; } = permitCount;
    }
}
