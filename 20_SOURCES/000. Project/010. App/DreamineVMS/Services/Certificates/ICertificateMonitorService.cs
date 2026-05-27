using DreamineVMS.Models.Certificates;
using DreamineVMS.Options;

namespace DreamineVMS.Services.Certificates;

/// <summary>
/// \brief 인증서 상태를 조회하는 서비스입니다.
/// </summary>
public interface ICertificateMonitorService
{
    /// <summary>
    /// \brief 지정된 옵션을 기준으로 인증서 상태를 조회합니다.
    /// </summary>
    /// <param name="options">인증서 모니터링 옵션입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>인증서 상태 정보입니다.</returns>
    Task<CertificateStatusInfo> GetStatusAsync(CertificateMonitorOptions options, CancellationToken cancellationToken);
}
