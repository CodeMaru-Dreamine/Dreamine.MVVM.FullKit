using Dreamine.UI.Blazor;
using Xunit;

namespace Dreamine.UI.Blazor.Tests;

public class DreamineDialogServiceTests
{
    [Fact]
    public void Current_InitiallyNull()
    {
        var svc = new DreamineDialogService();
        Assert.Null(svc.Current);
    }

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

    [Fact]
    public async Task ShowMessageBoxAsync_CustomTitle_IsSet()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowMessageBoxAsync("메시지", "사용자 제목");

        Assert.Equal("사용자 제목", svc.Current!.Title);

        svc.Complete(DreamineDialogResult.OK);
        await task;
    }

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

    [Fact]
    public async Task Complete_ClearsCurrentAndResolvesTask()
    {
        var svc = new DreamineDialogService();
        var task = svc.ShowMessageBoxAsync("msg");

        svc.Complete(DreamineDialogResult.OK);

        Assert.Null(svc.Current);
        Assert.Equal(DreamineDialogResult.OK, await task);
    }

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

    [Fact]
    public void TickAutoClick_WhenCurrentIsNull_DoesNotThrow()
    {
        var svc = new DreamineDialogService();
        var ex = Record.Exception(() => svc.TickAutoClick());
        Assert.Null(ex);
    }

    [Fact]
    public void Complete_WhenNoPendingRequest_DoesNotThrow()
    {
        var svc = new DreamineDialogService();
        var ex = Record.Exception(() => svc.Complete(DreamineDialogResult.OK));
        Assert.Null(ex);
    }
}
