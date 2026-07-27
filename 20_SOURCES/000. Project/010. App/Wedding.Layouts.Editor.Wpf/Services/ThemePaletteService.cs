using System.Globalization;
using Wedding.Layouts.Contracts;

namespace Wedding.Layouts.Editor.Wpf.Services;

public static class ThemePaletteService
{
    private const double MinimumMutedTextContrastRatio = 4.5;

    public static IReadOnlyList<LayoutStyleTokenValue> CreateFromPrimary(
        string primaryHex)
    {
        var primary = Rgb.Parse(primaryHex);
        var background = primary.Mix(Rgb.White, .94);
        var surface = primary.Mix(Rgb.White, .985);
        var text = primary.Mix(Rgb.Black, .78);
        var mutedText = EnsureMinimumContrast(
            text.Mix(background, .42),
            background,
            text,
            MinimumMutedTextContrastRatio);
        var border = primary.Mix(Rgb.White, .75);
        var buttonText =
            primary.ContrastRatio(Rgb.Black) >=
            primary.ContrastRatio(Rgb.White)
                ? Rgb.Black
                : Rgb.White;

        return
        [
            Token(LayoutStyleToken.PrimaryColor, primary),
            Token(LayoutStyleToken.SecondaryColor, primary.Mix(Rgb.White, .32)),
            Token(LayoutStyleToken.AccentColor, primary.Mix(Rgb.Black, .12)),
            Token(LayoutStyleToken.BackgroundColor, background),
            Token(LayoutStyleToken.SurfaceColor, surface),
            Token(LayoutStyleToken.TextColor, text),
            Token(LayoutStyleToken.MutedTextColor, mutedText),
            Token(LayoutStyleToken.BorderColor, border),
            Token(LayoutStyleToken.ButtonBackgroundColor, primary),
            Token(LayoutStyleToken.ButtonTextColor, buttonText),
            Token(LayoutStyleToken.NavigationBackgroundColor, surface),
            Token(LayoutStyleToken.NavigationTextColor, text),
        ];
    }

    private static Rgb EnsureMinimumContrast(
        Rgb preferred,
        Rgb background,
        Rgb fallback,
        double minimumRatio)
    {
        if (preferred.ContrastRatio(background) >= minimumRatio)
        {
            return preferred;
        }

        // Move only as far toward the already accessible body text as needed.
        // A byte-sized search keeps the generated palette deterministic and
        // preserves more of the muted character than replacing it with black.
        for (var step = 1; step <= byte.MaxValue; step++)
        {
            var candidate = preferred.Mix(fallback, step / (double)byte.MaxValue);
            if (candidate.ContrastRatio(background) >= minimumRatio)
            {
                return candidate;
            }
        }

        // The generated body text is deliberately dark, but retain a hard
        // guarantee if a future palette formula changes that assumption.
        return fallback.ContrastRatio(background) >= minimumRatio
            ? fallback
            : Rgb.Black;
    }

    private static LayoutStyleTokenValue Token(
        LayoutStyleToken token,
        Rgb color) =>
        new()
        {
            Token = token,
            Value = color.ToHex(),
        };

    private readonly record struct Rgb(byte Red, byte Green, byte Blue)
    {
        public static Rgb Black { get; } = new(18, 20, 24);

        public static Rgb White { get; } = new(255, 255, 255);

        public double RelativeLuminance
        {
            get
            {
                static double Linear(byte component)
                {
                    var value = component / 255d;
                    return value <= .04045
                        ? value / 12.92
                        : Math.Pow((value + .055) / 1.055, 2.4);
                }

                return .2126 * Linear(Red)
                    + .7152 * Linear(Green)
                    + .0722 * Linear(Blue);
            }
        }

        public static Rgb Parse(string hex)
        {
            var value = hex.Trim().TrimStart('#');
            if (value.Length != 6
                || !byte.TryParse(
                    value[..2],
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var red)
                || !byte.TryParse(
                    value.Substring(2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var green)
                || !byte.TryParse(
                    value.Substring(4, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var blue))
            {
                throw new FormatException("대표 색상은 #RRGGBB 형식이어야 합니다.");
            }

            return new Rgb(red, green, blue);
        }

        public Rgb Mix(Rgb other, double amount)
        {
            static byte Blend(byte left, byte right, double ratio) =>
                (byte)Math.Clamp(
                    Math.Round(left + (right - left) * ratio),
                    byte.MinValue,
                    byte.MaxValue);

            return new Rgb(
                Blend(Red, other.Red, amount),
                Blend(Green, other.Green, amount),
                Blend(Blue, other.Blue, amount));
        }

        public double ContrastRatio(Rgb other)
        {
            var lighter = Math.Max(RelativeLuminance, other.RelativeLuminance);
            var darker = Math.Min(RelativeLuminance, other.RelativeLuminance);
            return (lighter + .05) / (darker + .05);
        }

        public string ToHex() => $"#{Red:X2}{Green:X2}{Blue:X2}";
    }
}
