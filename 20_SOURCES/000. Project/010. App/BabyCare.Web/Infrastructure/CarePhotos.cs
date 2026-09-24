using BabyCare.Domain;
using Dreamine.Identity;

namespace BabyCare.Infrastructure;

public sealed class CarePhotos(string root, CareStore store)
{
    public const long MaxBytes = 8 * 1024 * 1024;
    public static bool ValidId(string? id) => id is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(id, @"\A[a-f0-9]{32}\.(jpg|png|webp)\z");
    public static string Url(string family, string photo) => $"/care-photos/{Uri.EscapeDataString(family)}/{Uri.EscapeDataString(photo)}";
    public async Task<string> SaveAsync(string user, string family, Stream stream, CancellationToken cancellation = default)
    {
        store.Get(user, family); // Authorize before reading or writing any bytes.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellation)) > 0)
        {
            if (buffer.Length + read > MaxBytes) throw new CareException("사진은 JPG·PNG·WebP, 장당 8MB까지 첨부할 수 있어요.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellation);
        }
        var bytes = buffer.ToArray();
        var extension = Detect(bytes) ?? throw new CareException("사진은 JPG·PNG·WebP, 장당 8MB까지 첨부할 수 있어요.");
        store.Get(user, family); // Recheck membership after the upload.
        var folder = Path.Combine(root, family);
        Directory.CreateDirectory(folder);
        var id = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(folder, id), bytes, cancellation);
        return id;
    }
    public string? ReadPath(string user, string family, string id)
    {
        store.Get(user, family);
        if (!ValidId(id)) return null;
        var path = Path.Combine(root, family, id);
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }
    public static string? Detect(byte[] b)
    {
        if (b.Length < 12) return null;
        if (b[0] == 0xff && b[1] == 0xd8 && b[2] == 0xff) return ".jpg";
        if (b.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return ".png";
        if (b.AsSpan(0,4).SequenceEqual("RIFF"u8) && b.AsSpan(8,4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }
}

public static class CarePhotoEndpoints
{
    public static void MapCarePhotos(this WebApplication app)
    {
        app.MapGet("/care-photos/{family}/{photo}", (string family, string photo, HttpContext context, CarePhotos photos) =>
        {
            if (context.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();
            var user = context.User.FindFirst(DreamineIdentityExtensions.UserIdClaimType)?.Value ?? "";
            context.Response.Headers.CacheControl = "private, no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
            try
            {
                var path = photos.ReadPath(user, family, photo);
                if (path is null) return Results.NotFound();
                var contentType = Path.GetExtension(path) switch { ".jpg" => "image/jpeg", ".png" => "image/png", _ => "image/webp" };
                return Results.File(path, contentType);
            }
            catch (CareException) { return Results.NotFound(); }
        });
    }
}
