using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

/// <summary>
/// \if KO
/// <para>Dreamine 확장 컨트롤의 기본 상태, 높이, 이벤트와 콘텐츠를 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies Dreamine expander defaults, height, events, and content.</para>
/// \endif
/// </summary>
public class DreamineExpanderTests
{
    /// <summary>
    /// \if KO
    /// <para>확장 컨트롤이 기본적으로 펼쳐져 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the expander is expanded by default.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsExpanded_DefaultIsTrue()
    {
        var exp = new DreamineExpander();
        Assert.True(exp.IsExpanded);
    }

    /// <summary>
    /// \if KO
    /// <para>접을 때 컨트롤 높이가 줄어드는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that collapsing reduces the control height.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsExpanded_SetFalse_CollapsesHeight()
    {
        var exp = new DreamineExpander();
        int expandedH = exp.Height;
        exp.IsExpanded = false;
        Assert.True(exp.Height < expandedH);
    }

    /// <summary>
    /// \if KO
    /// <para>두 번의 확장 상태 변경이 두 번의 이벤트를 발생시키는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that two expansion-state changes raise two events.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsExpanded_RaisesExpandedChanged()
    {
        var exp = new DreamineExpander();
        int fired = 0;
        exp.ExpandedChanged += (_, _) => fired++;
        exp.IsExpanded = false;
        exp.IsExpanded = true;
        Assert.Equal(2, fired);
    }

    /// <summary>
    /// \if KO
    /// <para>머리글 설정 및 조회가 같은 값을 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior of the header.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Header_SetAndGet_Roundtrip()
    {
        var exp = new DreamineExpander { Header = "Section A" };
        Assert.Equal("Section A", exp.Header);
    }

    /// <summary>
    /// \if KO
    /// <para>내부 콘텐츠 패널이 생성되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the inner content panel is created.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Content_IsNotNull()
    {
        var exp = new DreamineExpander();
        Assert.NotNull(exp.Content);
    }

    /// <summary>
    /// \if KO
    /// <para>접었다 다시 펼치면 원래 높이가 복원되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that collapsing and re-expanding restores the original height.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Collapse_ThenExpand_RestoresHeight()
    {
        var exp = new DreamineExpander();
        int original = exp.Height;
        exp.IsExpanded = false;
        exp.IsExpanded = true;
        Assert.Equal(original, exp.Height);
    }
}
