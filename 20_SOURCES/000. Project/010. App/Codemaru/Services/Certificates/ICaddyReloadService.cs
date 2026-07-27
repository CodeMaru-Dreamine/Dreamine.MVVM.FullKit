using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief Caddy 설정을 검증하고 실행 중인 인스턴스에 다시 로드하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Validates and reloads the active Caddy configuration.</para>
/// \endif
/// </summary>
public interface ICaddyReloadService
{
    /// <summary>
    /// \if KO
    /// <para>\brief Caddy 설정 검증에 성공한 경우에만 graceful reload를 실행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs a graceful reload only after configuration validation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="options">인증서 모니터링 및 Caddy 실행 옵션입니다.</param>
    /// <param name="cancellationToken">취소 요청을 감시하는 토큰입니다.</param>
    /// <returns>검증과 reload 명령 실행 결과입니다.</returns>
    Task<ProcessExecutionResult> ReloadAsync(
        CertificateMonitorOptions options,
        CancellationToken cancellationToken);
}
