using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WeddingThankYou.Services;

/// <summary>
/// \file WeddingOptions.cs
/// \brief Wedding 모듈 전역 옵션(appsettings의 "Wedding" 섹션 바인딩).
/// </summary>
public sealed class WeddingOptions
{
    public string DataPath { get; set; } = "";
    public string DefaultSlug { get; set; } = "";
    public string SuperAdminPassword { get; set; } = "admin1234";
    public string AtlanAuthKey { get; set; } = "";
    public string TmapAppKey { get; set; } = "";

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

    public static WeddingOptions From(IConfiguration configuration)
    {
        var opts = new WeddingOptions();
        configuration.GetSection("Wedding").Bind(opts);
        return opts;
    }

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
