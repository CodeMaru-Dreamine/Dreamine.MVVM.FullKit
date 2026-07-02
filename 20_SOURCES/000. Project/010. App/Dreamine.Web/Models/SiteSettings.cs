namespace DreamineWeb.Models;

public class SiteSettings
{
    public string Title       { get; set; } = "Dreamine";
    public string Description { get; set; } = "Modular Architecture for Real Applications";
    public string IconUrl     { get; set; } = "";
    public string GitHubUrl   { get; set; } = "https://github.com/CodeMaru-Dreamine";

    // Open Graph / SNS 공유
    public string OgTitle       { get; set; } = "";   // 비면 Title 사용
    public string OgDescription { get; set; } = "";   // 비면 Description 사용
    public string OgImageUrl    { get; set; } = "/img/og-dreamine.png";
    public string OgSiteName    { get; set; } = "Dreamine";
    public string OgUrl         { get; set; } = "https://dreamine.kr/";
    public string TwitterCard   { get; set; } = "summary_large_image";
}
