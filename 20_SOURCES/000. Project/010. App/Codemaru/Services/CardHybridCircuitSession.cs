using Codemaru.Models;
using Codemaru.States;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Codemaru.Services;

/// <summary>
/// 웹 방문자 1회 접속(Blazor Circuit) 단위로 격리되는 CardHybrid 세션.
/// Singleton인 CardHybridSession과 달리 회로마다 독립된 상태를 가지므로
/// 다른 방문자의 로그인·이력이 노출되지 않습니다.
/// </summary>
public sealed class CardHybridCircuitSession : IDisposable
{
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly List<CardHistoryEntry> _history = new();
    private readonly IQrSvgGenerator _qr;
    private readonly ICardProfileStore _store;

    private CardHybridState _state;
    private CardHybridUser _currentUser = CardHybridUser.Guest;

    public CardHybridCircuitSession(IQrSvgGenerator qr, ICardProfileStore store)
    {
        _qr = qr ?? throw new ArgumentNullException(nameof(qr));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _state = CardHybridState.CreateDefault(_qr.CreateSvg(CardProfile.Default.LandingUrl));
    }

    public CardHybridState State => _state;
    public CardHybridUser CurrentUser => _currentUser;

    public event EventHandler<CardHybridState>? StateChanged;

    public async Task<CardHybridSignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return CardHybridSignInResult.MissingEmail;
        if (string.IsNullOrWhiteSpace(password))
            return CardHybridSignInResult.MissingPassword;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var userId = CreateUserId(normalizedEmail);
        var storedUser = await _store.LoadUserAsync(userId, cancellationToken).ConfigureAwait(false);

        if (storedUser is not null && !VerifyPassword(password, storedUser.PasswordHash))
            return CardHybridSignInResult.InvalidPassword;

        var result = storedUser is null ? CardHybridSignInResult.Created : CardHybridSignInResult.Success;
        var user = new CardHybridUser(
            Id: userId,
            Email: normalizedEmail,
            DisplayName: CreateDisplayName(normalizedEmail),
            PasswordHash: storedUser?.PasswordHash ?? HashPassword(password),
            SignedInAt: DateTime.Now);

        _currentUser = user;
        await _store.SaveUserAsync(user, cancellationToken).ConfigureAwait(false);
        await _store.SaveLastUserAsync(user, cancellationToken).ConfigureAwait(false);

        var snapshot = await _store.LoadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        LoadSnapshot(snapshot);
        NotifyStateChanged();
        return result;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _currentUser = CardHybridUser.Guest;
        var snapshot = await _store.LoadAsync(CardHybridUser.Guest.Id, cancellationToken).ConfigureAwait(false);
        LoadSnapshot(snapshot);
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
        PersistCurrent();
        NotifyStateChanged();
        return entry;
    }

    public void LoadHistory(CardHistoryEntry entry) => UpdateProfile(entry.Profile);

    public void ClearHistory()
    {
        _history.Clear();
        _state = _state with { History = Array.Empty<CardHistoryEntry>(), LastUpdated = DateTime.Now };
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

    private void PersistCurrent()
    {
        if (_currentUser == CardHybridUser.Guest)
            return;

        var snapshot = new CardHybridSnapshot(_currentUser.Id, _state.Profile, _history.ToArray(), DateTime.Now);
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

    private static string CreateUserId(string email) =>
        new string(email.Select(static c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());

    private static string CreateDisplayName(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 ? email[..at] : email;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }

    public void Dispose() => _saveLock.Dispose();
}
