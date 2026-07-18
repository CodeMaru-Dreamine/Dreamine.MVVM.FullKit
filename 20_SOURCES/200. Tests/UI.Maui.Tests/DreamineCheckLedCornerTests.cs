using Dreamine.UI.Maui;
using Xunit;

namespace Dreamine.UI.Maui.Tests;

/// <summary>
/// \if KO
/// <para>MAUI 체크 LED 모서리 열거형의 정의와 멤버 수를 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies definitions and member count of the MAUI check-LED corner enumeration.</para>
/// \endif
/// </summary>
public class DreamineCheckLedCornerTests
{
    /// <summary>
    /// \if KO
    /// <para>각 모서리 값이 열거형에 정의되어 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that each corner value is defined by the enumeration.</para>
    /// \endif
    /// </summary>
    /// <param name="corner">
    /// \if KO
    /// <para>검증할 모서리 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The corner value to verify.</para>
    /// \endif
    /// </param>
    [Theory]
    [InlineData(DreamineCheckLedCorner.TopLeft)]
    [InlineData(DreamineCheckLedCorner.TopRight)]
    [InlineData(DreamineCheckLedCorner.BottomLeft)]
    [InlineData(DreamineCheckLedCorner.BottomRight)]
    public void AllCornerValues_AreDefined(DreamineCheckLedCorner corner)
    {
        Assert.True(Enum.IsDefined(typeof(DreamineCheckLedCorner), corner));
    }

    /// <summary>
    /// \if KO
    /// <para>열거형에 네 개의 모서리 값이 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the enumeration contains four corner values.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void EnumHasFourValues()
    {
        Assert.Equal(4, Enum.GetValues<DreamineCheckLedCorner>().Length);
    }
}
