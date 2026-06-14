namespace ShopPlatform.Models;

/// <summary>appsettings.json ShopPlatform 섹션 바인딩.</summary>
public sealed class ShopOptions
{
    public const string Section = "ShopPlatform";

    /// <summary>테넌트 데이터 루트 경로 (상대 or 절대).</summary>
    public string DataPath { get; set; } = "App_Data/Shops";

    /// <summary>슈퍼어드민 비밀번호 (평문, 환경변수 주입 권장).</summary>
    public string SuperAdminPassword { get; set; } = string.Empty;

    /// <summary>배포 도메인 (결제 콜백 URL 생성용).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5200";

    /// <summary>절대 경로로 변환된 DataPath.</summary>
    public string ResolvedDataPath =>
        Path.IsPathRooted(DataPath)
            ? DataPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, DataPath));

    public static ShopOptions From(IConfiguration cfg)
    {
        var opt = new ShopOptions();
        cfg.GetSection(Section).Bind(opt);
        return opt;
    }
}
