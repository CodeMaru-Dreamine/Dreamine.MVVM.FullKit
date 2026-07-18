using System.Diagnostics;
using System.Security.Claims;
using Codemaru.Models;
using Codemaru.States;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>\brief 웹 방문자 1회 접속(Blazor Circuit) 단위로 격리되는 CardHybrid 세션입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card hybrid circuit session functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>인증 자체는 상위 Dreamine.Identity 계층이 처리하고, 이 세션은 <see cref="ApplyUserAsync"/> 로 결과를 반영합니다. Guest 상태에서 편집한 내역은 <see cref="HasGuestChanges"/> 로 추적하며 로그인 시 <see cref="PromoteGuestChangesAsync"/> 로 사용자 소유로 이관할 수 있습니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public sealed class CardHybridCircuitSession : IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>save Lock 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the save lock value.</para>
    /// \endif
    /// </summary>
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    /// <summary>
    /// \if KO
    /// <para>history 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the history value.</para>
    /// \endif
    /// </summary>
    private readonly List<CardHistoryEntry> _history = new();
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
    /// <para>store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the store value.</para>
    /// \endif
    /// </summary>
    private readonly ICardProfileStore _store;
    /// <summary>
    /// \if KO
    /// <para>auth Provider 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the auth provider value.</para>
    /// \endif
    /// </summary>
    private readonly AuthenticationStateProvider? _authProvider;

    /// <summary>
    /// \if KO
    /// <para>state 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the state value.</para>
    /// \endif
    /// </summary>
    private CardHybridState _state;
    /// <summary>
    /// \if KO
    /// <para>current User 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the current user value.</para>
    /// \endif
    /// </summary>
    private CardHybridUser _currentUser = CardHybridUser.Guest;
    /// <summary>
    /// \if KO
    /// <para>has Guest Changes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the has guest changes value.</para>
    /// \endif
    /// </summary>
    private bool _hasGuestChanges;
    /// <summary>
    /// \if KO
    /// <para>initialized 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the initialized value.</para>
    /// \endif
    /// </summary>
    private bool _initialized;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="CardHybridCircuitSession"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="CardHybridCircuitSession"/> class with the specified settings.</para>
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
    /// <param name="store">
    /// \if KO
    /// <para>store에 사용할 <c>ICardProfileStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ICardProfileStore</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <param name="authProvider">
    /// \if KO
    /// <para>auth Provider에 사용할 <c>AuthenticationStateProvider?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AuthenticationStateProvider?</c> value used for auth provider.</para>
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

    /// <summary>
    /// \if KO
    /// <para>State 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the state value.</para>
    /// \endif
    /// </summary>
    public CardHybridState State => _state;
    /// <summary>
    /// \if KO
    /// <para>Current User 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current user value.</para>
    /// \endif
    /// </summary>
    public CardHybridUser CurrentUser => _currentUser;

    /// <summary>
    /// \if KO
    /// <para>\brief Guest 상태에서 이번 서킷 안에 편집이 있었는지 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has guest changes value.</para>
    /// \endif
    /// </summary>
    public bool HasGuestChanges => _hasGuestChanges && _currentUser.IsGuest;

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
    /// <para>\brief 서킷 첫 사용 시 인증 상태를 읽어 사용자를 반영합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure initialized async operation.</para>
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
    /// <para>Ensure Initialized Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the ensure initialized async operation.</para>
    /// \endif
    /// </returns>
    /// <remarks>
    /// \if KO
    /// <para>Blazor Server 의 쿠키 기반 인증은 서킷 생성 시점에 확정되므로 그 이후엔 다시 조회할 필요가 없습니다. 페이지 <c>OnInitializedAsync</c> 에서 호출합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
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

    /// <summary>
    /// \if KO
    /// <para>Map Principal To User 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the map principal to user operation.</para>
    /// \endif
    /// </summary>
    /// <param name="principal">
    /// \if KO
    /// <para>principal에 사용할 <c>ClaimsPrincipal</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ClaimsPrincipal</c> value used for principal.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Map Principal To User 작업에서 생성한 <c>CardHybridUser</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridUser</c> result produced by the map principal to user operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 인증 결과로 얻은 사용자를 세션에 반영하고 저장된 스냅샷을 로드합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply user async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="user">
    /// \if KO
    /// <para>세션에 반영할 사용자 (<see cref="CardHybridUser.Guest"/> 가능).</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 토큰.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Apply User Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the apply user async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 외부(예: 브라우저 localStorage) 에서 복구한 스냅샷을 현재 사용자의 저장 스냅샷으로 대체합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the replace user snapshot async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="snapshot">
    /// \if KO
    /// <para>snapshot에 사용할 <c>CardHybridSnapshot</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSnapshot</c> value used for snapshot.</para>
    /// \endif
    /// </param>
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
    /// <para>Replace User Snapshot Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the replace user snapshot async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Replace User Snapshot Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the replace user snapshot async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    /// <remarks>
    /// \if KO
    /// <para>Guest 상태에서 편집한 내용을 로그인 후 사용자 계정으로 이관할 때 사용합니다. 사용자가 이미 저장한 스냅샷이 있으면 그 스냅샷은 이 호출로 덮어써집니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 현재 편집 상태를 <see cref="CardHybridSnapshot"/> 으로 캡처합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the capture current snapshot operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Capture Current Snapshot 작업에서 생성한 <c>CardHybridSnapshot</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSnapshot</c> result produced by the capture current snapshot operation.</para>
    /// \endif
    /// </returns>
    public CardHybridSnapshot CaptureCurrentSnapshot() =>
        new(_currentUser.Id, _state.Profile, _history.ToArray(), DateTime.Now);

    /// <summary>
    /// \if KO
    /// <para>\brief 외부 스냅샷을 세션 메모리에 로드합니다 (저장하지 않음).</para>
    /// \endif
    /// \if EN
    /// <para>Performs the import snapshot operation.</para>
    /// \endif
    /// </summary>
    /// <param name="snapshot">
    /// \if KO
    /// <para>snapshot에 사용할 <c>CardHybridSnapshot</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSnapshot</c> value used for snapshot.</para>
    /// \endif
    /// </param>
    /// <remarks>
    /// \if KO
    /// <para>브라우저 localStorage 에서 Guest 상태를 복구할 때 사용합니다. Guest 세션이면 <see cref="HasGuestChanges"/> 를 참으로 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
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

    /// <summary>
    /// \if KO
    /// <para>Update Profile 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update profile operation.</para>
    /// \endif
    /// </summary>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Current 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves current data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Current 작업에서 생성한 <c>CardHistoryEntry</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHistoryEntry</c> result produced by the save current operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Update History 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update history operation.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>Guid</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Guid</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <param name="profile">
    /// \if KO
    /// <para>profile에 사용할 <c>CardProfile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardProfile</c> value used for profile.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Update History 작업에서 생성한 <c>CardHistoryEntry?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHistoryEntry?</c> result produced by the update history operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>History 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads history data.</para>
    /// \endif
    /// </summary>
    /// <param name="entry">
    /// \if KO
    /// <para>entry에 사용할 <c>CardHistoryEntry</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHistoryEntry</c> value used for entry.</para>
    /// \endif
    /// </param>
    public void LoadHistory(CardHistoryEntry entry) => UpdateProfile(entry.Profile);

    /// <summary>
    /// \if KO
    /// <para>Clear History 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear history operation.</para>
    /// \endif
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _state = _state with { History = Array.Empty<CardHistoryEntry>(), LastUpdated = DateTime.Now };
        MarkDirty();
        PersistCurrent();
        NotifyStateChanged();
    }

    /// <summary>
    /// \if KO
    /// <para>History 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the history item.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>Guid</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Guid</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Remove History 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the remove history condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Snapshot 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads snapshot data.</para>
    /// \endif
    /// </summary>
    /// <param name="snapshot">
    /// \if KO
    /// <para>snapshot에 사용할 <c>CardHybridSnapshot?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSnapshot?</c> value used for snapshot.</para>
    /// \endif
    /// </param>
    private void LoadSnapshot(CardHybridSnapshot? snapshot)
    {
        _history.Clear();
        if (snapshot?.History is not null)
            _history.AddRange(snapshot.History);

        var profile = snapshot?.Profile ?? CardProfile.Default;
        _state = CardHybridState.Create(profile, _qr.CreateSvg(profile.LandingUrl), _history.ToArray());
    }

    /// <summary>
    /// \if KO
    /// <para>Mark Dirty 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the mark dirty operation.</para>
    /// \endif
    /// </summary>
    private void MarkDirty()
    {
        if (_currentUser.IsGuest)
        {
            _hasGuestChanges = true;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Persist Current 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the persist current operation.</para>
    /// \endif
    /// </summary>
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
            return;

        foreach (var handler in handlers)
        {
            try { ((EventHandler<CardHybridState>)handler).Invoke(this, _state); }
            catch (Exception ex) { Debug.WriteLine($"CardHybridCircuitSession handler failed: {ex}"); }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    public void Dispose() => _saveLock.Dispose();
}
