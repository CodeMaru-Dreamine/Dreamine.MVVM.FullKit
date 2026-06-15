using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \brief nginx reload 명령을 실행하는 서비스입니다.
/// </summary>
public interface INginxReloadService
{
    /// <summary>
    /// \brief nginx reload를 실행합니다.
    /// </summary>
    /// <param name="options">인증서 모니터링 옵션입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>명령 실행 결과입니다.</returns>
    Task<ProcessExecutionResult> ReloadAsync(CertificateMonitorOptions options, CancellationToken cancellationToken);
}
