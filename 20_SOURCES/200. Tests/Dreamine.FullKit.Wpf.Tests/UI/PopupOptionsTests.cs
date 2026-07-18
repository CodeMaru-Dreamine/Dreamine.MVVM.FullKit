using System.Windows.Media;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Abstractions.VirtualKeyboard;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

/// <summary>
/// \if KO
/// <para>Popup Options Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates popup options tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PopupOptionsTests
{
    // ── BlinkPopupOptions ──────────────────────────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Blink Popup Options Default Values Are Reasonable 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the blink popup options default values are reasonable operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void BlinkPopupOptions_DefaultValues_AreReasonable()
    {
        var opt = new BlinkPopupOptions { Message = "Test message" };

        Assert.Equal("Test message", opt.Message);
        Assert.True(opt.BlinkIntervalMs > 0);
        Assert.True(opt.Opacity1 > 0);
        Assert.True(opt.Opacity2 > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>Blink Popup Options Is Modal Default True 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the blink popup options is modal default true operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void BlinkPopupOptions_IsModal_DefaultTrue()
    {
        var opt = new BlinkPopupOptions();
        Assert.True(opt.IsModal);
    }

    // ── KeyboardLayoutSelectorConverter ───────────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Keyboard Layout Selector Numeric Layout Returns Num Pad Template 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the keyboard layout selector numeric layout returns num pad template operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Keyboard Layout Selector Text Layout Returns Default Template 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the keyboard layout selector text layout returns default template operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void KeyboardLayoutSelector_TextLayout_ReturnsDefaultTemplate()
    {
        var conv = new KeyboardLayoutSelectorConverter();
        var defaultTpl = new System.Windows.DataTemplate();
        conv.DefaultTemplate = defaultTpl;

        var result = conv.Convert(VkLayout.Text, typeof(System.Windows.DataTemplate), null!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Same(defaultTpl, result);
    }

    /// <summary>
    /// \if KO
    /// <para>Keyboard Layout Selector Null Input Returns Default Template 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the keyboard layout selector null input returns default template operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void KeyboardLayoutSelector_NullInput_ReturnsDefaultTemplate()
    {
        var conv = new KeyboardLayoutSelectorConverter();
        var defaultTpl = new System.Windows.DataTemplate();
        conv.DefaultTemplate = defaultTpl;

        var result = conv.Convert(null!, typeof(System.Windows.DataTemplate), null!, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Same(defaultTpl, result);
    }
}
