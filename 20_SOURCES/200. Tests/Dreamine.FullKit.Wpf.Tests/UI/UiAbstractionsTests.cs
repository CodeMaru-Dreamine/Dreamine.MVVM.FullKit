using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

public sealed class UiAbstractionsTests
{
    [Fact]
    public void ELanguageCode_ContainsExpectedValues()
    {
        var values = Enum.GetValues<eLanguageCode>();
        Assert.Contains(eLanguageCode.en_US, values);
        Assert.Contains(eLanguageCode.ko_KR, values);
    }

    [Fact]
    public void EVkLayout_ContainsNumericAndText()
    {
        var values = Enum.GetValues<eVkLayout>();
        Assert.Contains(eVkLayout.Numeric, values);
        Assert.Contains(eVkLayout.Text, values);
    }
}
