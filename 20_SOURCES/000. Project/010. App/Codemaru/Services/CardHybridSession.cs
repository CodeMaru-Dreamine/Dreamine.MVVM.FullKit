using Codemaru.Models;
using Codemaru.States;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Codemaru.Services;

/// <summary>
/// \brief Coordinates CardHybrid state, QR generation, history, and persistence.
/// </summary>
public sealed class CardHybridSession
{
    private static readonly object Sync = new();
    private static readonly SemaphoreSlim SharedSaveLock = new(1, 1);
    private static readonly List<CardHistoryEntry> SharedHistory = new();
    private static CardHybridState? SharedState;
    private static CardHybridUser? SharedCurrentUser;
    private static event EventHandler<CardHybridState>? SharedStateChanged;

    private readonly FreeQrSvgGenerator _qr;
    private readonly ICardProfileStore _store;

    /// <summary>
    /// \brief Initializes a new instance of the <see cref="CardHybridSession" /> class.
    /// </summary>
    /// <param name="qr">The QR SVG generator.</param>
    /// <param name="store">The persistent profile store.</param>
    public CardHybridSession(FreeQrSvgGenerator qr, ICardProfileStore store)
    {
        _qr = qr ?? throw new ArgumentNullException(nameof(qr));
        _store = store ?? throw new ArgumentNullException(nameof(store));

        lock (Sync)
        {
            if (SharedState is not null)
            {
                return;
            }

            var lastUser = _store.LoadLastUserAsync().GetAwaiter().GetResult();
            SharedCurrentUser = lastUser is not null && !string.IsNullOrWhiteSpace(lastUser.PasswordHash)
                ? lastUser
                : CardHybridUser.Guest;
            var snapshot = _store.LoadAsync(SharedCurrentUser.Id).GetAwaiter().GetResult();
            LoadSnapshot(snapshot);
        }
    }

    /// <summary>
    /// \brief Gets the current CardHybrid state.
    /// </summary>
    public CardHybridState State => SharedState ?? CardHybridState.CreateDefault(_qr.CreateSvg(CardProfile.Default.LandingUrl));

    public CardHybridUser CurrentUser => SharedCurrentUser ?? CardHybridUser.Guest;

    /// <summary>
    /// \brief Occurs when the CardHybrid state has changed.
    /// </summary>
    public event EventHandler<CardHybridState>? StateChanged
    {
        add => SharedStateChanged += value;
        remove => SharedStateChanged -= value;
    }

    public async Task<CardHybridSignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return CardHybridSignInResult.MissingEmail;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return CardHybridSignInResult.MissingPassword;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var userId = CreateUserId(normalizedEmail);
        var storedUser = await _store.LoadUserAsync(userId, cancellationToken).ConfigureAwait(false);

        if (storedUser is not null && !VerifyPassword(password, storedUser.PasswordHash))
        {
            return CardHybridSignInResult.InvalidPassword;
        }

        var result = storedUser is null ? CardHybridSignInResult.Created : CardHybridSignInResult.Success;
        var user = new CardHybridUser(
            Id: userId,
            Email: normalizedEmail,
            DisplayName: CreateDisplayName(normalizedEmail),
            PasswordHash: storedUser?.PasswordHash ?? HashPassword(password),
            SignedInAt: DateTime.Now);

