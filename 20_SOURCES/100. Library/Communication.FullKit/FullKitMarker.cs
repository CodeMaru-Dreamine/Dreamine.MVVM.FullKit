namespace Dreamine.Communication.FullKit;

/// <summary>
/// \if KO
/// <para>Dreamine Communication FullKit 메타 패키지를 식별하는 표식 형식입니다.</para>
/// \endif
/// \if EN
/// <para>Identifies the Dreamine Communication FullKit meta package.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>이 형식은 의도적으로 작게 유지됩니다. 실제 패키지는 프로젝트 또는 패키지 참조를 통해 핵심 통신 전송 구현을 하나로 묶습니다.</para>
/// \endif
/// \if EN
/// <para>This type is intentionally tiny. The package aggregates the core communication transports through project or package references.</para>
/// \endif
/// </remarks>
public static class FullKitMarker
{
    /// <summary>
    /// \if KO
    /// <para>이 메타 패키지가 공개하는 NuGet 패키지 식별자를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the NuGet package identifier exposed by this meta package.</para>
    /// \endif
    /// </summary>
    public const string PackageName = "Dreamine.Communication.FullKit";
}
