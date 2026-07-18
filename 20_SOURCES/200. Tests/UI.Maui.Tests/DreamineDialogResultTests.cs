using Dreamine.UI.Maui.Popup;
using Xunit;

namespace Dreamine.UI.Maui.Tests;

/// <summary>
/// \if KO
/// <para>MAUI 대화 상자 결과 열거형의 값과 기본값을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies values and the default value of the MAUI dialog-result enumeration.</para>
/// \endif
/// </summary>
public class DreamineDialogResultTests
{
    /// <summary>
    /// \if KO
    /// <para>각 대화 상자 결과가 열거형에 정의되어 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that each dialog result is defined by the enumeration.</para>
    /// \endif
    /// </summary>
    /// <param name="result">
    /// \if KO
    /// <para>검증할 대화 상자 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The dialog result to verify.</para>
    /// \endif
    /// </param>
    [Theory]
    [InlineData(DreamineDialogResult.None)]
    [InlineData(DreamineDialogResult.OK)]
    [InlineData(DreamineDialogResult.Cancel)]
    [InlineData(DreamineDialogResult.Yes)]
    [InlineData(DreamineDialogResult.No)]
    public void AllResultValues_AreDefined(DreamineDialogResult result)
    {
        Assert.True(Enum.IsDefined(typeof(DreamineDialogResult), result));
    }

    /// <summary>
    /// \if KO
    /// <para>열거형에 다섯 개의 결과 값이 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the enumeration contains five result values.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void EnumHasFiveValues()
    {
        Assert.Equal(5, Enum.GetValues<DreamineDialogResult>().Length);
    }

    /// <summary>
    /// \if KO
    /// <para>기본 결과 값이 <see cref="DreamineDialogResult.None"/>인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default result is <see cref="DreamineDialogResult.None"/>.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void None_IsDefault()
    {
        var result = default(DreamineDialogResult);
        Assert.Equal(DreamineDialogResult.None, result);
    }
}
