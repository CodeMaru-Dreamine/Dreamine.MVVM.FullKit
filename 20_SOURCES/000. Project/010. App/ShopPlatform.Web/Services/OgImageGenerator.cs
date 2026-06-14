using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ShopPlatform.Services;

/// <summary>
/// 샵별 OG 미리보기 이미지(1200×630 PNG)를 동적으로 생성합니다.
/// 이미 파일이 있으면 건너뜁니다.
/// </summary>
public static class OgImageGenerator
{
    private const int W = 1200;
    private const int H = 630;

    /// <summary>기본 플랫폼 OG 이미지 생성 (shop-og-default.png).</summary>
    public static void EnsureDefault(string wwwrootPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return; // Linux는 건너뜀
        var dir  = Path.Combine(wwwrootPath, "img");
        var path = Path.Combine(dir, "shop-og-default.png");
        Directory.CreateDirectory(dir);
        if (File.Exists(path)) return;

        using var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // 그라데이션 배경 (진한 남색 → 보라)
        using var bg = new LinearGradientBrush(
            new Rectangle(0, 0, W, H),
            Color.FromArgb(0x1e, 0x1b, 0x4b),   // indigo-950
            Color.FromArgb(0x4c, 0x1d, 0x95),   // violet-900
            LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(bg, 0, 0, W, H);

        // 장식 원 (반투명)
        using var circleBrush = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
        g.FillEllipse(circleBrush, -120, -120, 480, 480);
        g.FillEllipse(circleBrush, W - 300, H - 300, 500, 500);

        // 쇼핑백 아이콘 영역 (흰 둥근 사각형)
        var iconRect = new Rectangle(W / 2 - 60, 140, 120, 120);
        using var iconBg = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
        DrawRoundRect(g, iconBg, iconRect, 24);
        using var iconFont = new Font("Segoe UI Emoji", 52, FontStyle.Regular, GraphicsUnit.Pixel);
        using var iconBrush = new SolidBrush(Color.White);
        var iconSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("🛒", iconFont, iconBrush, new RectangleF(iconRect.X, iconRect.Y, iconRect.Width, iconRect.Height), iconSf);

        // 메인 타이틀
        using var titleFont = new Font("Malgun Gothic", 62, FontStyle.Bold, GraphicsUnit.Pixel);
        using var titleBrush = new SolidBrush(Color.White);
        var titleSf = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("ShopPlatform", titleFont, titleBrush, new RectangleF(0, 295, W, 80), titleSf);

        // 부제목
        using var subFont = new Font("Malgun Gothic", 30, FontStyle.Regular, GraphicsUnit.Pixel);
        using var subBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        var subSf = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("나만의 온라인 쇼핑몰을 시작하세요", subFont, subBrush, new RectangleF(0, 378, W, 50), subSf);

        // 하단 URL 배지
        using var badgeBrush = new SolidBrush(Color.FromArgb(50, 255, 255, 255));
        DrawRoundRect(g, badgeBrush, new Rectangle(W / 2 - 200, 470, 400, 46), 23);
        using var urlFont = new Font("Malgun Gothic", 22, FontStyle.Regular, GraphicsUnit.Pixel);
        using var urlBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
        var urlSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("shop.codemaru.co.kr", urlFont, urlBrush, new RectangleF(W / 2 - 200, 470, 400, 46), urlSf);

        bmp.Save(path, ImageFormat.Png);
    }

    /// <summary>샵별 OG 이미지 생성 (og-{slug}.png) — 샵 이름/설명 포함.</summary>
    public static string EnsureShopOg(string wwwrootPath, string slug, string shopName, string description, string baseUrl)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "/img/shop-og-default.png"; // Linux fallback
        var dir      = Path.Combine(wwwrootPath, "img", "og");
        var fileName = $"og-{slug}.png";
        var path     = Path.Combine(dir, fileName);
        Directory.CreateDirectory(dir);

        // 항상 재생성 (설정 변경 반영)
        using var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // 배경
        using var bg = new LinearGradientBrush(
            new Rectangle(0, 0, W, H),
            Color.FromArgb(0x1e, 0x1b, 0x4b),
            Color.FromArgb(0x4c, 0x1d, 0x95),
            LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(bg, 0, 0, W, H);

        // 장식
        using var circleBrush = new SolidBrush(Color.FromArgb(25, 255, 255, 255));
        g.FillEllipse(circleBrush, -100, -100, 400, 400);
        g.FillEllipse(circleBrush, W - 250, H - 250, 450, 450);

        // 샵 이름
        var nameText = shopName.Length > 18 ? shopName[..18] + "…" : shopName;
        using var nameFont = new Font("Malgun Gothic", 72, FontStyle.Bold, GraphicsUnit.Pixel);
        using var nameBrush = new SolidBrush(Color.White);
        var centerSf = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString(nameText, nameFont, nameBrush, new RectangleF(60, 200, W - 120, 90), centerSf);

        // 설명
        if (!string.IsNullOrEmpty(description))
        {
            var descText = description.Length > 40 ? description[..40] + "…" : description;
            using var descFont = new Font("Malgun Gothic", 32, FontStyle.Regular, GraphicsUnit.Pixel);
            using var descBrush = new SolidBrush(Color.FromArgb(210, 255, 255, 255));
            g.DrawString(descText, descFont, descBrush, new RectangleF(60, 305, W - 120, 55), centerSf);
        }

        // 구분선
        using var linePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1);
        g.DrawLine(linePen, 100, 390, W - 100, 390);

        // 하단 플랫폼 표시
        using var platformFont = new Font("Malgun Gothic", 24, FontStyle.Regular, GraphicsUnit.Pixel);
        using var platformBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255));
        g.DrawString($"🛒  {baseUrl.Replace("https://", "").Replace("http://", "")}/{slug}",
            platformFont, platformBrush, new RectangleF(60, 415, W - 120, 40), centerSf);

        bmp.Save(path, ImageFormat.Png);
        return $"/img/og/{fileName}";
    }

    private static void DrawRoundRect(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
