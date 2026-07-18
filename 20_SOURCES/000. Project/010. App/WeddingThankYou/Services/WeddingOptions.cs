using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WeddingThankYou.Services;

/// <summary>
/// \if KO
/// <para>\file WeddingOptions.cs \brief Wedding 모듈 전역 옵션(appsettings의 "Wedding" 섹션 바인딩).</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WeddingOptions
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
    /// <para>Default Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the default slug value.</para>
    /// \endif
    /// </summary>
    public string DefaultSlug { get; set; } = "";
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
    /// <para>Atlan Auth Key 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the atlan auth key value.</para>
    /// \endif
    /// </summary>
    public string AtlanAuthKey { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Tmap App Key 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tmap app key value.</para>
    /// \endif
    /// </summary>
    public string TmapAppKey { get; set; } = "";

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
            {
                var outputDataPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "Wedding");
                var sourceDataPath = TryFindSourceDataPath();
                return sourceDataPath ?? outputDataPath;
            }

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
    /// <para>From 작업에서 생성한 <c>WeddingOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingOptions</c> result produced by the from operation.</para>
    /// \endif
    /// </returns>
    public static WeddingOptions From(IConfiguration configuration)
    {
        var opts = new WeddingOptions();
        configuration.GetSection("Wedding").Bind(opts);
        return opts;
    }

    /// <summary>
    /// \if KO
    /// <para>Find Source Data Path 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to find source data path and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Try Find Source Data Path 작업에서 생성한 <c>string?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> result produced by the try find source data path operation.</para>
    /// \endif
    /// </returns>
    private static string? TryFindSourceDataPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "App_Data", "Wedding");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "WeddingThankYou.csproj")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
