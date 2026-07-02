using System.IO;

namespace DreamineWeb.Models;

public sealed class DreamineOptions
{
    public string DataPath { get; set; } = string.Empty;
    public string SuperAdminPassword { get; set; } = "admin1234";
    public string SiteTitle { get; set; } = "Dreamine";
    public string SiteDescription { get; set; } = "Modular Architecture for Real Applications";
    public string GitHubOrgUrl { get; set; } = "https://github.com/CodeMaru-Dreamine";

    /// <summary>라이브러리 소스 루트 (버전 자동 동기화용). 예: D:\Work\...\100. Library</summary>
    public string LibrarySourceRoot { get; set; } = string.Empty;

    public string ResolvedDataPath =>
        string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data")
            : DataPath;
}
