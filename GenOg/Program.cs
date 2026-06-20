using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

const int W = 1200, H = 630;
using var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
using var g = Graphics.FromImage(bmp);
g.SmoothingMode = SmoothingMode.AntiAlias;
g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

// 배경 그라데이션 (다크 인디고 → 퍼플)
using (var bg = new LinearGradientBrush(new Point(0,0), new Point(W,H),
    Color.FromArgb(15,23,42), Color.FromArgb(30,27,75)))
    g.FillRectangle(bg, 0, 0, W, H);

// 장식 원들 (반투명 인디고)
void Circle(float x, float y, float d, int a) {
    using var b = new SolidBrush(Color.FromArgb(a,99,102,241));
    g.FillEllipse(b, x, y, d, d);
}
Circle(-80,-80,380,40); Circle(920,360,420,25);
Circle(1000,-60,200,30); Circle(50,440,200,20); Circle(520,-50,150,20);

// 둥근 사각형 경로
GraphicsPath RRect(float x, float y, float w, float h, float r) {
    var p = new GraphicsPath();
    p.AddArc(x, y, r*2, r*2, 180, 90);
    p.AddArc(x+w-r*2, y, r*2, r*2, 270, 90);
    p.AddArc(x+w-r*2, y+h-r*2, r*2, r*2, 0, 90);
    p.AddArc(x, y+h-r*2, r*2, r*2, 90, 90);
    p.CloseFigure();
    return p;
}

// 카드 (반투명 다크)
using (var path = RRect(140,80,920,470,28)) {
    using var fb = new SolidBrush(Color.FromArgb(180,30,41,59));
    g.FillPath(fb, path);
    using var pen = new Pen(Color.FromArgb(80,99,102,241), 1.5f);
    g.DrawPath(pen, path);
}

// 텍스트 중앙 정렬
void CText(string text, string family, float size, FontStyle fs, Color color, float cx, float cy) {
    using var font = new Font(family, size, fs, GraphicsUnit.Pixel);
    using var brush = new SolidBrush(color);
    var sz = g.MeasureString(text, font);
    g.DrawString(text, font, brush, cx - sz.Width/2f, cy - sz.Height/2f);
}

// 이모지
CText("🧑‍💻", "Segoe UI Emoji", 60, FontStyle.Regular, Color.FromArgb(220,220,230), W/2f, 195);

// 메인 타이틀
CText("무료 개발자 포트폴리오", "맑은 고딕", 50, FontStyle.Bold, Color.FromArgb(241,245,249), W/2f, 295);

// 서브타이틀
CText("프로젝트 · 이력서 · 기술스택 · 연락처", "맑은 고딕", 22, FontStyle.Regular, Color.FromArgb(148,163,184), W/2f, 362);

// 구분선
using (var pen = new Pen(Color.FromArgb(60,99,102,241), 1.5f))
    g.DrawLine(pen, 380, 398, 820, 398);

// 배지 배경 (인디고)
using (var path = RRect(420,412,360,44,22)) {
    using var fb = new SolidBrush(Color.FromArgb(220,99,102,241));
    g.FillPath(fb, path);
}
CText("5분이면 완성 · 완전 무료", "맑은 고딕", 18, FontStyle.Bold, Color.White, W/2f, 438);

// 도메인
CText("portfolio.codemaru.co.kr", "Arial", 18, FontStyle.Regular, Color.FromArgb(100,148,163,184), W/2f, 505);

var outDir = @"D:\Work\Dreamine.MVVM.FullKit\20_SOURCES\000. Project\010. App\Portfolio.Web\wwwroot\img";
Directory.CreateDirectory(outDir);
var outPath = Path.Combine(outDir, "og-platform.png");
bmp.Save(outPath, ImageFormat.Png);
Console.WriteLine("Saved: " + outPath);
