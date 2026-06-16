using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace FamiliesApp.Services;

/// <summary>
/// 앱 시작 시 wwwroot/img/og-platform.png 를 자동 생성한다.
/// </summary>
public static class OgImageGenerator
{
    public static void EnsureGenerated(string wwwrootPath)
    {
        var imgDir = Path.Combine(wwwrootPath, "img");
        var outPath = Path.Combine(imgDir, "og-platform.png");

        if (File.Exists(outPath)) return;

        Directory.CreateDirectory(imgDir);

        const int W = 1200, H = 630;
        using var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        // ── 배경 그라데이션 (웜 오렌지) ──────────────────────────────
        using (var bgBrush = new LinearGradientBrush(
            new Point(0, 0), new Point(W, H),
            Color.FromArgb(255, 147, 51),   // #ff9333
            Color.FromArgb(251, 191, 36)))  // #fbbf24
        {
            g.FillRectangle(bgBrush, 0, 0, W, H);
        }

        // ── 장식 원 (반투명 흰색) ─────────────────────────────────────
        DrawCircle(g, -100, -100, 380, Color.FromArgb(30, 255, 255, 255));
        DrawCircle(g, 900,  380,  420, Color.FromArgb(20, 255, 255, 255));
        DrawCircle(g, 980,  -80,  180, Color.FromArgb(25, 255, 255, 255));
        DrawCircle(g, 40,   430,  220, Color.FromArgb(20, 255, 255, 255));
        DrawCircle(g, 500,  -60,  140, Color.FromArgb(15, 255, 255, 255));

        // ── 카드 (흰색 반투명) ────────────────────────────────────────
        var cardRect = new RectangleF(160, 90, 880, 450);
        DrawRoundedRect(g, cardRect, 28,
            Color.FromArgb(210, 255, 255, 255),
            Color.FromArgb(60, 255, 255, 255), 2f);

        // ── 이모지 ───────────────────────────────────────────────────
        using var iconFont = new Font("Segoe UI Emoji", 56, FontStyle.Regular, GraphicsUnit.Pixel);
        DrawCenteredText(g, "👨‍👩‍👧‍👦", iconFont, Color.FromArgb(60, 40, 20), W / 2f, 198);

        // ── 메인 타이틀 ───────────────────────────────────────────────
        using var titleFont = new Font("맑은 고딕", 50, FontStyle.Bold, GraphicsUnit.Pixel);
        DrawCenteredText(g, "무료 가족 앨범", titleFont, Color.FromArgb(45, 28, 8), W / 2f, 295);

        // ── 서브타이틀 ────────────────────────────────────────────────
        using var subFont = new Font("맑은 고딕", 22, FontStyle.Regular, GraphicsUnit.Pixel);
        DrawCenteredText(g, "사진 · 동영상 · 이야기 · 반응 · 댓글", subFont,
            Color.FromArgb(90, 60, 20), W / 2f, 360);

        // ── 구분선 ────────────────────────────────────────────────────
        using var linePen = new Pen(Color.FromArgb(80, 255, 200, 100), 1.5f);
        g.DrawLine(linePen, 400, 394, 800, 394);

        // ── 배지 ─────────────────────────────────────────────────────
        var badgeRect = new RectangleF(440, 410, 320, 44);
        using var badgeBrush = new SolidBrush(Color.FromArgb(220, 249, 115, 22));
        FillRoundedRect(g, badgeBrush, badgeRect, 22);
        using var badgeFont = new Font("맑은 고딕", 18, FontStyle.Bold, GraphicsUnit.Pixel);
        DrawCenteredText(g, "5분이면 완성 · 완전 무료", badgeFont, Color.White, W / 2f, 438);

        // ── 도메인 ────────────────────────────────────────────────────
        using var domFont = new Font("Arial", 18, FontStyle.Regular, GraphicsUnit.Pixel);
        DrawCenteredText(g, "families.codemaru.co.kr", domFont,
            Color.FromArgb(100, 45, 28, 8), W / 2f, 503);

        // ── PNG 저장 ──────────────────────────────────────────────────
        bmp.Save(outPath, ImageFormat.Png);
    }

    private static void DrawCenteredText(Graphics g, string text, Font font, Color color, float cx, float cy)
    {
        using var brush = new SolidBrush(color);
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, cx - size.Width / 2f, cy - size.Height / 2f);
    }

    private static void DrawCircle(Graphics g, float x, float y, float diameter, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x, y, diameter, diameter);
    }

    private static void DrawRoundedRect(Graphics g, RectangleF rect, float radius,
        Color fill, Color stroke, float strokeWidth)
    {
        using var path = RoundedRectPath(rect, radius);
        using var fillBrush = new SolidBrush(fill);
        g.FillPath(fillBrush, path);
        using var pen = new Pen(stroke, strokeWidth);
        g.DrawPath(pen, path);
    }

    private static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedRectPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static GraphicsPath RoundedRectPath(RectangleF rect, float r)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
        path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
        path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}
