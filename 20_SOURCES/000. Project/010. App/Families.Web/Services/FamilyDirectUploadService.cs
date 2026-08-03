using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// Issues short-lived, one-use upload capabilities and commits each successful upload
/// together with its post metadata. File bytes never travel through the Blazor circuit.
/// </summary>
public sealed class FamilyDirectUploadService
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan UploadBlockLifetime = TimeSpan.FromHours(24);

    private readonly IFamilyTenantStore _tenants;
    private readonly IPostStore _posts;
    private readonly IMediaService _media;
    private readonly ConcurrentDictionary<string, UploadTicket> _tickets = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _postLocks = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _blockedPosts = new();

    public FamilyDirectUploadService(
        IFamilyTenantStore tenants,
        IPostStore posts,
        IMediaService media)
    {
        _tenants = tenants;
        _posts = posts;
        _media = media;
    }

    /// <summary>Creates one ticket per selected browser file.</summary>
    public async Task<IReadOnlyList<string>> IssueTicketsAsync(
        string slug,
        FamilyUploadPurpose purpose,
        PostEntry? postSnapshot,
        int count,
        FamilyCurrentUser user,
        string? authorizedAdminSlug,
        CancellationToken ct = default)
    {
        if (count is < 1 or > 50)
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.TooManyFiles);
        }

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                     ?? throw new FamilyUploadValidationException(FamilyUploadErrorCodes.AlbumMissing);

        var accountAuthorized = user.IsAuthenticated && IsAdmin(config, user.Id);
        var passwordSessionAuthorized =
            string.Equals(authorizedAdminSlug, slug, StringComparison.Ordinal);
        if (!accountAuthorized && !passwordSessionAuthorized)
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.Unauthorized);
        }

        if (purpose == FamilyUploadPurpose.PostMedia
            && (postSnapshot is null || string.IsNullOrWhiteSpace(postSnapshot.Id)))
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.PostRequired);
        }

        if (purpose == FamilyUploadPurpose.OgImage && count != 1)
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.TooManyFiles);
        }

        CleanupExpiredTickets();
        if (purpose == FamilyUploadPurpose.PostMedia
            && postSnapshot is not null
            && _blockedPosts.ContainsKey(PostKey(slug, postSnapshot.Id)))
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.UploadCancelled);
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(TicketLifetime);
        var result = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            // Account-authorized capabilities remain bound to that account and are
            // membership-checked again at the HTTP endpoint. A password-authorized Blazor
            // session may coexist with an unrelated signed-in CodeMaru account, so it is
            // represented by an empty account id and relies on the opaque one-use ticket.
            var ticketUserId = accountAuthorized ? user.Id : string.Empty;
            var ticket = new UploadTicket(
                slug,
                purpose,
                postSnapshot is null ? null : ClonePost(postSnapshot),
                ticketUserId,
                expiresAt);
            if (!_tickets.TryAdd(token, ticket))
            {
                index--;
                continue;
            }

            result.Add(token);
        }

        return result;
    }

    /// <summary>
    /// Consumes and validates a ticket before ASP.NET reads the multipart body. This prevents
    /// an invalid capability from forcing the server to buffer an attacker-controlled file.
    /// </summary>
    public async Task<FamilyUploadReservation> ReserveTicketAsync(
        string token,
        FamilyCurrentUser requestUser,
        CancellationToken ct = default)
    {
        if (!_tickets.TryRemove(token, out var ticket)
            || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new FamilyUploadTicketException(FamilyUploadErrorCodes.TicketExpired);
        }

        if (!string.IsNullOrWhiteSpace(ticket.UserId)
            && !string.Equals(ticket.UserId, requestUser.Id, StringComparison.Ordinal))
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.Unauthorized);
        }

        var config = await _tenants.GetAsync(ticket.Slug, ct).ConfigureAwait(false)
                     ?? throw new FamilyUploadValidationException(FamilyUploadErrorCodes.AlbumMissing);
        if (!string.IsNullOrWhiteSpace(ticket.UserId) && !IsAdmin(config, ticket.UserId))
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.Unauthorized);
        }

        return new FamilyUploadReservation(
            ticket.Slug,
            ticket.Purpose,
            ticket.PostSnapshot);
    }

    /// <summary>Stores one reserved file and commits its metadata.</summary>
    public async Task<FamilyUploadResult> UploadAsync(
        FamilyUploadReservation reservation,
        Stream content,
        string originalFileName,
        string contentType,
        long size,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        if (reservation.Purpose == FamilyUploadPurpose.Cover)
        {
            var cover = await _media.UploadCoverAsync(
                reservation.Slug,
                content,
                originalFileName,
                contentType,
                size,
                ct).ConfigureAwait(false);
            return new FamilyUploadResult(
                cover.FileName,
                cover.IsVideo,
                _media.GetCoverUrl(reservation.Slug, cover.FileName),
                originalFileName);
        }

        if (reservation.Purpose == FamilyUploadPurpose.OgImage)
        {
            var ogImage = await _media.UploadOgImageAsync(
                reservation.Slug,
                content,
                originalFileName,
                contentType,
                size,
                ct).ConfigureAwait(false);
            var version = Uri.EscapeDataString(ogImage.FileName);
            return new FamilyUploadResult(
                ogImage.FileName,
                ogImage.IsVideo,
                $"/og/families/{Uri.EscapeDataString(reservation.Slug)}?v={version}",
                originalFileName);
        }

        var snapshot = reservation.PostSnapshot
                       ?? throw new FamilyUploadValidationException(FamilyUploadErrorCodes.PostRequired);
        var lockKey = PostKey(reservation.Slug, snapshot.Id);
        if (_blockedPosts.ContainsKey(lockKey))
        {
            throw new FamilyUploadValidationException(FamilyUploadErrorCodes.UploadCancelled);
        }

        var postLock = _postLocks.GetOrAdd(lockKey, static _ => new SemaphoreSlim(1, 1));
        StoredMediaFile? stored = null;
        var lockTaken = false;
        try
        {
            await postLock.WaitAsync(ct).ConfigureAwait(false);
            lockTaken = true;
            if (_blockedPosts.ContainsKey(lockKey))
            {
                throw new FamilyUploadValidationException(FamilyUploadErrorCodes.UploadCancelled);
            }

            // Hold the post lock while writing the physical file as well as its metadata.
            // A concurrent delete can mark the post as blocked immediately, but it waits
            // until the stream has closed before removing the media directory.
            stored = await _media.UploadPostMediaAsync(
                reservation.Slug,
                snapshot.Id,
                content,
                originalFileName,
                contentType,
                size,
                ct).ConfigureAwait(false);
            if (_blockedPosts.ContainsKey(lockKey))
            {
                throw new FamilyUploadValidationException(FamilyUploadErrorCodes.UploadCancelled);
            }

            await _posts.MutateAsync(
                reservation.Slug,
                snapshot.Id,
                current =>
                {
                    var post = current ?? ClonePost(snapshot);
                    var mediaNames = stored.IsVideo ? post.VideoFileNames : post.PhotoFileNames;
                    if (!mediaNames.Contains(stored.FileName, StringComparer.Ordinal))
                    {
                        mediaNames.Add(stored.FileName);
                    }

                    return post;
                },
                ct).ConfigureAwait(false);
        }
        catch
        {
            if (stored is not null)
            {
                try
                {
                    await _media.DeletePostMediaAsync(
                        reservation.Slug, snapshot.Id, stored.FileName, CancellationToken.None).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // Preserve the original persistence error; cleanup can be retried by maintenance.
                }
            }

            throw;
        }
        finally
        {
            if (lockTaken)
            {
                postLock.Release();
            }
        }

        // The successful path assigns stored before committing post metadata.
        return new FamilyUploadResult(
            stored!.FileName,
            stored.IsVideo,
            _media.GetMediaUrl(reservation.Slug, snapshot.Id, stored.FileName),
            originalFileName);
    }

    /// <summary>
    /// Prevents already-issued or in-flight uploads from recreating a post after an
    /// administrator deletes it, then removes the persisted post and its media atomically
    /// with respect to direct-upload metadata commits.
    /// </summary>
    public async Task DeletePostAndBlockUploadsAsync(
        string slug,
        string postId,
        CancellationToken ct = default)
    {
        var lockKey = PostKey(slug, postId);
        _blockedPosts[lockKey] = DateTimeOffset.UtcNow;

        foreach (var entry in _tickets)
        {
            if (entry.Value.Purpose == FamilyUploadPurpose.PostMedia
                && string.Equals(entry.Value.Slug, slug, StringComparison.Ordinal)
                && string.Equals(entry.Value.PostSnapshot?.Id, postId, StringComparison.Ordinal))
            {
                _tickets.TryRemove(entry.Key, out _);
            }
        }

        var postLock = _postLocks.GetOrAdd(lockKey, static _ => new SemaphoreSlim(1, 1));
        await postLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _posts.DeleteAsync(slug, postId, ct).ConfigureAwait(false);
        }
        finally
        {
            postLock.Release();
        }
    }

    private static bool IsAdmin(FamilyConfig config, string userId) =>
        string.Equals(config.OwnerUserId, userId, StringComparison.Ordinal)
        || config.AdminUsers.Any(admin =>
            string.Equals(admin.UserId, userId, StringComparison.Ordinal));

    private static PostEntry ClonePost(PostEntry source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Content = source.Content,
        PostedAt = source.PostedAt,
        AlbumId = source.AlbumId,
        PhotoFileNames = [.. source.PhotoFileNames],
        VideoFileNames = [.. source.VideoFileNames],
        IsPinned = source.IsPinned,
        MediaPosition = source.MediaPosition
    };

    private static string PostKey(string slug, string postId) => $"{slug}\n{postId}";

    private void CleanupExpiredTickets()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in _tickets)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _tickets.TryRemove(entry.Key, out _);
            }
        }

        foreach (var entry in _blockedPosts)
        {
            if (entry.Value.Add(UploadBlockLifetime) <= now)
            {
                _blockedPosts.TryRemove(entry.Key, out _);
            }
        }
    }

    private sealed record UploadTicket(
        string Slug,
        FamilyUploadPurpose Purpose,
        PostEntry? PostSnapshot,
        string UserId,
        DateTimeOffset ExpiresAt);
}

