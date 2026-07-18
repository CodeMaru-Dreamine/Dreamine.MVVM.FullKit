using Codemaru.Options;

namespace Codemaru.Services.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 인증서 설정을 appsettings.local.json에 저장하는 서비스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i certificate settings writer functionality and related state.</para>
/// \endif
/// </summary>
public interface ICertificateSettingsWriter
{
    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 모니터링 옵션을 로컬 설정 파일에 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>저장할 인증서 모니터링 옵션입니다.</para>
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
    /// <para>비동기 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    Task SaveAsync(CertificateMonitorOptions options, CancellationToken cancellationToken);
}
