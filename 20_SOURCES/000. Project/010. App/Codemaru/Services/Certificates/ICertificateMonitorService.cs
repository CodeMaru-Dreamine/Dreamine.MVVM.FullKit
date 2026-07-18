using Codemaru.Models.Certificates;
using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 인증서 상태를 조회하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i certificate monitor service functionality and related state.</para>
/// \endif
/// </summary>
public interface ICertificateMonitorService
{
    /// <summary>
    /// \if KO
    /// <para>\brief 지정된 옵션을 기준으로 인증서 상태를 조회합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the status async value.</para>
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
    /// <para>인증서 상태 정보입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CertificateStatusInfo&gt;</c> result produced by the get status async operation.</para>
    /// \endif
    /// </returns>
    Task<CertificateStatusInfo> GetStatusAsync(CertificateMonitorOptions options, CancellationToken cancellationToken);
}
