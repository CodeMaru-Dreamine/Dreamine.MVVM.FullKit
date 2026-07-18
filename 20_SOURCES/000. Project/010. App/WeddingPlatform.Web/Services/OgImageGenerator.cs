using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>앱 시작 시 wwwroot/img/og-platform.jpg 를 자동 생성한다. System.Drawing (net8.0-windows) 사용.</para>
/// \endif
/// \if EN
/// <para>Encapsulates og image generator functionality and related state.</para>
/// \endif
/// </summary>
public static class OgImageGenerator
{
    /// <summary>
    /// \if KO
    /// <para>Ensure Generated 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure generated operation.</para>
    /// \endif
    /// </summary>
    /// <param name="wwwrootPath">
    /// \if KO
    /// <para>wwwroot Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for wwwroot path.</para>
    /// \endif
    /// </param>
    public static void EnsureGenerated(string wwwrootPath)
    {
        var imgDir = Path.Combine(wwwrootPath, "img");
        var outPath = Path.Combine(imgDir, "og-platform.jpg");

        if (File.Exists(outPath)) return;

        Directory.CreateDirectory(imgDir);

        const int W = 1200, H = 630;
        using var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        // ── 배경 그라데이션 ───────────────────────────────────────
        using (var bgBrush = new LinearGradientBrush(
            new Point(0, 0), new Point(W, H),
            Color.FromArgb(253, 246, 238),
            Color.FromArgb(238, 220, 200)))
        {
            g.FillRectangle(bgBrush, 0, 0, W, H);
        }

        // ── 장식 원 ───────────────────────────────────────────────
        DrawCircle(g, -80, -80, 320, 12);
        DrawCircle(g, 920, 350, 400, 10);
        DrawCircle(g, 970, -60, 160, 8);
        DrawCircle(g, 50, 420, 200, 8);

        // ── 카드 ─────────────────────────────────────────────────
        var cardRect = new RectangleF(200, 100, 800, 430);
        DrawRoundedRect(g, cardRect, 24,
            Color.FromArgb(220, 255, 255, 255),
            Color.FromArgb(80, 200, 168, 130), 2);

        // ── 이모지 대신 하트 아이콘 (텍스트) ─────────────────────
        using var iconFont = new Font("Segoe UI Emoji", 48, FontStyle.Regular, GraphicsUnit.Pixel);
        DrawCenteredText(g, "💒", iconFont, Color.FromArgb(58, 46, 40), W / 2f, 200);

        // ── 메인 타이틀 ───────────────────────────────────────────
        using var titleFont = new Font("맑은 고딕", 46, FontStyle.Bold, GraphicsUnit.Pixel);
        DrawCenteredText(g, "무료 모바일 청첩장", titleFont, Color.FromArgb(58, 46, 40), W / 2f, 285);

        // ── 서브타이틀 ────────────────────────────────────────────
        using var subFont = new Font("맑은 고딕", 22, FontStyle.Regular, GraphicsUnit.Pixel);
        DrawCenteredText(g, "사진 · 음악 · 지도 · 방명록 · 계좌 안내", subFont,
            Color.FromArgb(122, 106, 94), W / 2f, 348);

        // ── 구분선 ────────────────────────────────────────────────
        using var linePen = new Pen(Color.FromArgb(128, 200, 168, 130), 1.5f);
        g.DrawLine(linePen, 420, 382, 780, 382);

        // ── 배지 ─────────────────────────────────────────────────
        var badgeRect = new RectangleF(470, 398, 260, 42);
        using var badgeBrush = new SolidBrush(Color.FromArgb(200, 168, 130));
        FillRoundedRect(g, badgeBrush, badgeRect, 21);
        using var badgeFont = new Font("맑은 고딕", 17, FontStyle.Bold, GraphicsUnit.Pixel);
        DrawCenteredText(g, "5분이면 완성 · 완전 무료", badgeFont, Color.White, W / 2f, 425);

        // ── 도메인 ────────────────────────────────────────────────
        using var domFont = new Font("Arial", 19, FontStyle.Regular, GraphicsUnit.Pixel);
        DrawCenteredText(g, "wedding.codemaru.co.kr", domFont,
            Color.FromArgb(100, 58, 46, 40), W / 2f, 492);

        // ── JPEG 저장 ─────────────────────────────────────────────
        var jpegParams = new EncoderParameters(1);
        jpegParams.Param[0] = new EncoderParameter(Encoder.Quality, 92L);
        var jpegCodec = ImageCodecInfo.GetImageEncoders()
            .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

        bmp.Save(outPath, jpegCodec, jpegParams);
    }

