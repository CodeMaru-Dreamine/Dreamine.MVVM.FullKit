namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>appsettings.json ShopPlatform 섹션 바인딩.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopOptions
{
    /// <summary>
    /// \if KO
    /// <para>Section 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the section value.</para>
    /// \endif
    /// </summary>
    public const string Section = "ShopPlatform";

    /// <summary>
    /// \if KO
    /// <para>테넌트 데이터 루트 경로 (상대 or 절대).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the data path value.</para>
    /// \endif
    /// </summary>
    public string DataPath { get; set; } = "App_Data/Shops";

    /// <summary>
    /// \if KO
    /// <para>슈퍼어드민 비밀번호 (평문, 환경변수 주입 권장).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the super admin password value.</para>
    /// \endif
    /// </summary>
    public string SuperAdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>배포 도메인 (결제 콜백 URL 생성용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the base url value.</para>
    /// \endif
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5200";

    /// <summary>
    /// \if KO
    /// <para>절대 경로로 변환된 DataPath.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the resolved data path value.</para>
    /// \endif
    /// </summary>
    public string ResolvedDataPath =>
        Path.IsPathRooted(DataPath)
            ? DataPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, DataPath));

    /// <summary>
    /// \if KO
    /// <para>From 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the from operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>From 작업에서 생성한 <c>ShopOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopOptions</c> result produced by the from operation.</para>
    /// \endif
    /// </returns>
    public static ShopOptions From(IConfiguration cfg)
    {
        var opt = new ShopOptions();
        cfg.GetSection(Section).Bind(opt);
        return opt;
    }
}