/// <summary>Scopes an upload ticket to either post media or the tenant cover.</summary>
public enum FamilyUploadPurpose
{
    PostMedia,
    Cover,
    OgImage
}

/// <summary>Result returned to the browser after a committed upload.</summary>
public sealed record FamilyUploadResult(
    string FileName,
    bool IsVideo,
    string Url,
    string OriginalFileName);

/// <summary>Validated, in-process capability passed from endpoint validation to body handling.</summary>
public sealed class FamilyUploadReservation
{
    internal FamilyUploadReservation(
        string slug,
        FamilyUploadPurpose purpose,
        PostEntry? postSnapshot)
    {
        Slug = slug;
        Purpose = purpose;
        PostSnapshot = postSnapshot;
    }

    internal string Slug { get; }
    internal FamilyUploadPurpose Purpose { get; }
    internal PostEntry? PostSnapshot { get; }
}

/// <summary>Stable upload error codes shared by the Blazor client and HTTP endpoint.</summary>
public static class FamilyUploadErrorCodes
{
    public const string Unauthorized = "unauthorized";
    public const string TooManyFiles = "too_many_files";
    public const string AlbumMissing = "album_missing";
    public const string PostRequired = "post_required";
    public const string TicketExpired = "ticket_expired";
    public const string UploadCancelled = "upload_cancelled";
}

/// <summary>Represents a user-correctable upload validation failure without UI text.</summary>
public sealed class FamilyUploadValidationException : Exception
{
    public FamilyUploadValidationException(string errorCode) : base(errorCode)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

/// <summary>Indicates that an opaque one-use upload capability is no longer valid.</summary>
public sealed class FamilyUploadTicketException : Exception
{
    public FamilyUploadTicketException(string errorCode) : base(errorCode)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
