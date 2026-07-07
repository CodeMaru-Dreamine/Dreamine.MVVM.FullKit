using System.Diagnostics;
using System.Security.Claims;
using Codemaru.Models;
using Codemaru.States;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace Codemaru.Services;

/// <summary>
/// \brief 웹 방문자 1회 접속(Blazor Circuit) 단위로 격리되는 CardHybrid 세션입니다.
/// </summary>
/// <remarks>
/// 인증 자체는 상위 Dreamine.Identity 계층이 처리하고,
/// 이 세션은 <see cref="ApplyUserAsync"/> 로 결과를 반영합니다.
/// Guest 상태에서 편집한 내역은 <see cref="HasGuestChanges"/> 로 추적하며
/// 로그인 시 <see cref="PromoteGuestChangesAsync"/> 로 사용자 소유로 이관할 수 있습니다.
/// </remarks>
public sealed class CardHybridCircuitSession : IDisposable
{
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly List<CardHistoryEntry> _history = new();
    private readonly IQrSvgGenerator _qr;
    private readonly ICardProfileStore _store;
    private readonly AuthenticationStateProvider? _authProvider;

    private CardHybridState _state;
    private CardHybridUser _currentUser = CardHybridUser.Guest;
    private bool _hasGuestChanges;
    private bool _initialized;

    public CardHybridCircuitSession(
        IQrSvgGenerator qr,
        ICardProfileStore store,
        AuthenticationStateProvider? authProvider = null)
    {
        _qr = qr ?? throw new ArgumentNullException(nameof(qr));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _authProvider = authProvider;
        _state = CardHybridState.CreateDefault(_qr.CreateSvg(CardProfile.Default.LandingUrl));
    }

    public CardHybridState State => _state;
    public CardHybridUser CurrentUser => _currentUser;

    /// <summary>
    /// \brief Guest 상태에서 이번 서킷 안에 편집이 있었는지 여부입니다.
    /// </summary>
    public bool HasGuestChanges => _hasGuestChanges && _currentUser.IsGuest;

    public event EventHandler<CardHybridState>? StateChanged;

    /// <summary>
    /// \brief 서킷 첫 사용 시 인증 상태를 읽어 사용자를 반영합니다.
    /// </summary>
    /// <remarks>
    /// Blazor Server 의 쿠키 기반 인증은 서킷 생성 시점에 확정되므로
    /// 그 이후엔 다시 조회할 필요가 없습니다. 페이지 <c>OnInitializedAsync</c> 에서 호출합니다.
    /// </remarks>
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        if (_authProvider is null)
        {
            return;
        }

        var authState = await _authProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = MapPrincipalToUser(authState.User);
        await ApplyUserAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private static CardHybridUser MapPrincipalToUser(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return CardHybridUser.Guest;
        }

        var userIdClaim = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return CardHybridUser.Guest;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = principal.FindFirstValue(ClaimTypes.Name)
                   ?? principal.FindFirstValue(ClaimTypes.GivenName)
                   ?? string.Empty;

