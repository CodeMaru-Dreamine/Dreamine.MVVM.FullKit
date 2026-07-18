using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief nginx reload 명령을 실행하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i nginx reload service functionality and related state.</para>
/// \endif
/// </summary>
public interface INginxReloadService
{
    /// <summary>
    /// \if KO
    /// <para>\brief nginx reload를 실행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reload async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>인증서 모니터링 옵션입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>명령 실행 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the reload async operation.</para>
    /// \endif
    /// </returns>
    Task<ProcessExecutionResult> ReloadAsync(CertificateMonitorOptions options, CancellationToken cancellationToken);
}
