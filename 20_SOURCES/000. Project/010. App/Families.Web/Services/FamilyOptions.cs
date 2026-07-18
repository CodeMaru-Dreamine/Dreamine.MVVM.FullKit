using System.IO;
using Microsoft.Extensions.Configuration;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>Family Options 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyOptions
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
    public string SuperAdminPassword { get; set; } = "v1.600000.1oy6GjdnZFCRZpxUY6R4tQ==.1r45CdqBw/2U22r0bF9KwzdIfyCVkkRt/VcoAg19LrQ=";

    /// <summary>
    /// \if KO
    /// <para>Resolved Data Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the resolved data path value.</para>
    /// \endif
    /// </summary>
    public string ResolvedDataPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DataPath))
                return Path.Combine(AppContext.BaseDirectory, "App_Data", "Family");

            return Path.IsPathRooted(DataPath)
                ? DataPath
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, DataPath));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>From 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the from operation.</para>
    /// \endif
    /// </summary>
    /// <param name="configuration">
    /// \if KO
    /// <para>configuration에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for configuration.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>From 작업에서 생성한 <c>FamilyOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyOptions</c> result produced by the from operation.</para>
    /// \endif
    /// </returns>
    public static FamilyOptions From(IConfiguration configuration)
    {
        var opts = new FamilyOptions();
        configuration.GetSection("Family").Bind(opts);
        return opts;
    }
}
