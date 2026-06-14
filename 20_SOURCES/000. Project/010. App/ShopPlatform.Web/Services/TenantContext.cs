using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>현재 요청의 테넌트 정보. Scoped DI로 미들웨어에서 설정.</summary>
public sealed class TenantContext
{
    public ShopConfig? Config { get; set; }
    public bool IsResolved => Config != null;
    public string Slug => Config?.Slug ?? string.Empty;
}