        return new CardHybridUser(
            Id: $"oauth-{userIdClaim}",
            Email: email,
            DisplayName: string.IsNullOrWhiteSpace(name) ? email : name,
            SignedInAt: DateTime.UtcNow);
    }

    /// <summary>
    /// \brief 인증 결과로 얻은 사용자를 세션에 반영하고 저장된 스냅샷을 로드합니다.
    /// </summary>
    /// <param name="user">세션에 반영할 사용자 (<see cref="CardHybridUser.Guest"/> 가능).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    public async Task ApplyUserAsync(CardHybridUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        _currentUser = user;
        var snapshot = await _store.LoadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        LoadSnapshot(snapshot);
        _hasGuestChanges = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// \brief 외부(예: 브라우저 localStorage) 에서 복구한 스냅샷을 현재 사용자의 저장 스냅샷으로 대체합니다.
    /// </summary>
    /// <remarks>
    /// Guest 상태에서 편집한 내용을 로그인 후 사용자 계정으로 이관할 때 사용합니다.
    /// 사용자가 이미 저장한 스냅샷이 있으면 그 스냅샷은 이 호출로 덮어써집니다.
    /// </remarks>
    public async Task ReplaceUserSnapshotAsync(CardHybridSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (_currentUser.IsGuest)
        {
            throw new InvalidOperationException("Cannot replace snapshot for guest user.");
        }

        var withUserId = snapshot with { UserId = _currentUser.Id };
        LoadSnapshot(withUserId);
        _hasGuestChanges = false;

        await _store.SaveAsync(_currentUser.Id, withUserId, cancellationToken).ConfigureAwait(false);
        NotifyStateChanged();
    }

    /// <summary>
    /// \brief 현재 편집 상태를 <see cref="CardHybridSnapshot"/> 으로 캡처합니다.
    /// </summary>
    public CardHybridSnapshot CaptureCurrentSnapshot() =>
        new(_currentUser.Id, _state.Profile, _history.ToArray(), DateTime.Now);

    /// <summary>
    /// \brief 외부 스냅샷을 세션 메모리에 로드합니다 (저장하지 않음).
    /// </summary>
    /// <remarks>
    /// 브라우저 localStorage 에서 Guest 상태를 복구할 때 사용합니다.
    /// Guest 세션이면 <see cref="HasGuestChanges"/> 를 참으로 설정합니다.
    /// </remarks>
    public void ImportSnapshot(CardHybridSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        LoadSnapshot(snapshot);
        if (_currentUser.IsGuest)
        {
            _hasGuestChanges = true;
        }
        NotifyStateChanged();
    }

    public void UpdateProfile(CardProfile profile)
    {
        var payload = profile.LandingUrl;
        _state = _state with
        {
            Profile = profile,
            QrPayload = payload,
            QrSvg = _qr.CreateSvg(payload),
            LastUpdated = DateTime.Now
        };
        MarkDirty();
        PersistCurrent();
        NotifyStateChanged();
    }

    public CardHistoryEntry SaveCurrent()
    {
        var entry = new CardHistoryEntry(
            Id: Guid.NewGuid(),
            Profile: _state.Profile,
            LandingUrl: _state.Profile.LandingUrl,
            QrPayload: _state.QrPayload,
            CreatedAt: DateTime.Now);

        _history.Insert(0, entry);
        _state = _state with { History = _history.ToArray(), LastUpdated = DateTime.Now };
        MarkDirty();
        PersistCurrent();
        NotifyStateChanged();
        return entry;
    }

    public CardHistoryEntry? UpdateHistory(Guid id, CardProfile profile)
    {
        var index = _history.FindIndex(e => e.Id == id);
        if (index < 0)
            return null;

        var payload = profile.LandingUrl;
        var entry = _history[index] with
        {
            Profile = profile,
            LandingUrl = payload,
            QrPayload = payload,
            CreatedAt = DateTime.Now
        };
        _history[index] = entry;
        _state = _state with
        {
            Profile = profile,
            QrPayload = payload,
            QrSvg = _qr.CreateSvg(payload),
            History = _history.ToArray(),
            LastUpdated = DateTime.Now
        };
        MarkDirty();
        PersistCurrent();
        NotifyStateChanged();
        return entry;
    }

    public void LoadHistory(CardHistoryEntry entry) => UpdateProfile(entry.Profile);

    public void ClearHistory()
    {
        _history.Clear();
        _state = _state with { History = Array.Empty<CardHistoryEntry>(), LastUpdated = DateTime.Now };
        MarkDirty();
        PersistCurrent();
        NotifyStateChanged();
    }

    public bool RemoveHistory(Guid id)
    {
        var index = _history.FindIndex(e => e.Id == id);
        if (index < 0)
            return false;

        _history.RemoveAt(index);
        _state = _state with { History = _history.ToArray() };
        MarkDirty();
        PersistCurrent();
        NotifyStateChanged();
        return true;
    }

    private void LoadSnapshot(CardHybridSnapshot? snapshot)
    {
        _history.Clear();
        if (snapshot?.History is not null)
            _history.AddRange(snapshot.History);

        var profile = snapshot?.Profile ?? CardProfile.Default;
        _state = CardHybridState.Create(profile, _qr.CreateSvg(profile.LandingUrl), _history.ToArray());
    }

    private void MarkDirty()
    {
        if (_currentUser.IsGuest)
        {
            _hasGuestChanges = true;
        }
    }

    private void PersistCurrent()
    {
        if (_currentUser.IsGuest)
        {
            return;
        }

        var snapshot = new CardHybridSnapshot(
            _currentUser.Id, _state.Profile, _history.ToArray(), DateTime.Now);
        var userId = _currentUser.Id;

        _ = Task.Run(async () =>
        {
            await _saveLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _store.SaveAsync(userId, snapshot).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CardHybridCircuitSession save failed: {ex}");
            }
            finally
            {
                _saveLock.Release();
            }
        });
    }

    private void NotifyStateChanged()
    {
        var handlers = StateChanged?.GetInvocationList();
        if (handlers is null)
            return;

        foreach (var handler in handlers)
        {
            try { ((EventHandler<CardHybridState>)handler).Invoke(this, _state); }
            catch (Exception ex) { Debug.WriteLine($"CardHybridCircuitSession handler failed: {ex}"); }
        }
    }

    public void Dispose() => _saveLock.Dispose();
}
