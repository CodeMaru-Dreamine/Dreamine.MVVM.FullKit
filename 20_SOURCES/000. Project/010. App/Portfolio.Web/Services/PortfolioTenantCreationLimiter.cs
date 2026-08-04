using System.Security.Cryptography;
using System.Text;

namespace PortfolioApp.Services;

/// <summary>Process-wide protection for the public portfolio sign-up path.</summary>
public sealed class PortfolioTenantCreationLimiter
{
    private static readonly TimeSpan WindowLength = TimeSpan.FromHours(1);
    private const int ClientPermitLimit = 3;
    private const int GlobalPermitLimit = 30;

    private readonly object _sync = new();
    private readonly SemaphoreSlim _creationGate = new(1, 1);
    private readonly Dictionary<string, FixedWindow> _clients = new(StringComparer.Ordinal);
    private FixedWindow _global = new(DateTimeOffset.MinValue, 0);

    public Task<bool> TryEnterCreationAsync() => _creationGate.WaitAsync(0);

    public void ExitCreation() => _creationGate.Release();

    public bool TryAcquire(string? remoteIpAddress, string slug, string ownerName)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string key = BuildClientKey(remoteIpAddress, slug, ownerName);

        lock (_sync)
        {
            if (now - _global.StartedAt >= WindowLength)
            {
                _global = new FixedWindow(now, 0);
            }

            if (_global.PermitCount >= GlobalPermitLimit)
            {
                return false;
            }

            if (!_clients.TryGetValue(key, out FixedWindow? client) ||
                now - client.StartedAt >= WindowLength)
            {
                client = new FixedWindow(now, 0);
                _clients[key] = client;
            }

            if (client.PermitCount >= ClientPermitLimit)
            {
                return false;
            }

            client.PermitCount++;
            _global.PermitCount++;

            foreach (string expiredKey in _clients
                         .Where(pair => now - pair.Value.StartedAt >= WindowLength)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _clients.Remove(expiredKey);
            }

            return true;
        }
    }

    private static string BuildClientKey(string? remoteIpAddress, string slug, string ownerName)
    {
        string material = string.Join('\n',
            string.IsNullOrWhiteSpace(remoteIpAddress) ? "no-ip" : remoteIpAddress.Trim(),
            slug.Trim().ToLowerInvariant(),
            ownerName.Trim().ToLowerInvariant());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private sealed class FixedWindow(DateTimeOffset startedAt, int permitCount)
    {
        public DateTimeOffset StartedAt { get; } = startedAt;
        public int PermitCount { get; set; } = permitCount;
    }
}
