using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Ghost Account Cleanup Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates ghost account cleanup service functionality and related state.</para>
/// \endif
/// </summary>
public class GhostAccountCleanupService : BackgroundService
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IPortfolioTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>projects 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the projects value.</para>
    /// \endif
    /// </summary>
    private readonly IProjectStore _projects;
    /// <summary>
    /// \if KO
    /// <para>log 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the log value.</para>
    /// \endif
    /// </summary>
    private readonly ILogger<GhostAccountCleanupService> _log;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="GhostAccountCleanupService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="GhostAccountCleanupService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IPortfolioTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPortfolioTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="projects">
    /// \if KO
    /// <para>projects에 사용할 <c>IProjectStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IProjectStore</c> value used for projects.</para>
    /// \endif
    /// </param>
    /// <param name="log">
    /// \if KO
    /// <para>log에 사용할 <c>ILogger&lt;GhostAccountCleanupService&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILogger&lt;GhostAccountCleanupService&gt;</c> value used for log.</para>
    /// \endif
    /// </param>
    public GhostAccountCleanupService(
        IPortfolioTenantStore tenants,
        IProjectStore projects,
        ILogger<GhostAccountCleanupService> log)
    {
        _tenants = tenants;
        _projects = projects;
        _log = log;
    }

    /// <summary>
    /// \if KO
    /// <para>Execute Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Execute Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the execute async operation.</para>
    /// \endif
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            try { await CleanupAsync(); } catch (Exception ex) { _log.LogError(ex, "Cleanup error"); }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Cleanup Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the cleanup async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Cleanup Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the cleanup async operation.</para>
    /// \endif
    /// </returns>
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
