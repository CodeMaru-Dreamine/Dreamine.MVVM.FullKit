using Microsoft.AspNetCore.DataProtection;

namespace ShopPlatform.Services;

/// <summary>DataProtection을 이용해 결제 시크릿 키를 암호화/복호화.</summary>
public sealed class PaymentKeyProtector
{
    private readonly IDataProtector _protector;

    public PaymentKeyProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("ShopPlatform.PaymentKeys");

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string? Unprotect(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;
        try { return _protector.Unprotect(cipherText); }
        catch { return null; }
    }
}
