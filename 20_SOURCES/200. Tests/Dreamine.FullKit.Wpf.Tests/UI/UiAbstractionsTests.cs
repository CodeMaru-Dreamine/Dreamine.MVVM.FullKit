using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

public sealed class UiAbstractionsTests
{
    [Fact]
    public void ELanguageCode_ContainsExpectedValues()
    {
        var values = Enum.GetValues<LanguageCode>();
        Assert.Contains(LanguageCode.en_US, values);
        Assert.Contains(LanguageCode.ko_KR, values);
    }

    [Fact]
    public void EVkLayout_ContainsNumericAndText()
    {
        var values = Enum.GetValues<VkLayout>();
        Assert.Contains(VkLayout.Numeric, values);
        Assert.Contains(VkLayout.Text, values);
    }
}
