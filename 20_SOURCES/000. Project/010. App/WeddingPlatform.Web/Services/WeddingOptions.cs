using System.IO;
using Microsoft.Extensions.Configuration;

namespace WeddingPlatform.Services;

public sealed class WeddingOptions
{
    public string DataPath { get; set; } = "";
    public string SuperAdminPassword { get; set; } = "admin1234";

    public string ResolvedDataPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DataPath))
                return Path.Combine(AppContext.BaseDirectory, "App_Data", "Wedding");

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
}
