using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>현재 요청의 테넌트 정보. Scoped DI로 미들웨어에서 설정.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tenant context functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TenantContext
{
    /// <summary>
    /// \if KO
    /// <para>Config 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the config value.</para>
    /// \endif
    /// </summary>
    public ShopConfig? Config { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Is Resolved 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is resolved value.</para>
    /// \endif
    /// </summary>
    public bool IsResolved => Config != null;
    /// <summary>
    /// \if KO
    /// <para>Slug 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the slug value.</para>
    /// \endif
    /// </summary>
    public string Slug => Config?.Slug ?? string.Empty;
}
