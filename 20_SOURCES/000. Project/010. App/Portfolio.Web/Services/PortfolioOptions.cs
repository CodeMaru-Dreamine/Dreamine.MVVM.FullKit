using System.IO;
using Microsoft.Extensions.Configuration;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Portfolio Options 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates portfolio options functionality and related state.</para>
/// \endif
/// </summary>
public class PortfolioOptions
{
    /// <summary>
    /// \if KO
    /// <para>Data Path 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the data path value.</para>
    /// \endif
    /// </summary>
    public string DataPath { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Super Admin Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the super admin password value.</para>
    /// \endif
    /// </summary>
    public string SuperAdminPassword { get; set; } = string.Empty;

    /// <summary>Maximum media bytes stored for one portfolio tenant.</summary>
    public long MaxTenantMediaBytes { get; set; } = 1024L * 1024 * 1024;

    /// <summary>Maximum number of uploaded video files stored for one tenant.</summary>
    public int MaxTenantVideoCount { get; set; } = 50;

    /// <summary>
    /// \if KO
    /// <para>Resolved Data Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the resolved data path value.</para>
    /// \endif
    /// </summary>
    public string ResolvedDataPath =>
        string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "Portfolio")
            : DataPath;

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
    /// <para>From 작업에서 생성한 <c>PortfolioOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioOptions</c> result produced by the from operation.</para>
    /// \endif
    /// </returns>
    public static PortfolioOptions From(IConfiguration cfg)
    {
        var o = new PortfolioOptions();
        cfg.GetSection("Portfolio").Bind(o);
        return o;
    }
}
