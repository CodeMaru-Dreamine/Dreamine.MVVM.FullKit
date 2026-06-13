using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WeddingPlatform.Services;

/// <summary>
/// 사진 없이 24시간 이상 방치된 계정을 자동 삭제합니다.
/// </summary>
public sealed class GhostAccountCleanupService : BackgroundService
{
    private readonly ITenantStore _tenants;
    private readonly ILogger<GhostAccountCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

    public GhostAccountCleanupService(ITenantStore tenants, ILogger<GhostAccountCleanupService> logger)
    {
        _tenants = tenants;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            await RunCleanupAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        try
        {
            var all = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
            var cutoff = DateTime.Now - GracePeriod;

            foreach (var t in all)
            {
                if (t.GalleryFileNames.Count > 0) continue;
                if (t.CreatedAt > cutoff) continue;

                await _tenants.DeleteAsync(t.Slug, ct).ConfigureAwait(false);
                _logger.LogInformation("고스트 계정 자동 삭제: {Slug} (생성: {CreatedAt:g}, 사진 0장)", t.Slug, t.CreatedAt);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "고스트 계정 정리 중 오류 발생");
        }
    }
}
