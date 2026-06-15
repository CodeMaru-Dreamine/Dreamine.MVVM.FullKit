namespace Codemaru.Models.Certificates;

/// <summary>
/// \brief 인증서 상태 조회 결과입니다.
/// </summary>
public sealed class CertificateStatusInfo
{
    /// <summary>\brief 인증서 조회 성공 여부입니다.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>\brief 조회 대상 인증서 폴더입니다.</summary>
    public string CertificateDirectory { get; init; } = string.Empty;

    /// <summary>\brief 선택된 인증서 파일 경로입니다.</summary>
    public string CertificatePath { get; init; } = string.Empty;

    /// <summary>\brief 인증서 주체입니다.</summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>\brief 인증서 발급자입니다.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>\brief 인증서 시작 시간입니다.</summary>
    public DateTimeOffset? NotBefore { get; init; }

    /// <summary>\brief 인증서 만료 시간입니다.</summary>
    public DateTimeOffset? NotAfter { get; init; }

    /// <summary>\brief 인증서 남은 일수입니다.</summary>
    public int? RemainingDays { get; init; }

    /// <summary>\brief 인증서 상태입니다.</summary>
    public CertificateHealthState State { get; init; } = CertificateHealthState.Unknown;

    /// <summary>\brief 상태 메시지입니다.</summary>
    public string Message { get; init; } = string.Empty;
}
