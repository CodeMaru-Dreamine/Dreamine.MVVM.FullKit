namespace Codemaru.Models;

/// <summary>
/// \if KO
/// <para>Card Profile 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card profile functionality and related state.</para>
/// \endif
/// </summary>
public sealed record CardProfile(
    string Brand,
    string Tagline,
    string BackBrand,
    string BackTagline,
    string Category,
    string Name,
    string Role,
    string Email,
    string Phone,
    string Address,
    string Website,
    string LandingSlug,
    string AccentColor,
    string ShortBio,
    string LandingDescription,
    string VCardNote,
    string InternalMemo,
    string? LogoImageDataUrl,
    bool RemoveLogoBackground,
    double CardWidthMm,
    double CardHeightMm,
    string FrontFontFamily,
    string BackFontFamily,
    double FrontBrandFontSize,
    double FrontTaglineFontSize,
    double FrontCategoryFontSize,
    double FrontEmailFontSize,
    double BackBrandFontSize,
    double BackTaglineFontSize,
    double BackNameFontSize,
    double BackRoleFontSize,
    string LandingTheme,
    bool IncludePhoneInVCard,
    bool IncludeAddressInVCard,
    string? ImportedFrontImageDataUrl,
    string? ImportedBackImageDataUrl)
{
    /// <summary>
    /// \if KO
    /// <para>Default 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the default value.</para>
    /// \endif
    /// </summary>
    public static CardProfile Default { get; } = new(
        Brand: "Dreamine",
        Tagline: "Modular Automation Framework",
        BackBrand: "Codemaru",
        BackTagline: "Agri-Tech & Automation Platform",
        Category: $"Industrial Automation{Environment.NewLine}AI · Smart Farm · Agri-Tech",
        Name: "장민수",
        Role: "Founder & CTO",
        Email: "togood1983@gmail.com",
        Phone: "+8210-XXXX-XXXX",
        Address: "충청남도 청양군 남양면 XXXX XX-XX 코드마루 빌딩",
        Website: "https://codemaru.co.kr",
        LandingSlug: "card/minsu",
        AccentColor: "#5d7f57",

        ShortBio: "Founder & CTO of CodeMaru",
        LandingDescription: "Building Dreamine, a modular automation framework for industrial automation, AI, smart farm, and agri-tech solutions.",
        VCardNote: "CodeMaru / Dreamine business profile",
        InternalMemo: "Primary public profile for personal business card landing page.",

        LogoImageDataUrl: null,
        RemoveLogoBackground: false,
        CardWidthMm: 90,
        CardHeightMm: 50,
        FrontFontFamily: "Segoe UI",
        BackFontFamily: "Segoe UI",
        FrontBrandFontSize: 28,
        FrontTaglineFontSize: 17,
        FrontCategoryFontSize: 18,
        FrontEmailFontSize: 14,
        BackBrandFontSize: 22,
        BackTaglineFontSize: 14,
        BackNameFontSize: 16,
        BackRoleFontSize: 14,
        LandingTheme: "light",
        IncludePhoneInVCard: false,
        IncludeAddressInVCard: false,
        ImportedFrontImageDataUrl: null,
        ImportedBackImageDataUrl: null);

    /// <summary>
    /// \if KO
    /// <para>Landing Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the landing url value.</para>
    /// \endif
    /// </summary>
    public string LandingUrl
    {
        get
        {
            var website = string.IsNullOrWhiteSpace(Website)
                ? "https://dreamine.local/card"
                : Website.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(LandingSlug))
            {
                return website;
            }

            var slug = LandingSlug.Trim().Trim('/').Replace(" ", "-", StringComparison.Ordinal);

            return website.EndsWith($"/{slug}", StringComparison.OrdinalIgnoreCase)
                ? website
                : $"{website}/{slug}";
        }
    }
}
