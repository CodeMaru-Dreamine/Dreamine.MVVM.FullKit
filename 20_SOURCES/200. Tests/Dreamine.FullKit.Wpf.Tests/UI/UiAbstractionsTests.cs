using Dreamine.UI.Abstractions.VirtualKeyboard;

namespace Dreamine.FullKit.Wpf.Tests.UI;

/// <summary>
/// \if KO
/// <para>Ui Abstractions Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates ui abstractions tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class UiAbstractionsTests
{
    /// <summary>
    /// \if KO
    /// <para>E Language Code Contains Expected Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the e language code contains expected values operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ELanguageCode_ContainsExpectedValues()
    {
        var values = Enum.GetValues<LanguageCode>();
        Assert.Contains(LanguageCode.en_US, values);
        Assert.Contains(LanguageCode.ko_KR, values);
    }

    /// <summary>
    /// \if KO
    /// <para>E Vk Layout Contains Numeric And Text 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the e vk layout contains numeric and text operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void EVkLayout_ContainsNumericAndText()
    {
        var values = Enum.GetValues<VkLayout>();
        Assert.Contains(VkLayout.Numeric, values);
        Assert.Contains(VkLayout.Text, values);
    }
}
