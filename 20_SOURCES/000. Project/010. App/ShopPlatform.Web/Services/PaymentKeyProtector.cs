using Microsoft.AspNetCore.DataProtection;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>DataProtection을 이용해 결제 시크릿 키를 암호화/복호화.</para>
/// \endif
/// \if EN
/// <para>Encapsulates payment key protector functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PaymentKeyProtector
{
    /// <summary>
    /// \if KO
    /// <para>protector 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the protector value.</para>
    /// \endif
    /// </summary>
    private readonly IDataProtector _protector;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PaymentKeyProtector"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PaymentKeyProtector"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="provider">
    /// \if KO
    /// <para>provider에 사용할 <c>IDataProtectionProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IDataProtectionProvider</c> value used for provider.</para>
    /// \endif
    /// </param>
    public PaymentKeyProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("ShopPlatform.PaymentKeys");

    /// <summary>
    /// \if KO
    /// <para>Protect 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the protect operation.</para>
    /// \endif
    /// </summary>
    /// <param name="plainText">
    /// \if KO
    /// <para>plain Text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for plain text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Protect 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the protect operation.</para>
    /// \endif
    /// </returns>
    public string Protect(string plainText) => _protector.Protect(plainText);

    /// <summary>
    /// \if KO
    /// <para>Unprotect 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unprotect operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cipherText">
    /// \if KO
    /// <para>cipher Text에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for cipher text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unprotect 작업에서 생성한 <c>string?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> result produced by the unprotect operation.</para>
    /// \endif
    /// </returns>
    public string? Unprotect(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;
        try { return _protector.Unprotect(cipherText); }
        catch { return null; }
    }
}
