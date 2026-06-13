using System.Windows.Media;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Abstractions.VirtualKeyboard;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

public sealed class PopupOptionsTests
{
    // ── BlinkPopupOptions ──────────────────────────────────────────────────

    [Fact]
    public void BlinkPopupOptions_DefaultValues_AreReasonable()
    {
        var opt = new BlinkPopupOptions { Message = "Test message" };

        Assert.Equal("Test message", opt.Message);
        Assert.True(opt.BlinkIntervalMs > 0);
        Assert.True(opt.Opacity1 > 0);
        Assert.True(opt.Opacity2 > 0);
    }

    [Fact]
    public void BlinkPopupOptions_IsModal_DefaultTrue()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.IsModal);
    }

    // ── KeyboardLayoutSelectorConverter ───────────────────────────────────

    [Fact]
    public void KeyboardLayoutSelector_NumericLayout_ReturnsNumPadTemplate()
    {
        var conv = new KeyboardLayoutSelectorConverter();
        var numPad = new System.Windows.DataTemplate();
        var defaultTpl = new System.Windows.DataTemplate();
        conv.NumPadTemplate = numPad;
        conv.DefaultTemplate = defaultTpl;

        var result = conv.Convert(VkLayout.Numeric, typeof(System.Windows.DataTemplate), null!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Same(numPad, result);
    }

    [Fact]
    public void KeyboardLayoutSelector_TextLayout_ReturnsDefaultTemplate()
    {
        var conv = new KeyboardLayoutSelectorConverter();
        var defaultTpl = new System.Windows.DataTemplate();
        conv.DefaultTemplate = defaultTpl;

        var result = conv.Convert(VkLayout.Text, typeof(System.Windows.DataTemplate), null!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Same(defaultTpl, result);
    }

    [Fact]
    public void KeyboardLayoutSelector_NullInput_ReturnsDefaultTemplate()
    {
        var conv = new KeyboardLayoutSelectorConverter();
        var defaultTpl = new System.Windows.DataTemplate();
        conv.DefaultTemplate = defaultTpl;

        var result = conv.Convert(null, typeof(System.Windows.DataTemplate), null!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Same(defaultTpl, result);
    }
}
