using Codemaru.Models;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>Landing Page Exporter 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates landing page exporter functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LandingPageExporter
{
    /// <summary>
    /// \if KO
    /// <para>Card Side Svg 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the card side svg value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="qrSvg">
    /// \if KO
    /// <para>qr Svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr svg.</para>
    /// \endif
    /// </param>
    /// <param name="front">
    /// \if KO
    /// <para>front에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for front.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Card Side Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build card side svg operation.</para>
    /// \endif
    /// </returns>
    public string BuildCardSideSvg(CardProfile profile, string qrSvg, bool front)
    {
        if (front && !string.IsNullOrWhiteSpace(profile.ImportedFrontImageDataUrl))
        {
            return BuildImportedImageCardSvg(profile, profile.ImportedFrontImageDataUrl);
        }

        if (!front && !string.IsNullOrWhiteSpace(profile.ImportedBackImageDataUrl))
        {
            return BuildImportedImageCardSvg(profile, profile.ImportedBackImageDataUrl);
        }

        return front ? BuildFrontCardSvg(profile, qrSvg) : BuildBackCardSvg(profile);
    }

    /// <summary>
    /// \if KO
    /// <para>Card Side Html 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the card side html value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="qrSvg">
    /// \if KO
    /// <para>qr Svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr svg.</para>
    /// \endif
    /// </param>
    /// <param name="front">
    /// \if KO
    /// <para>front에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for front.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Card Side Html 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build card side html operation.</para>
    /// \endif
    /// </returns>
    public string BuildCardSideHtml(CardProfile profile, string qrSvg, bool front)
    {
        var card = front
            ? BuildFrontCard(profile, qrSvg)
            : BuildBackCard(profile);
        var title = front ? "Business Card Front" : "Business Card Back";

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"ko\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine($"  <title>{title}</title>");
        html.AppendLine("  <style>");
        html.AppendLine($"    {BuildCardCss(profile)}");
        html.AppendLine("    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #f2f4f0; padding: 24px; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine(card);
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    /// <summary>
    /// \if KO
    /// <para>Html 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the html value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="qrPayload">
    /// \if KO
    /// <para>qr Payload에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr payload.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Html 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build html operation.</para>
    /// \endif
    /// </returns>
    public string BuildHtml(CardProfile profile, string qrPayload)
    {
        var palette = GetThemePalette(profile.LandingTheme);
        var logo = string.IsNullOrWhiteSpace(profile.LogoImageDataUrl)
            ? $"<div class=\"brand-mark\" style=\"background:{Html(profile.AccentColor)}\"></div>"
            : $"<img class=\"brand-logo\" src=\"{Html(profile.LogoImageDataUrl)}\" alt=\"{Html(profile.BackBrand)} logo\" />";

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"ko\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine($"  <title>{Html(profile.Name)} - {Html(profile.BackBrand)}</title>");
        html.AppendLine($"  <meta name=\"description\" content=\"{Html(profile.LandingDescription)}\" />");
        html.AppendLine("  <style>");
        html.AppendLine($"    :root {{ --bg: {palette.Background}; --panel: {palette.Panel}; --text: {palette.Text}; --muted: {palette.Muted}; --accent: {Html(profile.AccentColor)}; --line: rgba(120,140,120,.24); }}");
        html.AppendLine("    * { box-sizing: border-box; }");
        html.AppendLine("    body { margin: 0; min-height: 100vh; font-family: \"Segoe UI\", \"Noto Sans KR\", \"Malgun Gothic\", sans-serif; background: var(--bg); color: var(--text); }");
        html.AppendLine("    body::before { content: \"\"; position: fixed; inset: 0; pointer-events: none; background-image: linear-gradient(rgba(120,140,120,.08) 1px, transparent 1px), linear-gradient(90deg, rgba(120,140,120,.08) 1px, transparent 1px); background-size: 32px 32px; mask-image: linear-gradient(to bottom, rgba(0,0,0,.72), transparent 70%); }");
        html.AppendLine("    main { position: relative; width: min(1040px, calc(100% - 32px)); margin: 0 auto; padding: 42px 0; }");
        html.AppendLine("    .shell { display: grid; grid-template-columns: minmax(0, .95fr) minmax(320px, 1.05fr); gap: 18px; align-items: stretch; }");
        html.AppendLine("    .hero, .details { background: color-mix(in srgb, var(--panel) 94%, transparent); border: 1px solid var(--line); border-radius: 8px; box-shadow: 0 20px 58px rgba(0,0,0,.12); }");
        html.AppendLine("    .hero { min-height: 560px; display: grid; align-content: space-between; gap: 28px; padding: 34px; }");
        html.AppendLine("    .brand-row { display: flex; align-items: center; gap: 14px; }");
        html.AppendLine("    .brand-frame { width: 92px; aspect-ratio: 8 / 5; display: grid; place-items: center; border: 2px solid var(--accent); border-radius: 8px; padding: 8px; overflow: hidden; background: rgba(255,255,255,.5); }");
        html.AppendLine("    .brand-logo { display: block; width: 100%; height: 100%; object-fit: contain; }");
        html.AppendLine("    .brand-mark { width: 34px; height: 34px; border-radius: 999px; }");
        html.AppendLine("    .eyebrow { margin: 0; color: var(--muted); font-size: 12px; font-weight: 900; text-transform: uppercase; letter-spacing: 0; }");
        html.AppendLine("    h1 { margin: 18px 0 10px; font-size: clamp(44px, 8vw, 86px); line-height: .92; letter-spacing: 0; }");
        html.AppendLine("    .role { margin: 0; color: var(--text); font-size: clamp(18px, 3vw, 28px); font-weight: 650; }");
        html.AppendLine("    .tagline { margin: 8px 0 0; color: var(--muted); font-size: 17px; line-height: 1.55; }");
        html.AppendLine("    .cta-row { display: flex; flex-wrap: wrap; gap: 10px; }");
        html.AppendLine("    .cta { min-height: 44px; display: inline-flex; align-items: center; justify-content: center; padding: 0 16px; border-radius: 6px; text-decoration: none; font-weight: 800; }");
        html.AppendLine("    .cta.primary { background: var(--accent); color: #fff; }");
        html.AppendLine("    .cta.secondary { color: var(--text); border: 1px solid var(--line); }");
        html.AppendLine("    .details { padding: 28px; display: grid; gap: 24px; }");
        html.AppendLine("    .section { display: grid; gap: 12px; }");
        html.AppendLine("    h2 { margin: 0; font-size: 17px; }");
        html.AppendLine("    p { margin: 0; color: var(--muted); line-height: 1.62; }");
        html.AppendLine("    dl { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; margin: 0; }");
        html.AppendLine("    dt { color: var(--muted); font-size: 12px; font-weight: 900; text-transform: uppercase; }");
        html.AppendLine("    dd { margin: 4px 0 0; overflow-wrap: anywhere; }");
        html.AppendLine("    a { color: var(--accent); overflow-wrap: anywhere; }");
        html.AppendLine("    .wide { grid-column: 1 / -1; }");
        html.AppendLine("    .footer { margin-top: 18px; color: var(--muted); font-size: 12px; text-align: center; }");
        html.AppendLine("    @media (max-width: 820px) { main { padding: 16px 0; } .shell { grid-template-columns: 1fr; } .hero { min-height: auto; padding: 24px; } .details { padding: 22px; } dl { grid-template-columns: 1fr; } }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <main>");
        html.AppendLine("    <div class=\"shell\">");
        html.AppendLine("      <section class=\"hero\">");
        html.AppendLine("        <div class=\"brand-row\">");
        html.AppendLine($"          <div class=\"brand-frame\">{logo}</div>");
        html.AppendLine("          <div>");
        html.AppendLine($"            <p class=\"eyebrow\">{Html(profile.BackBrand)}</p>");
        html.AppendLine($"            <p class=\"tagline\">{Html(profile.BackTagline)}</p>");
        html.AppendLine("          </div>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div>");
        html.AppendLine($"          <h1>{Html(profile.Name)}</h1>");
        html.AppendLine($"          <p class=\"role\">{Html(profile.Role)}</p>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"cta-row\">");
        html.AppendLine($"          <a class=\"cta primary\" href=\"mailto:{Html(profile.Email)}\">메일 보내기</a>");
        if (!string.IsNullOrWhiteSpace(profile.Phone))
        {
            html.AppendLine($"          <a class=\"cta secondary\" href=\"tel:{Html(profile.Phone)}\">전화하기</a>");
        }
        html.AppendLine($"          <a class=\"cta secondary\" href=\"{Html(qrPayload)}\">명함 링크</a>");
        html.AppendLine("        </div>");
        html.AppendLine("      </section>");
        html.AppendLine("      <section class=\"details\">");
        html.AppendLine("        <div class=\"section\">");
        html.AppendLine("          <h2>About</h2>");
        html.AppendLine($"          <p>{Html(profile.LandingDescription)}</p>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"section\">");
        html.AppendLine("          <h2>Contact</h2>");
        html.AppendLine("          <dl>");
        html.AppendLine($"            <div><dt>Email</dt><dd>{Html(profile.Email)}</dd></div>");
        html.AppendLine($"            <div><dt>Phone</dt><dd>{Html(DisplayValue(profile.Phone))}</dd></div>");
        html.AppendLine($"            <div><dt>Company</dt><dd>{Html(profile.BackBrand)}</dd></div>");
        html.AppendLine($"            <div><dt>Address</dt><dd>{Html(DisplayValue(profile.Address))}</dd></div>");
        html.AppendLine($"            <div class=\"wide\"><dt>Card URL</dt><dd><a href=\"{Html(qrPayload)}\">{Html(qrPayload)}</a></dd></div>");
        html.AppendLine("          </dl>");
        html.AppendLine("        </div>");
        html.AppendLine("      </section>");
        html.AppendLine("    </div>");
        html.AppendLine("    <p class=\"footer\">Generated by CodeMaru CardHybrid · Powered by Dreamine</p>");
        html.AppendLine("  </main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    /// <summary>
    /// \if KO
    /// <para>Front Card 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the front card value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="qrSvg">
    /// \if KO
    /// <para>qr Svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr svg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Front Card 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build front card operation.</para>
    /// \endif
    /// </returns>
    private static string BuildFrontCard(CardProfile profile, string qrSvg)
    {
        return $"""
        <div class="business-card front">
          <div class="front-copy">
            <h2>{Html(profile.Brand)}</h2>
            <p>{Html(profile.Tagline)}</p>
            <strong>{Html(profile.Category)}</strong>
            <span>{Html(profile.Email)}</span>
          </div>
          <div class="qr-stack">
            <div class="qr-box">{qrSvg}</div>
          </div>
        </div>
        """;
    }

    /// <summary>
    /// \if KO
    /// <para>Back Card 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the back card value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Back Card 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build back card operation.</para>
    /// \endif
    /// </returns>
    private static string BuildBackCard(CardProfile profile)
    {
        var logo = string.IsNullOrWhiteSpace(profile.LogoImageDataUrl)
            ? $"<span style=\"background:{Html(profile.AccentColor)}\"></span>"
            : $"<img src=\"{Html(profile.LogoImageDataUrl)}\" alt=\"{Html(profile.BackBrand)} logo\" />";

        return $"""
        <div class="business-card back">
          <div class="back-logo-slot">{logo}</div>
          <div class="back-copy">
            <h3>{Html(profile.BackBrand)}</h3>
            <p>{Html(profile.BackTagline)}</p>
            <strong>{Html(profile.Name)}</strong>
            <span>{Html(profile.Role)}</span>
          </div>
        </div>
        """;
    }

    /// <summary>
    /// \if KO
    /// <para>Card Css 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the card css value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Card Css 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build card css operation.</para>
    /// \endif
    /// </returns>
    private static string BuildCardCss(CardProfile profile)
    {
        var width = Math.Clamp(profile.CardWidthMm, 40, 140);
        var height = Math.Clamp(profile.CardHeightMm, 25, 90);
        return string.Join(Environment.NewLine,
            "* { box-sizing: border-box; }",
            $".business-card {{ width: {width * 5:0.##}px; aspect-ratio: {width:0.##} / {height:0.##}; border: 1px solid #cdd5cb; border-radius: 7px; background: #fbfaf7; box-shadow: 0 12px 28px rgba(38,49,39,.08); overflow: hidden; }}",
            $".business-card.front {{ display: grid; grid-template-columns: minmax(0, 1fr) minmax(92px, 30%); gap: 20px; align-items: center; padding: 30px; font-family: '{Css(profile.FrontFontFamily)}', 'Malgun Gothic', sans-serif; }}",
            $".business-card.front h2 {{ margin: 0 0 6px; color: {Html(profile.AccentColor)}; font-size: {profile.FrontBrandFontSize:0.##}px; }}",
            $".business-card.front p {{ margin: 0 0 14px; color: #3f4841; font-size: {profile.FrontTaglineFontSize:0.##}px; }}",
            $".business-card.front strong {{ display: block; margin-bottom: 14px; color: #222a24; font-size: {profile.FrontCategoryFontSize:0.##}px; white-space: pre-line; }}",
            $".business-card.front span {{ color: #4d574f; font-size: {profile.FrontEmailFontSize:0.##}px; }}",
            ".qr-stack { display: grid; justify-items: center; min-width: 0; }",
            ".qr-box { width: min(132px, 100%); aspect-ratio: 1; padding: 10px; border: 1px solid #202520; border-radius: 999px; background: #fff; }",
            ".qr-box svg { display: block; width: 100%; height: 100%; }",
            $".business-card.back {{ display: grid; grid-template-rows: minmax(92px, .9fr) auto; align-items: center; justify-items: center; text-align: center; gap: 10px; padding: 24px; font-family: '{Css(profile.BackFontFamily)}', 'Malgun Gothic', sans-serif; }}",
            $".back-logo-slot {{ width: 154px; aspect-ratio: 8 / 5; display: grid; place-items: center; border: 2px solid {Html(profile.AccentColor)}; border-radius: 20px; padding: 8px; overflow: hidden; }}",
            ".back-logo-slot span { width: 52%; height: 52%; border-radius: 50%; }",
            ".back-logo-slot img { display: block; width: 100%; height: 100%; object-fit: contain; border-radius: 5px; }",
            ".back-copy { display: grid; justify-items: center; gap: 5px; }",
            $".business-card.back h3 {{ margin: 0; font-size: {profile.BackBrandFontSize:0.##}px; line-height: 1.12; }}",
            $".business-card.back p {{ margin: 0; color: #576158; font-size: {profile.BackTaglineFontSize:0.##}px; }}",
            $".business-card.back strong {{ display: block; margin-top: 8px; font-size: {profile.BackNameFontSize:0.##}px; line-height: 1.15; }}",
            $".business-card.back span {{ color: #4d574f; font-size: {profile.BackRoleFontSize:0.##}px; }}");
    }

    /// <summary>
    /// \if KO
    /// <para>모바일/iPhone/Android/Gmail용 표준 vCard 3.0 문자열을 생성합니다. URL은 랜딩 슬러그가 아닌 기본 웹 사이트 주소만 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the v card value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>명함 프로필입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>UTF-8 vCard 3.0 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build v card operation.</para>
    /// \endif
    /// </returns>
    public string BuildVCard(CardProfile profile)
    {
        var name = profile.Name.Trim();

        var lines = new List<string>
        {
            "BEGIN:VCARD",
            "VERSION:3.0",
            "PRODID:-//CodeMaru//Codemaru//KO",
            $"N;CHARSET=UTF-8:{EscapeVCard(name)};;;;",
            $"FN;CHARSET=UTF-8:{EscapeVCard(name)}"
        };

        AddUtf8Value(lines, "ORG", profile.BackBrand);
        AddUtf8Value(lines, "TITLE", profile.Role);
        AddPlainValues(lines, "EMAIL;TYPE=INTERNET", profile.Email);

        if (profile.IncludePhoneInVCard)
        {
            AddPlainValues(lines, "TEL;TYPE=CELL,VOICE", profile.Phone);
        }

        if (profile.IncludeAddressInVCard && !string.IsNullOrWhiteSpace(profile.Address))
        {
            lines.Add($"ADR;TYPE=WORK;CHARSET=UTF-8:;;{EscapeVCard(profile.Address)};;;;");
        }

        AddPlainValue(lines, "URL", GetVCardContactUrl(profile));
        AddUtf8Value(lines, "NOTE", profile.VCardNote);
        AddPhoto(lines, profile.LogoImageDataUrl);
        lines.Add("END:VCARD");

        return string.Join("\r\n", lines.Select(FoldVCardLineByUtf8Bytes)) + "\r\n";
    }

    /// <summary>
    /// \if KO
    /// <para>Windows Contacts(wab.exe)용 vCard 2.1 문자열을 생성합니다. 한글 필드는 Windows Contacts 호환을 위해 CP949 quoted-printable로 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the windows v card value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>명함 프로필입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>ASCII quoted-printable 기반 vCard 2.1 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build windows v card operation.</para>
    /// \endif
    /// </returns>
    public string BuildWindowsVCard(CardProfile profile)
    {
        var name = profile.Name.Trim();

        var lines = new List<string>
        {
            "BEGIN:VCARD",
            "VERSION:2.1",
            $"N;CHARSET=ks_c_5601-1987;ENCODING=QUOTED-PRINTABLE:{ToKoreanQuotedPrintable(name)};;;;",
            $"FN;CHARSET=ks_c_5601-1987;ENCODING=QUOTED-PRINTABLE:{ToKoreanQuotedPrintable(name)}"
        };

        AddKoreanQuotedPrintableValue(lines, "ORG", profile.BackBrand);
        AddKoreanQuotedPrintableValue(lines, "TITLE", profile.Role);
        AddPlainValues(lines, "EMAIL;INTERNET", profile.Email);

        if (profile.IncludePhoneInVCard)
        {
            AddPlainValues(lines, "TEL;CELL;VOICE", profile.Phone);
        }

        if (profile.IncludeAddressInVCard && !string.IsNullOrWhiteSpace(profile.Address))
        {
            lines.Add($"ADR;WORK;CHARSET=ks_c_5601-1987;ENCODING=QUOTED-PRINTABLE:;;{ToKoreanQuotedPrintable(profile.Address)};;;;");
        }

        AddPlainValue(lines, "URL;WORK", GetVCardContactUrl(profile));
        AddKoreanQuotedPrintableValue(lines, "NOTE", profile.VCardNote);
        lines.Add("END:VCARD");

        return string.Join("\r\n", lines.Select(FoldQuotedPrintableVCardLine)) + "\r\n";
    }

    /// <summary>
    /// \if KO
    /// <para>Front Card Svg 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the front card svg value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="qrSvg">
    /// \if KO
    /// <para>qr Svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for qr svg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Front Card Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build front card svg operation.</para>
    /// \endif
    /// </returns>
    private static string BuildFrontCardSvg(CardProfile profile, string qrSvg)
    {
        const double scale = 10;
        var width = Math.Clamp(profile.CardWidthMm, 40, 140) * scale;
        var height = Math.Clamp(profile.CardHeightMm, 25, 90) * scale;
        var fontScale = scale / 5;

        // QR 원: 카드 크기에 비례하여 안전 영역 안에 배치
        // 카드 높이의 40%를 반지름 최대값으로 (작은 명함에서도 원이 잘리지 않음)
        // 우측·상하 stroke 여백 6px 확보
        var maxRadiusByHeight = (height / 2) - 18;
        var qrCircleRadius = Math.Min(maxRadiusByHeight, 112);
        var qrCircleCx = width - qrCircleRadius - 24;   // 우측 여백 = stroke 여유 + rx(14) 회피
        var qrCircleCy = height / 2;
        // QR 정사각형이 원 안에 완전히 들어가려면 대각선 ≤ 원의 지름
        // 한 변 ≤ r × √2 ≈ r × 1.414. 안전 마진 두고 r × 1.3 사용
        var qrSize = qrCircleRadius * 1.3;
        var qrX = qrCircleCx - qrSize / 2;
        var qrY = qrCircleCy - qrSize / 2;
        var qr = EmbedSvg(qrSvg, qrX, qrY, qrSize, qrSize);

        var categoryLines = SplitLines(profile.Category);
        var categoryText = SvgTextLines(categoryLines, 64, 305, profile.FrontCategoryFontSize * fontScale, 1.25, "font-weight=\"700\"");

        // rect는 stroke가 line center에 그려지므로 1px 안쪽으로 들여서 viewBox 잘림 방지
        return $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="{profile.CardWidthMm:0.##}mm" height="{profile.CardHeightMm:0.##}mm" viewBox="0 0 {width:0.##} {height:0.##}" preserveAspectRatio="xMidYMid meet">
          <rect x="1" y="1" width="{width - 2:0.##}" height="{height - 2:0.##}" rx="14" fill="#fbfaf7" stroke="#cdd5cb" stroke-width="1"/>
          <g font-family="{Svg(profile.FrontFontFamily)}, Malgun Gothic, sans-serif">
            <text x="64" y="150" fill="{Svg(profile.AccentColor)}" font-size="{profile.FrontBrandFontSize * fontScale:0.##}" font-weight="700">{Svg(profile.Brand)}</text>
            <text x="64" y="215" fill="#3f4841" font-size="{profile.FrontTaglineFontSize * fontScale:0.##}">{Svg(profile.Tagline)}</text>
            {categoryText}
            <text x="64" y="{height - 70:0.##}" fill="#4d574f" font-size="{profile.FrontEmailFontSize * fontScale:0.##}">{Svg(profile.Email)}</text>
          </g>
          <circle cx="{qrCircleCx:0.##}" cy="{qrCircleCy:0.##}" r="{qrCircleRadius:0.##}" fill="#ffffff" stroke="#202520" stroke-width="2"/>
          {qr}
        </svg>
        """;
    }

    /// <summary>
    /// \if KO
    /// <para>Imported Image Card Svg 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the imported image card svg value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <param name="imageDataUrl">
    /// \if KO
    /// <para>image Data Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for image data url.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Imported Image Card Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build imported image card svg operation.</para>
    /// \endif
    /// </returns>
    private static string BuildImportedImageCardSvg(CardProfile profile, string imageDataUrl)
    {
        const double scale = 10;
        var width = Math.Clamp(profile.CardWidthMm, 40, 140) * scale;
        var height = Math.Clamp(profile.CardHeightMm, 25, 90) * scale;

        return $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="{profile.CardWidthMm:0.##}mm" height="{profile.CardHeightMm:0.##}mm" viewBox="0 0 {width:0.##} {height:0.##}" preserveAspectRatio="xMidYMid meet">
          <rect x="1" y="1" width="{width - 2:0.##}" height="{height - 2:0.##}" rx="14" fill="#fbfaf7" stroke="#cdd5cb" stroke-width="1"/>
          <image href="{Svg(imageDataUrl)}" x="0" y="0" width="{width:0.##}" height="{height:0.##}" preserveAspectRatio="xMidYMid meet"/>
        </svg>
        """;
    }

    /// <summary>
    /// \if KO
    /// <para>Back Card Svg 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the back card svg value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Back Card Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build back card svg operation.</para>
    /// \endif
    /// </returns>
    private static string BuildBackCardSvg(CardProfile profile)
    {
        const double scale = 10;
        var width = Math.Clamp(profile.CardWidthMm, 40, 140) * scale;
        var height = Math.Clamp(profile.CardHeightMm, 25, 90) * scale;
        var fontScale = scale / 5;
        var logo = string.IsNullOrWhiteSpace(profile.LogoImageDataUrl)
            ? $"""<ellipse cx="{width / 2:0.##}" cy="155" rx="60" ry="34" fill="{Svg(profile.AccentColor)}"/>"""
            : $"""<image href="{Svg(profile.LogoImageDataUrl)}" x="{width / 2 - 108:0.##}" y="92" width="216" height="135" preserveAspectRatio="xMidYMid meet"/>""";

        return $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="{profile.CardWidthMm:0.##}mm" height="{profile.CardHeightMm:0.##}mm" viewBox="0 0 {width:0.##} {height:0.##}">
          <rect width="100%" height="100%" rx="14" fill="#fbfaf7" stroke="#cdd5cb"/>
          <rect x="{width / 2 - 126:0.##}" y="74" width="252" height="158" rx="34" fill="none" stroke="{Svg(profile.AccentColor)}" stroke-width="4"/>
          {logo}
          <g font-family="{Svg(profile.BackFontFamily)}, Malgun Gothic, sans-serif" text-anchor="middle">
            <text x="{width / 2:0.##}" y="290" fill="#172119" font-size="{profile.BackBrandFontSize * fontScale:0.##}" font-weight="700">{Svg(profile.BackBrand)}</text>
            <text x="{width / 2:0.##}" y="335" fill="#576158" font-size="{profile.BackTaglineFontSize * fontScale:0.##}">{Svg(profile.BackTagline)}</text>
            <text x="{width / 2:0.##}" y="405" fill="#172119" font-size="{profile.BackNameFontSize * fontScale:0.##}" font-weight="700">{Svg(profile.Name)}</text>
            <text x="{width / 2:0.##}" y="452" fill="#4d574f" font-size="{profile.BackRoleFontSize * fontScale:0.##}">{Svg(profile.Role)}</text>
          </g>
        </svg>
        """;
    }

    /// <summary>
    /// \if KO
    /// <para>Theme Palette 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the theme palette value.</para>
    /// \endif
    /// </summary>
    /// <param name="theme">
    /// \if KO
    /// <para>theme에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for theme.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Theme Palette 작업에서 생성한 <c>(string Background, string Panel, string Text, string Muted)</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>(string Background, string Panel, string Text, string Muted)</c> result produced by the get theme palette operation.</para>
    /// \endif
    /// </returns>
    private static (string Background, string Panel, string Text, string Muted) GetThemePalette(string theme)
    {
        return theme switch
        {
            "dark" => ("#101412", "#171d1a", "#f4f7f2", "#aeb9ae"),
            "blue" => ("#eef5fb", "#ffffff", "#122436", "#5a6c7d"),
            "green" => ("#eef5ee", "#ffffff", "#18261c", "#607064"),
            _ => ("#f7f8f5", "#ffffff", "#172119", "#5d685f")
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Display Value 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the display value operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Display Value 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the display value operation.</para>
    /// \endif
    /// </returns>
    private static string DisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    /// <summary>
    /// \if KO
    /// <para>Utf8 Value 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the utf8 value item.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>ICollection&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICollection&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    private static void AddUtf8Value(ICollection<string> lines, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        lines.Add($"{key};CHARSET=UTF-8:{EscapeVCard(value)}");
    }

    /// <summary>
    /// \if KO
    /// <para>Plain Value 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the plain value item.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>ICollection&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICollection&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    private static void AddPlainValue(ICollection<string> lines, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        lines.Add($"{key}:{EscapeVCard(value)}");
    }

    /// <summary>
    /// \if KO
    /// <para>Plain Values 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the plain values item.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>ICollection&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICollection&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    private static void AddPlainValues(ICollection<string> lines, string key, string value)
    {
        foreach (var item in SplitMultiValue(value))
        {
            AddPlainValue(lines, key, item);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Korean Quoted Printable Value 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the korean quoted printable value item.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>ICollection&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICollection&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    private static void AddKoreanQuotedPrintableValue(ICollection<string> lines, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        lines.Add($"{key};CHARSET=ks_c_5601-1987;ENCODING=QUOTED-PRINTABLE:{ToKoreanQuotedPrintable(value)}");
    }

    /// <summary>
    /// \if KO
    /// <para>V Card Contact Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the v card contact url value.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get V Card Contact Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get v card contact url operation.</para>
    /// \endif
    /// </returns>
    private static string GetVCardContactUrl(CardProfile profile)
    {
        return string.IsNullOrWhiteSpace(profile.Website)
            ? string.Empty
            : profile.Website.Trim().TrimEnd('/');
    }

    /// <summary>
    /// \if KO
    /// <para>To Korean Quoted Printable 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to korean quoted printable operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Korean Quoted Printable 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to korean quoted printable operation.</para>
    /// \endif
    /// </returns>
    private static string ToKoreanQuotedPrintable(string value)
    {
        return ToQuotedPrintable(value, GetKoreanEncoding());
    }

    /// <summary>
    /// \if KO
    /// <para>Korean Encoding 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the korean encoding value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Korean Encoding 작업에서 생성한 <c>Encoding</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Encoding</c> result produced by the get korean encoding operation.</para>
    /// \endif
    /// </returns>
    private static Encoding GetKoreanEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(949);
    }

    /// <summary>
    /// \if KO
    /// <para>To Quoted Printable 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to quoted printable operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="encoding">
    /// \if KO
    /// <para>encoding에 사용할 <c>Encoding</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Encoding</c> value used for encoding.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Quoted Printable 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to quoted printable operation.</para>
    /// \endif
    /// </returns>
    private static string ToQuotedPrintable(string value, Encoding encoding)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var bytes = encoding.GetBytes(normalized);
        var builder = new StringBuilder(bytes.Length * 3);

        foreach (var b in bytes)
        {
            if (b == 0x0A)
            {
                builder.Append("=0A");
            }
            else if ((b >= 33 && b <= 60) || (b >= 62 && b <= 126))
            {
                builder.Append((char)b);
            }
            else
            {
                builder.Append('=').Append(b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// \if KO
    /// <para>Photo 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the photo item.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>ICollection&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICollection&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    /// <param name="dataUrl">
    /// \if KO
    /// <para>data Url에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for data url.</para>
    /// \endif
    /// </param>
    private static void AddPhoto(ICollection<string> lines, string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !TryReadDataUrl(dataUrl, out var contentType, out var base64))
        {
            return;
        }

        var type = contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase)
                ? "JPEG"
                : "PNG";

        // base64 데이터에 공백/줄바꿈이 섞여 있으면 Windows People이 디코딩 중 크래시 발생.
        // 안전을 위해 모든 whitespace를 제거하고 단일 라인으로 출력한다.
        var cleanBase64 = new string(base64.Where(static c => !char.IsWhiteSpace(c)).ToArray());

        // PHOTO 라인 호환성 규칙:
        //   - ENCODING=BASE64 (vCard 2.1/3.0 양쪽 호환, Windows People이 인식하는 형식)
        //     ENCODING=b 는 RFC 2426 표준이지만 Windows People에서 인식 실패 → 앱 종료 원인
        //   - TYPE 먼저, ENCODING 나중 (Windows People 선호 순서)
        //   - line folding 적용하지 않음
        //     Windows의 일부 vCard 파서는 fold된 base64를 잘못 unfold하여 디코딩 실패함
        lines.Add($"PHOTO;TYPE={type};ENCODING=BASE64:{cleanBase64}");
    }

    /// <summary>
    /// \if KO
    /// <para>Read Data Url 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to read data url and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="dataUrl">
    /// \if KO
    /// <para>data Url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for data url.</para>
    /// \endif
    /// </param>
    /// <param name="contentType">
    /// \if KO
    /// <para>content Type에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content type.</para>
    /// \endif
    /// </param>
    /// <param name="base64">
    /// \if KO
    /// <para>base64에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for base64.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Read Data Url 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the try read data url condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool TryReadDataUrl(string dataUrl, out string contentType, out string base64)
    {
        contentType = string.Empty;
        base64 = string.Empty;

        const string marker = ";base64,";
        var markerIndex = dataUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (!dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || markerIndex < 0)
        {
            return false;
        }

        contentType = dataUrl[5..markerIndex];
        base64 = dataUrl[(markerIndex + marker.Length)..];
        return base64.Length > 0;
    }

    /// <summary>
    /// \if KO
    /// <para>Fold V Card Line By Utf8 Bytes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fold v card line by utf8 bytes operation.</para>
    /// \endif
    /// </summary>
    /// <param name="line">
    /// \if KO
    /// <para>line에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for line.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Fold V Card Line By Utf8 Bytes 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the fold v card line by utf8 bytes operation.</para>
    /// \endif
    /// </returns>
    private static string FoldVCardLineByUtf8Bytes(string line)
    {
        const int maxBytes = 75;
        var bytes = Encoding.UTF8.GetBytes(line);
        if (bytes.Length <= maxBytes)
        {
            return line;
        }

        var builder = new StringBuilder(line.Length + 16);
        var currentBytes = 0;

        foreach (var rune in line.EnumerateRunes())
        {
            var runeText = rune.ToString();
            var runeBytes = Encoding.UTF8.GetByteCount(runeText);
            if (currentBytes > 0 && currentBytes + runeBytes > maxBytes)
            {
                builder.Append("\r\n ");
                currentBytes = 1;
            }

            builder.Append(runeText);
            currentBytes += runeBytes;
        }

        return builder.ToString();
    }

    /// <summary>
    /// \if KO
    /// <para>Fold Quoted Printable V Card Line 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fold quoted printable v card line operation.</para>
    /// \endif
    /// </summary>
    /// <param name="line">
    /// \if KO
    /// <para>line에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for line.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Fold Quoted Printable V Card Line 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the fold quoted printable v card line operation.</para>
    /// \endif
    /// </returns>
    private static string FoldQuotedPrintableVCardLine(string line)
    {
        const int maxLength = 73;
        if (line.Length <= maxLength)
        {
            return line;
        }

        var builder = new StringBuilder(line.Length + 32);
        var index = 0;

        while (index < line.Length)
        {
            var take = Math.Min(maxLength, line.Length - index);
            while (take > 1 && line[index + take - 1] == '=')
            {
                take--;
            }

            if (take > 2 && line[index + take - 2] == '=')
            {
                take -= 2;
            }

            if (take <= 0)
            {
                take = Math.Min(maxLength, line.Length - index);
            }

            if (index > 0)
            {
                builder.Append("\r\n ");
            }

            builder.Append(line, index, take);
            index += take;
        }

        return builder.ToString();
    }

    /// <summary>
    /// \if KO
    /// <para>Html 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the html operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Html 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the html operation.</para>
    /// \endif
    /// </returns>
    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    /// <summary>
    /// \if KO
    /// <para>Svg 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the svg operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the svg operation.</para>
    /// \endif
    /// </returns>
    private static string Svg(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    /// <summary>
    /// \if KO
    /// <para>Css 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the css operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Css 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the css operation.</para>
    /// \endif
    /// </returns>
    private static string Css(string? value)
    {
        return (value ?? string.Empty).Replace("'", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// \if KO
    /// <para>Split Lines 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the split lines operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Split Lines 작업에서 생성한 <c>string[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> result produced by the split lines operation.</para>
    /// \endif
    /// </returns>
    private static string[] SplitLines(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None);
    }

    /// <summary>
    /// \if KO
    /// <para>Split Multi Value 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the split multi value operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Split Multi Value 작업에서 생성한 <c>IEnumerable&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> result produced by the split multi value operation.</para>
    /// \endif
    /// </returns>
    private static IEnumerable<string> SplitMultiValue(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split(['\n', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => !string.IsNullOrWhiteSpace(item));
    }

    /// <summary>
    /// \if KO
    /// <para>Svg Text Lines 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the svg text lines operation.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>IReadOnlyList&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    /// <param name="x">
    /// \if KO
    /// <para>x에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for x.</para>
    /// \endif
    /// </param>
    /// <param name="y">
    /// \if KO
    /// <para>y에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for y.</para>
    /// \endif
    /// </param>
    /// <param name="fontSize">
    /// \if KO
    /// <para>font Size에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for font size.</para>
    /// \endif
    /// </param>
    /// <param name="lineHeight">
    /// \if KO
    /// <para>line Height에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for line height.</para>
    /// \endif
    /// </param>
    /// <param name="attributes">
    /// \if KO
    /// <para>attributes에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for attributes.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Svg Text Lines 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the svg text lines operation.</para>
    /// \endif
    /// </returns>
    private static string SvgTextLines(IReadOnlyList<string> lines, double x, double y, double fontSize, double lineHeight, string attributes)
    {
        var builder = new StringBuilder();
        builder.Append($"""<text x="{x:0.##}" y="{y:0.##}" fill="#222a24" font-size="{fontSize:0.##}" {attributes}>""");

        for (var i = 0; i < lines.Count; i++)
        {
            var dy = i == 0 ? "0" : $"{fontSize * lineHeight:0.##}";
            builder.Append($"""<tspan x="{x:0.##}" dy="{dy}">{Svg(lines[i])}</tspan>""");
        }

        builder.Append("</text>");
        return builder.ToString();
    }

    /// <summary>
    /// \if KO
    /// <para>Embed Svg 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the embed svg operation.</para>
    /// \endif
    /// </summary>
    /// <param name="svg">
    /// \if KO
    /// <para>svg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for svg.</para>
    /// \endif
    /// </param>
    /// <param name="x">
    /// \if KO
    /// <para>x에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for x.</para>
    /// \endif
    /// </param>
    /// <param name="y">
    /// \if KO
    /// <para>y에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for y.</para>
    /// \endif
    /// </param>
    /// <param name="width">
    /// \if KO
    /// <para>width에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for width.</para>
    /// \endif
    /// </param>
    /// <param name="height">
    /// \if KO
    /// <para>height에 사용할 <c>double</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> value used for height.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Embed Svg 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the embed svg operation.</para>
    /// \endif
    /// </returns>
    private static string EmbedSvg(string svg, double x, double y, double width, double height)
    {
        try
        {
            var element = XElement.Parse(svg);
            element.SetAttributeValue("x", x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            element.SetAttributeValue("y", y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            element.SetAttributeValue("width", width.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            element.SetAttributeValue("height", height.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            element.SetAttributeValue("preserveAspectRatio", "xMidYMid meet");
            element.SetAttributeValue("class", null);
            return element.ToString(SaveOptions.DisableFormatting);
        }
        catch
        {
            var dataUrl = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
            return $"""<image href="{dataUrl}" x="{x:0.##}" y="{y:0.##}" width="{width:0.##}" height="{height:0.##}" preserveAspectRatio="xMidYMid meet"/>""";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Escape V Card 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the escape v card operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Escape V Card 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the escape v card operation.</para>
    /// \endif
    /// </returns>
    private static string EscapeVCard(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