    /// <summary>
    /// \if KO
    /// <para>Draw Centered Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the draw centered text operation.</para>
    /// \endif
    /// </summary>
    /// <param name="g">
    /// \if KO
    /// <para>g에 사용할 <c>Graphics</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Graphics</c> value used for g.</para>
    /// \endif
    /// </param>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="font">
    /// \if KO
    /// <para>font에 사용할 <c>Font</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Font</c> value used for font.</para>
    /// \endif
    /// </param>
    /// <param name="color">
    /// \if KO
    /// <para>color에 사용할 <c>Color</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Color</c> value used for color.</para>
    /// \endif
    /// </param>
    /// <param name="cx">
    /// \if KO
    /// <para>cx에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for cx.</para>
    /// \endif
    /// </param>
    /// <param name="cy">
    /// \if KO
    /// <para>cy에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for cy.</para>
    /// \endif
    /// </param>
    private static void DrawCenteredText(Graphics g, string text, Font font, Color color, float cx, float cy)
    {
        using var brush = new SolidBrush(color);
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, cx - size.Width / 2f, cy - size.Height / 2f);
    }

    /// <summary>
    /// \if KO
    /// <para>Draw Circle 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the draw circle operation.</para>
    /// \endif
    /// </summary>
    /// <param name="g">
    /// \if KO
    /// <para>g에 사용할 <c>Graphics</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Graphics</c> value used for g.</para>
    /// \endif
    /// </param>
    /// <param name="x">
    /// \if KO
    /// <para>x에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for x.</para>
    /// \endif
    /// </param>
    /// <param name="y">
    /// \if KO
    /// <para>y에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for y.</para>
    /// \endif
    /// </param>
    /// <param name="diameter">
    /// \if KO
    /// <para>diameter에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for diameter.</para>
    /// \endif
    /// </param>
    /// <param name="alphaPercent">
    /// \if KO
    /// <para>alpha Percent에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for alpha percent.</para>
    /// \endif
    /// </param>
    private static void DrawCircle(Graphics g, float x, float y, float diameter, int alphaPercent)
    {
        int alpha = (int)(255 * alphaPercent / 100.0);
        using var brush = new SolidBrush(Color.FromArgb(alpha, 200, 168, 130));
        g.FillEllipse(brush, x, y, diameter, diameter);
    }

    /// <summary>
    /// \if KO
    /// <para>Draw Rounded Rect 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the draw rounded rect operation.</para>
    /// \endif
    /// </summary>
    /// <param name="g">
    /// \if KO
    /// <para>g에 사용할 <c>Graphics</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Graphics</c> value used for g.</para>
    /// \endif
    /// </param>
    /// <param name="rect">
    /// \if KO
    /// <para>rect에 사용할 <c>RectangleF</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>RectangleF</c> value used for rect.</para>
    /// \endif
    /// </param>
    /// <param name="radius">
    /// \if KO
    /// <para>radius에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for radius.</para>
    /// \endif
    /// </param>
    /// <param name="fill">
    /// \if KO
    /// <para>fill에 사용할 <c>Color</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Color</c> value used for fill.</para>
    /// \endif
    /// </param>
    /// <param name="stroke">
    /// \if KO
    /// <para>stroke에 사용할 <c>Color</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Color</c> value used for stroke.</para>
    /// \endif
    /// </param>
    /// <param name="strokeWidth">
    /// \if KO
    /// <para>stroke Width에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for stroke width.</para>
    /// \endif
    /// </param>
    private static void DrawRoundedRect(Graphics g, RectangleF rect, float radius,
        Color fill, Color stroke, float strokeWidth)
    {
        using var path = RoundedRectPath(rect, radius);
        using var fillBrush = new SolidBrush(fill);
        g.FillPath(fillBrush, path);
        using var pen = new Pen(stroke, strokeWidth);
        g.DrawPath(pen, path);
    }

    /// <summary>
    /// \if KO
    /// <para>Fill Rounded Rect 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fill rounded rect operation.</para>
    /// \endif
    /// </summary>
    /// <param name="g">
    /// \if KO
    /// <para>g에 사용할 <c>Graphics</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Graphics</c> value used for g.</para>
    /// \endif
    /// </param>
    /// <param name="brush">
    /// \if KO
    /// <para>brush에 사용할 <c>Brush</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Brush</c> value used for brush.</para>
    /// \endif
    /// </param>
    /// <param name="rect">
    /// \if KO
    /// <para>rect에 사용할 <c>RectangleF</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>RectangleF</c> value used for rect.</para>
    /// \endif
    /// </param>
    /// <param name="radius">
    /// \if KO
    /// <para>radius에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for radius.</para>
    /// \endif
    /// </param>
    private static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedRectPath(rect, radius);
        g.FillPath(brush, path);
    }

    /// <summary>
    /// \if KO
    /// <para>Rounded Rect Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the rounded rect path operation.</para>
    /// \endif
    /// </summary>
    /// <param name="rect">
    /// \if KO
    /// <para>rect에 사용할 <c>RectangleF</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>RectangleF</c> value used for rect.</para>
    /// \endif
    /// </param>
    /// <param name="r">
    /// \if KO
    /// <para>r에 사용할 <c>float</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>float</c> value used for r.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Rounded Rect Path 작업에서 생성한 <c>GraphicsPath</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>GraphicsPath</c> result produced by the rounded rect path operation.</para>
    /// \endif
    /// </returns>
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
