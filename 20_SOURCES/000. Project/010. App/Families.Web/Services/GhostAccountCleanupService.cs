using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>포스트 없이 24시간 이상 방치된 계정을 자동 삭제합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates ghost account cleanup service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class GhostAccountCleanupService : BackgroundService
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IFamilyTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>posts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the posts value.</para>
    /// \endif
    /// </summary>
    private readonly IPostStore _posts;
    /// <summary>
    /// \if KO
    /// <para>logger 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logger value.</para>
    /// \endif
    /// </summary>
    private readonly ILogger<GhostAccountCleanupService> _logger;
    /// <summary>
    /// \if KO
    /// <para>Interval 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the interval value.</para>
    /// \endif
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    /// <summary>
    /// \if KO
    /// <para>Grace Period 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the grace period value.</para>
    /// \endif
    /// </summary>
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

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
    /// <para>tenants에 사용할 <c>IFamilyTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IFamilyTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="posts">
    /// \if KO
    /// <para>posts에 사용할 <c>IPostStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPostStore</c> value used for posts.</para>
    /// \endif
    /// </param>
    /// <param name="logger">
    /// \if KO
    /// <para>logger에 사용할 <c>ILogger&lt;GhostAccountCleanupService&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILogger&lt;GhostAccountCleanupService&gt;</c> value used for logger.</para>
    /// \endif
    /// </param>
    public GhostAccountCleanupService(IFamilyTenantStore tenants, IPostStore posts,
        ILogger<GhostAccountCleanupService> logger)
    {
        _tenants = tenants;
        _posts = posts;
        _logger = logger;
    }

    /// <summary>
    /// \if KO
    /// <para>Execute Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the execute async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="stoppingToken">
    /// \if KO
    /// <para>stopping Token에 사용할 <c>CancellationToken</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CancellationToken</c> value used for stopping token.</para>
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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            await RunCleanupAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Run Cleanup Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run cleanup async operation.</para>
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
    /// <para>Run Cleanup Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the run cleanup async operation.</para>
    /// \endif
    /// </returns>
    private async Task RunCleanupAsync(CancellationToken ct)
    {
        try
        {
            var all = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
            var cutoff = DateTime.Now - GracePeriod;

            foreach (var t in all)
            {
                if (t.CreatedAt > cutoff) continue;

                var postList = await _posts.GetAllAsync(t.Slug, ct).ConfigureAwait(false);
                if (postList.Count > 0) continue;

                await _tenants.DeleteAsync(t.Slug, ct).ConfigureAwait(false);
                _logger.LogInformation("고스트 계정 자동 삭제: {Slug} (생성: {CreatedAt:g}, 포스트 0개)", t.Slug, t.CreatedAt);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "고스트 계정 정리 중 오류 발생");
        }
    }
}
