using System.IO;
using Microsoft.Extensions.Configuration;

namespace FamiliesApp.Services;

public sealed class FamilyOptions
{
    public string DataPath { get; set; } = "";
    public string SuperAdminPassword { get; set; } = "admin1234";

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

    public static FamilyOptions From(IConfiguration configuration)
    {
        var opts = new FamilyOptions();
        configuration.GetSection("Family").Bind(opts);
        return opts;
    }
}
