using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \brief win-acme 자동 갱신 작업과 수동 갱신을 처리하는 서비스입니다.
/// </summary>
public interface IWinAcmeService
{
    /// <summary>
    /// \brief win-acme 예약 작업 상태를 조회합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>예약 작업 상태입니다.</returns>
    Task<WinAcmeTaskInfo> GetRenewalTaskAsync(CancellationToken cancellationToken);

    /// <summary>
    /// \brief win-acme 갱신 명령을 실행합니다.
    /// </summary>
    /// <param name="options">인증서 모니터링 옵션입니다.</param>
    /// <param name="force">강제 갱신 여부입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>명령 실행 결과입니다.</returns>
    Task<ProcessExecutionResult> RunRenewAsync(CertificateMonitorOptions options, bool force, CancellationToken cancellationToken);
}
