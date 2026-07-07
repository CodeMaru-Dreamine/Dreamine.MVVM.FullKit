using System.Diagnostics;
using Codemaru.Models;
using Codemaru.States;

namespace Codemaru.Services;

/// <summary>
/// \brief WPF 프로세스 전역에서 공유하는 CardHybrid 상태 뷰입니다.
/// </summary>
/// <remarks>
/// 실제 사용자별 편집 상태는 <see cref="CardHybridCircuitSession"/> (Blazor Circuit 별)
/// 가 관리합니다. 이 싱글턴은 WPF 메인 창의 상태 요약 표시용 뷰이며 인증을 처리하지 않습니다.
/// </remarks>
public sealed class CardHybridSession
{
    private readonly object _sync = new();
    private readonly IQrSvgGenerator _qr;

    private CardHybridState? _state;

    public CardHybridSession(IQrSvgGenerator qr, ICardProfileStore _)
    {
        _qr = qr ?? throw new ArgumentNullException(nameof(qr));
    }

    public CardHybridState State =>
        _state ?? CardHybridState.CreateDefault(_qr.CreateSvg(CardProfile.Default.LandingUrl));

    /// <summary>
    /// \brief WPF 표시용 참조 사용자입니다. 현재는 항상 <see cref="CardHybridUser.Guest"/>.
    /// </summary>
    public CardHybridUser CurrentUser => CardHybridUser.Guest;

    public event EventHandler<CardHybridState>? StateChanged;

    /// <summary>
    /// \brief 시작 시 기본 상태를 준비합니다. App.OnStartup 에서 1회 호출합니다.
    /// </summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_state is not null)
            {
                return Task.CompletedTask;
            }

            _state = CardHybridState.CreateDefault(_qr.CreateSvg(CardProfile.Default.LandingUrl));
        }

        NotifyStateChanged();
        return Task.CompletedTask;
    }

    private void NotifyStateChanged()
    {
        var handlers = StateChanged?.GetInvocationList();
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                ((EventHandler<CardHybridState>)handler).Invoke(this, State);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CardHybrid state handler failed: {ex}");
            }
        }
    }
}
