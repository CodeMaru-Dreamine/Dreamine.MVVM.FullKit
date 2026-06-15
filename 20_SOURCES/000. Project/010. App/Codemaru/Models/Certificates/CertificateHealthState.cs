namespace Codemaru.Models.Certificates;

/// <summary>
/// \brief 인증서 상태 등급입니다.
/// </summary>
public enum CertificateHealthState
{
    /// <summary>\brief 인증서 상태를 알 수 없습니다.</summary>
    Unknown = 0,

    /// <summary>\brief 인증서가 정상입니다.</summary>
    Ok = 1,

    /// <summary>\brief 인증서 만료가 가까워졌습니다.</summary>
    Warning = 2,

    /// <summary>\brief 인증서 만료가 임박했습니다.</summary>
    Critical = 3,

    /// <summary>\brief 인증서가 만료되었습니다.</summary>
    Expired = 4,

    /// <summary>\brief 인증서 조회 중 오류가 발생했습니다.</summary>
    Error = 5
}
