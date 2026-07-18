using Dreamine.UI.Blazor;
using Xunit;

namespace Dreamine.UI.Blazor.Tests;

/// <summary>
/// \if KO
/// <para>Blazor 대화 상자 서비스의 요청 상태, 완료 및 자동 선택 동작을 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Verifies request state, completion, and automatic-selection behavior of the Blazor dialog service.</para>
/// \endif
/// </summary>
public class DreamineDialogServiceTests
{
    /// <summary>
    /// \if KO
    /// <para>서비스 생성 직후 현재 요청이 없는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a newly created service has no current request.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Current_InitiallyNull()
    {
        var svc = new DreamineDialogService();
        Assert.Null(svc.Current);
    }

    /// <summary>
    /// \if KO
    /// <para>메시지 상자 표시가 현재 요청을 설정하고 완료 결과를 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that showing a message box sets the current request and returns its completion result.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ShowMessageBoxAsync_SetsCurrent()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowMessageBoxAsync("테스트 메시지");

        Assert.NotNull(svc.Current);
        Assert.Equal("테스트 메시지", svc.Current!.Message);
        Assert.Equal("Information", svc.Current.Title);

        svc.Complete(DreamineDialogResult.OK);
        var result = await task;
        Assert.Equal(DreamineDialogResult.OK, result);
    }

    /// <summary>
    /// \if KO
    /// <para>사용자 지정 제목이 현재 메시지 상자 요청에 반영되는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a custom title is applied to the current message-box request.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ShowMessageBoxAsync_CustomTitle_IsSet()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowMessageBoxAsync("메시지", "사용자 제목");

        Assert.Equal("사용자 제목", svc.Current!.Title);

        svc.Complete(DreamineDialogResult.OK);
        await task;
    }

    /// <summary>
    /// \if KO
    /// <para>확인/취소 대화 상자가 두 버튼을 구성하고 취소 결과를 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that an OK/Cancel dialog configures two buttons and returns the Cancel result.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ShowOkCancelAsync_HasTwoButtons()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowOkCancelAsync("확인/취소?");

        Assert.Equal(2, svc.Current!.Buttons.Count);

        svc.Complete(DreamineDialogResult.Cancel);
        var result = await task;
        Assert.Equal(DreamineDialogResult.Cancel, result);
    }

    /// <summary>
    /// \if KO
    /// <para>예/아니요 대화 상자가 두 버튼을 구성하고 예 결과를 반환하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a Yes/No dialog configures two buttons and returns the Yes result.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ShowYesNoAsync_HasTwoButtons()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowYesNoAsync("예/아니오?");

        Assert.Equal(2, svc.Current!.Buttons.Count);

        svc.Complete(DreamineDialogResult.Yes);
        var result = await task;
        Assert.Equal(DreamineDialogResult.Yes, result);
    }

    /// <summary>
    /// \if KO
    /// <para>완료 호출이 현재 요청을 지우고 대기 중인 작업을 지정한 결과로 완료하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that completion clears the current request and resolves the pending task with the specified result.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task Complete_ClearsCurrentAndResolvesTask()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowMessageBoxAsync("msg");

        svc.Complete(DreamineDialogResult.OK);

        Assert.Null(svc.Current);
        Assert.Equal(DreamineDialogResult.OK, await task);
    }

    /// <summary>
    /// \if KO
    /// <para>대화 상자를 표시할 때 요청 변경 이벤트가 발생하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that showing a dialog raises the request-changed event.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task RequestChanged_FiredOnShow()
    {
        var svc = new DreamineDialogService();
        int callCount = 0;
        svc.RequestChanged += () => callCount++;

        var task = svc.ShowMessageBoxAsync("msg");
        Assert.Equal(1, callCount);

        svc.Complete(DreamineDialogResult.OK);
        await task;
    }

    /// <summary>
    /// \if KO
    /// <para>대화 상자를 완료할 때 요청 변경 이벤트가 다시 발생하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that completing a dialog raises the request-changed event again.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task RequestChanged_FiredOnComplete()
    {
        var svc = new DreamineDialogService();
        int callCount = 0;
        svc.RequestChanged += () => callCount++;

        var task = svc.ShowMessageBoxAsync("msg");
        svc.Complete(DreamineDialogResult.OK);
        await task;

        Assert.Equal(2, callCount); // 1=Show, 2=Complete
    }

    /// <summary>
    /// \if KO
    /// <para>확인 텍스트만 있는 깜빡임 요청이 하나의 버튼과 깜빡임 상태를 구성하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a blinking request with only OK text configures one button and blinking state.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ShowBlinkAsync_OkTextOnly_HasOneButton()
    {
        var svc = new DreamineDialogService();
        var opts = new BlinkPopupOptions { Title = "경고", Message = "확인하세요", OkText = "확인" };
        var task = svc.ShowBlinkAsync(opts);

        Assert.Single(svc.Current!.Buttons);
        Assert.True(svc.Current.UseBlink);

        svc.Complete(DreamineDialogResult.OK);
        await task;
    }

    /// <summary>
    /// \if KO
    /// <para>확인 및 취소 텍스트가 있는 깜빡임 요청이 두 버튼을 구성하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that a blinking request with OK and Cancel text configures two buttons.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task ShowBlinkAsync_OkAndCancelText_HasTwoButtons()
    {
        var svc = new DreamineDialogService();
        var opts = new BlinkPopupOptions { OkText = "확인", CancelText = "취소" };
        var task = svc.ShowBlinkAsync(opts);

        Assert.Equal(2, svc.Current!.Buttons.Count);

        svc.Complete(DreamineDialogResult.Cancel);
        await task;
    }

    /// <summary>
    /// \if KO
    /// <para>자동 선택 틱이 카운트다운을 줄이고 만료 시 요청을 완료하는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that automatic-selection ticks decrement the countdown and complete the request at expiration.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>비동기 검증 작업입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task representing the asynchronous verification.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task TickAutoClick_CountsDownAndCompletes()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowMessageBoxAsync("msg", autoClickDelaySeconds: 2);

        Assert.Equal(2, svc.Current!.AutoClickRemainingSeconds);

        svc.TickAutoClick(); // 1 남음
        Assert.Equal(1, svc.Current!.AutoClickRemainingSeconds);

        svc.TickAutoClick(); // 0 → 자동 Complete
        var result = await task;
        Assert.Equal(DreamineDialogResult.OK, result);
        Assert.Null(svc.Current);
    }

    /// <summary>
    /// \if KO
    /// <para>현재 요청이 없을 때 자동 선택 틱이 예외를 발생시키지 않는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that an automatic-selection tick does not throw when no request is current.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void TickAutoClick_WhenCurrentIsNull_DoesNotThrow()
    {
        var svc = new DreamineDialogService();
        var ex = Record.Exception(() => svc.TickAutoClick());
        Assert.Null(ex);
    }

    /// <summary>
    /// \if KO
    /// <para>대기 중인 요청이 없을 때 완료 호출이 예외를 발생시키지 않는지 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Verifies that completion does not throw when no request is pending.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Complete_WhenNoPendingRequest_DoesNotThrow()
    {
        var svc = new DreamineDialogService();
        var ex = Record.Exception(() => svc.Complete(DreamineDialogResult.OK));
        Assert.Null(ex);
    }
}
