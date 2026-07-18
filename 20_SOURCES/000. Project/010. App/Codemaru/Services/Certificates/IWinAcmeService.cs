using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief win-acme 자동 갱신 작업과 수동 갱신을 처리하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i win acme service functionality and related state.</para>
/// \endif
/// </summary>
public interface IWinAcmeService
{
    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 예약 작업 상태를 조회합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the renewal task async value.</para>
    /// \endif
    /// </summary>
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
    /// <para>예약 작업 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;WinAcmeTaskInfo&gt;</c> result produced by the get renewal task async operation.</para>
    /// \endif
    /// </returns>
    Task<WinAcmeTaskInfo> GetRenewalTaskAsync(CancellationToken cancellationToken);

    /// <summary>
    /// \if KO
    /// <para>\brief win-acme 갱신 명령을 실행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run renew async operation.</para>
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
    /// <param name="force">
    /// \if KO
    /// <para>강제 갱신 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for force.</para>
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
    /// <para>The <c>Task&lt;ProcessExecutionResult&gt;</c> result produced by the run renew async operation.</para>
    /// \endif
    /// </returns>
    Task<ProcessExecutionResult> RunRenewAsync(CertificateMonitorOptions options, bool force, CancellationToken cancellationToken);
}
