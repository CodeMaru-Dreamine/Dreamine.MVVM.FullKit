using System.Drawing;
using System.Windows.Input;
using Dreamine.UI.WinForms.Controls;
using Xunit;

namespace Dreamine.UI.WinForms.Tests.Controls;

/// <summary>
/// \if KO
/// <para>Dreamine 버튼의 기본값, 속성 왕복 및 명령 연결을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies Dreamine button defaults, property round trips, and command integration.</para>
/// \endif
/// </summary>
public class DreamineButtonTests
{
    /// <summary>
    /// \if KO
    /// <para>콘텐츠 기본값이 빈 문자열인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that content defaults to an empty string.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DefaultContent_IsEmpty()
    {
        var btn = new DreamineButton();
        Assert.Equal(string.Empty, btn.Content);
    }

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
    {
        var btn = new DreamineButton { Content = "Click Me" };
        Assert.Equal("Click Me", btn.Content);
    }

    /// <summary>
    /// \if KO
    /// <para>광택 색상의 기본값이 빈 색상인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the shine color defaults to an empty color.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ShineColor_DefaultIsEmpty()
    {
        var btn = new DreamineButton();
        Assert.Equal(Color.Empty, btn.ShineColor);
    }

    /// <summary>
    /// \if KO
    /// <para>광택 색상 설정 및 조회가 같은 값을 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies round-trip set-and-get behavior of the shine color.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ShineColor_SetAndGet_Roundtrip()
    {
        var btn = new DreamineButton { ShineColor = Color.Blue };
        Assert.Equal(Color.Blue, btn.ShineColor);
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
    public void IsSelected_DefaultIsFalse()
    {
        var btn = new DreamineButton();
        Assert.False(btn.IsSelected);
    }

    /// <summary>
    /// \if KO
    /// <para>선택 상태를 양방향으로 전환할 수 있는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that selected state can be toggled in both directions.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void IsSelected_ToggleWorks()
    {
        var btn = new DreamineButton { IsSelected = true };
        Assert.True(btn.IsSelected);
        btn.IsSelected = false;
        Assert.False(btn.IsSelected);
    }

    /// <summary>
    /// \if KO
    /// <para>기본 모서리 반지름이 양수인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default corner radius is positive.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CornerRadius_DefaultIsPositive()
    {
        var btn = new DreamineButton();
        Assert.True(btn.CornerRadius > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>연결한 명령을 실행하면 대상 동작이 호출되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that executing an associated command invokes its target action.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Command_Execute_InvokesAction()
    {
        bool executed = false;
        var btn = new DreamineButton
        {
            Command = new RelayTestCommand(_ => executed = true)
        };
        btn.Command.Execute(null);
        Assert.True(executed);
    }

    /// <summary>
    /// \if KO
    /// <para>명령 매개변수 설정 및 조회가 같은 객체를 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that command-parameter set and get return the same object.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void CommandParameter_SetAndGet_Roundtrip()
    {
        var param = new object();
        var btn = new DreamineButton { CommandParameter = param };
        Assert.Same(param, btn.CommandParameter);
    }

    /// <summary>
    /// \if KO
    /// <para>기본 버튼 크기가 양수인지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that the default button dimensions are positive.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DefaultSize_IsReasonable()
    {
        var btn = new DreamineButton();
        Assert.True(btn.Width > 0 && btn.Height > 0);
    }

    /// <summary>
    /// \if KO
    /// <para>테스트 동작을 <see cref="ICommand"/>으로 노출하는 최소 명령 구현입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Provides a minimal command implementation that exposes a test action through <see cref="ICommand"/>.</para>
    /// \endif
    /// </summary>
    /// <param name="execute">
    /// \if KO
    /// <para>명령 실행 시 호출할 동작입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The action invoked when the command executes.</para>
    /// \endif
    /// </param>
    private sealed class RelayTestCommand(Action<object?> execute) : ICommand
    {
        /// <summary>
        /// \if KO
        /// <para>테스트 명령에서는 사용하지 않는 실행 가능 상태 변경 이벤트입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Represents the executability-change event, unused by this test command.</para>
        /// \endif
        /// </summary>
        event EventHandler? ICommand.CanExecuteChanged { add { } remove { } }
        /// <summary>
        /// \if KO
        /// <para>테스트 명령이 항상 실행 가능한지 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether the test command can always execute.</para>
        /// \endif
        /// </summary>
        /// <param name="p">
        /// \if KO
        /// <para>무시되는 명령 매개변수입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The ignored command parameter.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>항상 <see langword="true"/>입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Always <see langword="true"/>.</para>
        /// \endif
        /// </returns>
        public bool CanExecute(object? p) => true;
        /// <summary>
        /// \if KO
        /// <para>저장된 테스트 동작에 매개변수를 전달하여 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Executes the stored test action with the supplied parameter.</para>
        /// \endif
        /// </summary>
        /// <param name="p">
        /// \if KO
        /// <para>테스트 동작에 전달할 매개변수입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The parameter passed to the test action.</para>
        /// \endif
        /// </param>
        public void Execute(object? p) => execute(p);
    }
}
