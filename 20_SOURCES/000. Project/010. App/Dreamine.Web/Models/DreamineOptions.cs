using System.IO;

namespace DreamineWeb.Models;

public sealed class DreamineOptions
{
    public string DataPath { get; set; } = string.Empty;
    public string SuperAdminPassword { get; set; } = "admin1234";
    public string SiteTitle { get; set; } = "Dreamine";
    public string SiteDescription { get; set; } = "Modular Architecture for Real Applications";
    public string GitHubOrgUrl { get; set; } = "https://github.com/CodeMaru-Dreamine";

    /// <summary>XML doc 자동 스캔할 라이브러리 루트 폴더 (비어있으면 자동 스캔 비활성)</summary>
    public string XmlDocScanRoot { get; set; } = string.Empty;

    public string ResolvedDataPath =>
        string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data")
            : DataPath;
}
