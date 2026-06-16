using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Services;

public class GhostAccountCleanupService : BackgroundService
{
    private readonly IPortfolioTenantStore _tenants;
    private readonly IProjectStore _projects;
    private readonly ILogger<GhostAccountCleanupService> _log;

    public GhostAccountCleanupService(
        IPortfolioTenantStore tenants,
        IProjectStore projects,
        ILogger<GhostAccountCleanupService> log)
    {
        _tenants = tenants;
        _projects = projects;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            try { await CleanupAsync(); } catch (Exception ex) { _log.LogError(ex, "Cleanup error"); }
        }
    }

    private async Task CleanupAsync()
    {
        var all = await _tenants.GetAllAsync();
        var cutoff = DateTime.Now.AddHours(-24);
        foreach (var cfg in all)
        {
            if (cfg.CreatedAt > cutoff) continue;
            var projects = await _projects.GetAllAsync(cfg.Slug);
            if (projects.Count == 0)
            {
                await _tenants.DeleteAsync(cfg.Slug);
                _log.LogInformation("Ghost account deleted: {Slug}", cfg.Slug);
            }
        }
    }
}
