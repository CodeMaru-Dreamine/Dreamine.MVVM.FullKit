using System.Diagnostics;
using Codemaru.Models;
using Codemaru.States;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>\brief WPF 프로세스 전역에서 공유하는 CardHybrid 상태 뷰입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card hybrid session functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>실제 사용자별 편집 상태는 <see cref="CardHybridCircuitSession"/> (Blazor Circuit 별) 가 관리합니다. 이 싱글턴은 WPF 메인 창의 상태 요약 표시용 뷰이며 인증을 처리하지 않습니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class CardHybridSession
{
    /// <summary>
    /// \if KO
    /// <para>sync 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the sync value.</para>
    /// \endif
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// \if KO
    /// <para>qr 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the qr value.</para>
    /// \endif
    /// </summary>
    private readonly IQrSvgGenerator _qr;

    /// <summary>
    /// \if KO
    /// <para>state 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the state value.</para>
    /// \endif
    /// </summary>
    private CardHybridState? _state;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="CardHybridSession"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CardHybridSession"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="qr">
    /// \if KO
    /// <para>qr에 사용할 <c>IQrSvgGenerator</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IQrSvgGenerator</c> value used for qr.</para>
    /// \endif
    /// </param>
    /// <param name="_">
    /// \if KO
    /// <para>에 사용할 <c>ICardProfileStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICardProfileStore</c> value used for .</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public CardHybridSession(IQrSvgGenerator qr, ICardProfileStore _)
    {
        _qr = qr ?? throw new ArgumentNullException(nameof(qr));
    }

    /// <summary>
    /// \if KO
    /// <para>State 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state value.</para>
    /// \endif
    /// </summary>
    public CardHybridState State =>
        _state ?? CardHybridState.CreateDefault(_qr.CreateSvg(CardProfile.Default.LandingUrl));

    /// <summary>
    /// \if KO
    /// <para>\brief WPF 표시용 참조 사용자입니다. 현재는 항상 <see cref="CardHybridUser.Guest"/>.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current user value.</para>
    /// \endif
    /// </summary>
    public CardHybridUser CurrentUser => CardHybridUser.Guest;

    /// <summary>
    /// \if KO
    /// <para>State Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when state changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler<CardHybridState>? StateChanged;

    /// <summary>
    /// \if KO
    /// <para>\brief 시작 시 기본 상태를 준비합니다. App.OnStartup 에서 1회 호출합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Initialize Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the initialize async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Notify State Changed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the notify state changed operation.</para>
    /// \endif
    /// </summary>
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
