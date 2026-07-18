namespace Codemaru.Models.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 인증서 상태 조회 결과입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates certificate status info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CertificateStatusInfo
{
    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 조회 성공 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is success value.</para>
    /// \endif
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 조회 대상 인증서 폴더입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the certificate directory value.</para>
    /// \endif
    /// </summary>
    public string CertificateDirectory { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 인증서 파일 경로입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the certificate path value.</para>
    /// \endif
    /// </summary>
    public string CertificatePath { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 주체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the subject value.</para>
    /// \endif
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 발급자입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the issuer value.</para>
    /// \endif
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 시작 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the not before value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset? NotBefore { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 만료 시간입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the not after value.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset? NotAfter { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 남은 일수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the remaining days value.</para>
    /// \endif
    /// </summary>
    public int? RemainingDays { get; init; }

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the state value.</para>
    /// \endif
    /// </summary>
    public CertificateHealthState State { get; init; } = CertificateHealthState.Unknown;

    /// <summary>
    /// \if KO
    /// <para>\brief 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
