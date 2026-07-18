namespace Codemaru.Models.Certificates;

/// <summary>
/// \if KO
/// <para>\brief 인증서 상태 등급입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates certificate health state functionality and related state.</para>
/// \endif
/// </summary>
public enum CertificateHealthState
{
    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 상태를 알 수 없습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the unknown value.</para>
    /// \endif
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서가 정상입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the ok value.</para>
    /// \endif
    /// </summary>
    Ok = 1,

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 만료가 가까워졌습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the warning value.</para>
    /// \endif
    /// </summary>
    Warning = 2,

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 만료가 임박했습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the critical value.</para>
    /// \endif
    /// </summary>
    Critical = 3,

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서가 만료되었습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the expired value.</para>
    /// \endif
    /// </summary>
    Expired = 4,

    /// <summary>
    /// \if KO
    /// <para>\brief 인증서 조회 중 오류가 발생했습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the error value.</para>
    /// \endif
    /// </summary>
    Error = 5
}
