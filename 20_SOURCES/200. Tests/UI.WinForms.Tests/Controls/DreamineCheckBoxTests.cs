using System.Runtime.ExceptionServices;
using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

/// <summary>
/// \if KO
/// <para>Dreamine 체크 상자의 상태, 이벤트 및 콘텐츠 속성을 STA 환경에서 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies Dreamine check-box state, events, and content properties in an STA environment.</para>
/// \endif
/// </summary>
public class DreamineCheckBoxTests
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
    /// <para>체크 상태 기본값이 거짓인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that checked state defaults to false.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_DefaultIsFalse()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox();
            Assert.False(cb.IsChecked);
        });

    /// <summary>
    /// \if KO
    /// <para>체크 상태를 참으로 설정할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that checked state can be set to true.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_SetToTrue_PropertyChanges()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox { IsChecked = true };
            Assert.True(cb.IsChecked);
        });

    /// <summary>
    /// \if KO
    /// <para>체크 상태를 두 번 변경하면 변경 이벤트가 두 번 발생하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that two checked-state changes raise two change events.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_Toggle_RaisesCheckedChanged()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox();
            int fired = 0;
            cb.CheckedChanged += (_, _) => fired++;
            cb.IsChecked = true;
            cb.IsChecked = false;
            Assert.Equal(2, fired);
        });

    /// <summary>
    /// \if KO
    /// <para>같은 체크 상태를 다시 설정하면 변경 이벤트가 발생하지 않는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that assigning the same checked state does not raise a change event.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsChecked_SameValue_DoesNotRaiseEvent()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox { IsChecked = false };
            int fired = 0;
            cb.CheckedChanged += (_, _) => fired++;
            cb.IsChecked = false;
            Assert.Equal(0, fired);
        });

    /// <summary>
    /// \if KO
    /// <para>콘텐츠 설정 및 조회가 같은 값을 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior of content.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Content_SetAndGet_Roundtrip()
        => Sta(() =>
        {
            var cb = new DreamineCheckBox { Content = "Accept terms" };
            Assert.Equal("Accept terms", cb.Content);
        });
}
