using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

/// <summary>
/// \if KO
/// <para>Dreamine 라디오 버튼의 상태, 이벤트 및 그룹 상호 배제를 STA 환경에서 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies Dreamine radio-button state, events, and group mutual exclusion in an STA environment.</para>
/// \endif
/// </summary>
public class DreamineRadioButtonTests
{
    /// <summary>
    /// \if KO
    /// <para>지정한 테스트 동작을 STA 스레드에서 실행하고 발생한 예외를 호출 스레드에 다시 던집니다.</para>
    /// \endif
    /// \if EN
    /// <para>Executes a test action on an STA thread and rethrows any exception on the calling thread.</para>
    /// \endif
    /// </summary>
    /// <param name="action">
    /// \if KO
    /// <para>STA 스레드에서 실행할 테스트 동작입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The test action to execute on the STA thread.</para>
    /// \endif
    /// </param>
    private static void Sta(Action action)
    {
        Exception? ex = null;
        var t = new Thread(() => { try { action(); } catch (Exception e) { ex = e; } });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (ex is not null) ExceptionDispatchInfo.Capture(ex).Throw();
    }

    /// <summary>
    /// \if KO
    /// <para>선택 상태 기본값이 거짓인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selected state defaults to false.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_DefaultIsFalse()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            Assert.False(rb.IsChecked);
        });

    /// <summary>
    /// \if KO
    /// <para>선택 상태를 참으로 설정할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selected state can be set to true.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_SetToTrue_PropertyChanges()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton { IsChecked = true };
            Assert.True(rb.IsChecked);
        });

    /// <summary>
    /// \if KO
    /// <para>선택 시 변경 이벤트가 한 번 발생하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selection raises one change event.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_SetTrue_RaisesCheckedChanged()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            int fired = 0;
            rb.CheckedChanged += (_, _) => fired++;
            rb.IsChecked = true;
            Assert.Equal(1, fired);
        });

    /// <summary>
    /// \if KO
    /// <para>그룹 이름 기본값이 빈 문자열인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the group name defaults to an empty string.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void GroupName_DefaultIsEmpty()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            Assert.Equal(string.Empty, rb.GroupName);
        });

    /// <summary>
    /// \if KO
    /// <para>그룹 이름 설정 및 조회가 같은 값을 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior of the group name.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void GroupName_SetAndGet_Roundtrip()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton { GroupName = "group1" };
            Assert.Equal("group1", rb.GroupName);
        });

    /// <summary>
    /// \if KO
    /// <para>같은 그룹의 두 번째 항목을 선택하면 첫 번째 항목이 해제되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selecting a second item in the same group clears the first item.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void SameGroup_SelectingOne_UnchecksOther()
        => Sta(() =>
        {
            using var container = new Panel();
            var rb1 = new DreamineRadioButton { GroupName = "g", IsChecked = true };
            var rb2 = new DreamineRadioButton { GroupName = "g" };
            container.Controls.Add(rb1);
            container.Controls.Add(rb2);
            rb2.IsChecked = true;
            Assert.False(rb1.IsChecked);
            Assert.True(rb2.IsChecked);
        });

    /// <summary>
    /// \if KO
    /// <para>서로 다른 그룹의 항목 선택이 서로 영향을 주지 않는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selection in different groups does not affect other items.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DifferentGroups_SelectingOne_DoesNotAffectOther()
        => Sta(() =>
        {
            using var container = new Panel();
            var rb1 = new DreamineRadioButton { GroupName = "g1", IsChecked = true };
            var rb2 = new DreamineRadioButton { GroupName = "g2" };
            container.Controls.Add(rb1);
            container.Controls.Add(rb2);
            rb2.IsChecked = true;
            Assert.True(rb1.IsChecked);
            Assert.True(rb2.IsChecked);
        });

    /// <summary>
    /// \if KO
    /// <para>부모가 없는 라디오 버튼을 선택해도 예외가 발생하지 않는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selecting a parentless radio button does not throw.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void NoParent_SettingChecked_DoesNotThrow()
        => Sta(() =>
        {
            var rb = new DreamineRadioButton();
            var ex = Record.Exception(() => rb.IsChecked = true);
            Assert.Null(ex);
        });
}