        SharedCurrentUser = user;
        await _store.SaveUserAsync(user, cancellationToken).ConfigureAwait(false);
        await _store.SaveLastUserAsync(user, cancellationToken).ConfigureAwait(false);
        var snapshot = await _store.LoadAsync(user.Id, cancellationToken).ConfigureAwait(false);
        LoadSnapshot(snapshot);
        NotifyStateChanged();
        return result;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        SharedCurrentUser = CardHybridUser.Guest;
        await _store.SaveLastUserAsync(CurrentUser, cancellationToken).ConfigureAwait(false);
        var snapshot = await _store.LoadAsync(CurrentUser.Id, cancellationToken).ConfigureAwait(false);
        LoadSnapshot(snapshot);
        NotifyStateChanged();
    }

    /// <summary>
    /// \brief Updates the current profile and persists it.
    /// </summary>
    /// <param name="profile">The updated profile.</param>
    public void UpdateProfile(CardProfile profile)
    {
        var payload = profile.LandingUrl;
        SharedState = State with
        {
            Profile = profile,
            QrPayload = payload,
            QrSvg = _qr.CreateSvg(payload),
            LastUpdated = DateTime.Now
        };

        PersistCurrent();
        NotifyStateChanged();
    }

    /// <summary>
    /// \brief Saves the current profile to the in-memory and persistent history.
    /// </summary>
    /// <returns>The saved history entry.</returns>
    public CardHistoryEntry SaveCurrent()
    {
        var entry = new CardHistoryEntry(
            Id: Guid.NewGuid(),
            Profile: State.Profile,
            LandingUrl: State.Profile.LandingUrl,
            QrPayload: State.QrPayload,
            CreatedAt: DateTime.Now);

        SharedHistory.Insert(0, entry);
        SharedState = State with
        {
            History = SharedHistory.ToArray(),
            LastUpdated = DateTime.Now
        };

        PersistCurrent();
        NotifyStateChanged();
        return entry;
    }

    /// <summary>
    /// \brief Updates a saved profile history entry with the supplied profile.
    /// </summary>
    /// <param name="id">The history entry ID to update.</param>
    /// <param name="profile">The replacement profile.</param>
    /// <returns>The updated history entry, or <c>null</c> when the entry does not exist.</returns>
    public CardHistoryEntry? UpdateHistory(Guid id, CardProfile profile)
    {
        var index = SharedHistory.FindIndex(entry => entry.Id == id);
        if (index < 0)
        {
            return null;
        }

        var payload = profile.LandingUrl;
        var entry = SharedHistory[index] with
        {
            Profile = profile,
            LandingUrl = payload,
            QrPayload = payload,
            CreatedAt = DateTime.Now
        };

        SharedHistory[index] = entry;
        SharedState = State with
        {
            Profile = profile,
            QrPayload = payload,
            QrSvg = _qr.CreateSvg(payload),
            History = SharedHistory.ToArray(),
            LastUpdated = DateTime.Now
        };

        PersistCurrent();
        NotifyStateChanged();
        return entry;
    }

    /// <summary>
    /// \brief Loads a saved history entry as the current profile.
    /// </summary>
    /// <param name="entry">The history entry to load.</param>
    public void LoadHistory(CardHistoryEntry entry)
    {
        UpdateProfile(entry.Profile);
    }

    /// <summary>
    /// \brief Clears the saved profile history.
    /// </summary>
    public void ClearHistory()
    {
        SharedHistory.Clear();
        SharedState = State with
        {
            History = Array.Empty<CardHistoryEntry>(),
            LastUpdated = DateTime.Now
        };

        PersistCurrent();
        NotifyStateChanged();
    }

    /// <summary>
    /// \brief Removes a saved profile history entry.
    /// </summary>
    /// <param name="id">The history entry ID to remove.</param>
    /// <returns><c>true</c> when an entry was removed.</returns>
    public bool RemoveHistory(Guid id)
    {
        var index = SharedHistory.FindIndex(entry => entry.Id == id);
        if (index < 0)
        {
            return false;
        }

        SharedHistory.RemoveAt(index);
        SharedState = State with
        {
            History = SharedHistory.ToArray(),
            LastUpdated = DateTime.Now
        };

        PersistCurrent();
        NotifyStateChanged();
        return true;
    }

    private void PersistCurrent()
    {
        var state = State;
        var userId = CurrentUser.Id;
        var snapshot = new CardHybridSnapshot(userId, state.Profile, SharedHistory.ToArray(), DateTime.Now);

        _ = Task.Run(async () =>
        {
            await SharedSaveLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _store.SaveAsync(userId, snapshot).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CardHybrid profile save failed: {ex}");
            }
            finally
            {
                SharedSaveLock.Release();
            }
        });
    }

    private void LoadSnapshot(CardHybridSnapshot? snapshot)
    {
        SharedHistory.Clear();
        if (snapshot?.History is not null)
        {
            SharedHistory.AddRange(snapshot.History);
        }

        var profile = snapshot?.Profile ?? CardProfile.Default;
        SharedState = CardHybridState.Create(profile, _qr.CreateSvg(profile.LandingUrl), SharedHistory.ToArray());
    }

    private void NotifyStateChanged()
    {
        var handlers = SharedStateChanged?.GetInvocationList();
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

    private static string CreateUserId(string email)
    {
        return new string(email
            .Select(static c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_')
            .ToArray());
    }

    private static string CreateDisplayName(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 ? email[..at] : email;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            120_000,
            HashAlgorithmName.SHA256,
            32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                120_000,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
