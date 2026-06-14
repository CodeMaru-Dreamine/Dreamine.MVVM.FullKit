using ShopPlatform.Models;

namespace ShopPlatform.Services;

public interface IShopTenantStore
{
    /// <summary>슬러그로 샵 설정 로드. 없으면 null.</summary>
    Task<ShopConfig?> GetAsync(string slug);

    /// <summary>샵 목록 (슈퍼어드민용).</summary>
    Task<IReadOnlyList<ShopConfig>> GetAllAsync();

    /// <summary>저장 (생성 + 수정 모두).</summary>
    Task SaveAsync(ShopConfig config);

    /// <summary>삭제.</summary>
    Task DeleteAsync(string slug);

    /// <summary>슬러그 사용 가능 여부.</summary>
    Task<bool> ExistsAsync(string slug);
}
