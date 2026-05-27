using DreamineVMS.Options;

namespace DreamineVMS.Services.Certificates;

/// <summary>
/// \brief 인증서 설정을 appsettings.local.json에 저장하는 서비스입니다.
/// </summary>
public interface ICertificateSettingsWriter
{
    /// <summary>
    /// \brief 인증서 모니터링 옵션을 로컬 설정 파일에 저장합니다.
    /// </summary>
    /// <param name="options">저장할 인증서 모니터링 옵션입니다.</param>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    /// <returns>비동기 작업입니다.</returns>
    Task SaveAsync(CertificateMonitorOptions options, CancellationToken cancellationToken);
}
