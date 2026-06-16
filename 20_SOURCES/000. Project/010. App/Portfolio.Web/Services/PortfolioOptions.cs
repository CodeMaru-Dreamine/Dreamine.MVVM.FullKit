using System.IO;
using Microsoft.Extensions.Configuration;

namespace PortfolioApp.Services;

public class PortfolioOptions
{
    public string DataPath { get; set; } = "";
    public string SuperAdminPassword { get; set; } = "admin1234";

    public string ResolvedDataPath =>
        string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "Portfolio")
            : DataPath;

    public static PortfolioOptions From(IConfiguration cfg)
    {
        var o = new PortfolioOptions();
        cfg.GetSection("Portfolio").Bind(o);
        return o;
    }
}
