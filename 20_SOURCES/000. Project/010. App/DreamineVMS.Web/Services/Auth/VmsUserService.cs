using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Dreamine.Identity;
using DreamineVMS.Web.Models;
using Microsoft.Data.Sqlite;

namespace DreamineVMS.Web.Services.Auth;

public sealed class VmsUserService
{
    private readonly VmsDatabase _db;

    public VmsUserService(VmsDatabase db) => _db = db;

    public async Task<(bool Ok, string Error, VmsUser? User)> RegisterAsync(
        string email, string displayName, string password)
    {
        email = email.Trim().ToLowerInvariant();
        displayName = displayName.Trim();

        if (string.IsNullOrWhiteSpace(email)) return (false, "이메일을 입력하세요.", null);
        if (string.IsNullOrWhiteSpace(displayName)) return (false, "이름을 입력하세요.", null);
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return (false, "비밀번호는 6자 이상이어야 합니다.", null);

        if (await FindByEmailAsync(email) is not null)
            return (false, "이미 사용 중인 이메일입니다.", null);

        var salt = GenerateSalt();
        var hash = HashPassword(password, salt);
        var slug = await UniqueSlugAsync(displayName);

        var user = new VmsUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = email,
            DisplayName = displayName,
            PublicSlug = slug,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO users (id, email, display_name, public_slug, password_hash, password_salt, created_at)
            VALUES ($id, $email, $dn, $slug, $hash, $salt, $ca)
            """;
        cmd.Parameters.AddWithValue("$id", user.Id);
        cmd.Parameters.AddWithValue("$email", user.Email);
        cmd.Parameters.AddWithValue("$dn", user.DisplayName);
        cmd.Parameters.AddWithValue("$slug", user.PublicSlug);
        cmd.Parameters.AddWithValue("$hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("$salt", user.PasswordSalt);
        cmd.Parameters.AddWithValue("$ca", user.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync();

        return (true, "", user);
    }

    public async Task<(bool Ok, string Error, VmsUser? User)> LoginAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await FindByEmailAsync(email);
        if (user is null) return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(user.PasswordSalt))
            return (false, "에이전트 연결 비밀번호가 아직 설정되지 않았습니다.", null);

        if (HashPassword(password, user.PasswordSalt) != user.PasswordHash)
            return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);

        return (true, "", user);
    }

    public async Task<VmsUser?> EnsureExternalUserAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identityId = principal.FindFirstValue(DreamineIdentityExtensions.UserIdClaimType);
        if (string.IsNullOrWhiteSpace(identityId))
        {
            return null;
        }

        var userId = $"oauth-{identityId}";
        var userById = await FindByIdAsync(userId);
        if (userById is not null)
        {
            return userById;
        }

        var provider = principal.FindFirstValue(DreamineIdentityExtensions.ProviderClaimType) ?? "OAuth";
        var email = NormalizeEmail(principal.FindFirstValue(ClaimTypes.Email));
        var displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = $"{provider} 사용자";
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var userByEmail = await FindByEmailAsync(email);
            if (userByEmail is not null)
            {
                return userByEmail;
            }
        }
        else
        {
            email = BuildPseudoEmail(provider, identityId);
        }

        var user = new VmsUser
        {
            Id = userId,
            Email = email,
            DisplayName = displayName.Trim(),
            PublicSlug = await UniqueSlugAsync(displayName),
            PasswordHash = "",
            PasswordSalt = "",
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO users (id, email, display_name, public_slug, password_hash, password_salt, created_at)
            VALUES ($id, $email, $dn, $slug, $hash, $salt, $ca)
            """;
        cmd.Parameters.AddWithValue("$id", user.Id);
        cmd.Parameters.AddWithValue("$email", user.Email);
        cmd.Parameters.AddWithValue("$dn", user.DisplayName);
        cmd.Parameters.AddWithValue("$slug", user.PublicSlug);
        cmd.Parameters.AddWithValue("$hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("$salt", user.PasswordSalt);
        cmd.Parameters.AddWithValue("$ca", user.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync();

        return user;
    }

    public async Task<(bool Ok, string Error, VmsUser? User)> SetAgentPasswordAsync(
        string userId,
        string password,
        string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return (false, "에이전트 연결 비밀번호는 8자 이상이어야 합니다.", null);
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "비밀번호 확인이 일치하지 않습니다.", null);
        }

        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "사용자를 찾을 수 없습니다.", null);
        }

        var salt = GenerateSalt();
        var hash = HashPassword(password, salt);

        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET password_hash = $hash, password_salt = $salt WHERE id = $id";
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$salt", salt);
        cmd.Parameters.AddWithValue("$id", userId);
        await cmd.ExecuteNonQueryAsync();

        return (true, "", await FindByIdAsync(userId));
    }

    public async Task<VmsUser?> FindByIdAsync(string id)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE id = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id);
        return await ReadOneAsync(cmd);
    }

    /// <summary>공개 카메라(is_public=1, enabled=1)가 있는 유저 목록을 반환합니다.</summary>
    public List<VmsUser> GetUsersWithPublicCameras()
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT u.*
            FROM users u
            INNER JOIN cameras c ON c.tenant_id = u.id
            WHERE c.is_public = 1 AND c.enabled = 1
            ORDER BY u.display_name
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<VmsUser>();
        while (reader.Read())
            list.Add(ReadUser(reader));
        return list;
    }

    public async Task<VmsUser?> FindBySlugAsync(string slug)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE public_slug = $slug LIMIT 1";
        cmd.Parameters.AddWithValue("$slug", slug.ToLowerInvariant());
        return await ReadOneAsync(cmd);
    }

    private async Task<VmsUser?> FindByEmailAsync(string email)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE email = $email LIMIT 1";
        cmd.Parameters.AddWithValue("$email", email);
        return await ReadOneAsync(cmd);
    }

    public async Task SetLayoutAsync(string userId, string layout)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET live_layout = $layout WHERE id = $id";
        cmd.Parameters.AddWithValue("$layout", layout);
        cmd.Parameters.AddWithValue("$id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateOgAsync(string userId, string ogTitle, string ogDescription, string ogImage)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE users SET og_title = $t, og_description = $d, og_image = $i WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$t", ogTitle.Trim());
        cmd.Parameters.AddWithValue("$d", ogDescription.Trim());
        cmd.Parameters.AddWithValue("$i", ogImage.Trim());
        cmd.Parameters.AddWithValue("$id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<VmsUser?> ReadOneAsync(SqliteCommand cmd)
    {
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return ReadUser(reader);
    }

    private static VmsUser ReadUser(SqliteDataReader r)
    {
        string Col(string name) => r.IsDBNull(r.GetOrdinal(name)) ? "" : r.GetString(r.GetOrdinal(name));
        var layoutOrd = r.GetOrdinal("live_layout");
        return new VmsUser
        {
            Id = r.GetString(r.GetOrdinal("id")),
            Email = r.GetString(r.GetOrdinal("email")),
            DisplayName = r.GetString(r.GetOrdinal("display_name")),
            PublicSlug = r.GetString(r.GetOrdinal("public_slug")),
            PasswordHash = r.GetString(r.GetOrdinal("password_hash")),
            PasswordSalt = r.GetString(r.GetOrdinal("password_salt")),
            CreatedAt = DateTimeOffset.Parse(r.GetString(r.GetOrdinal("created_at"))),
            LiveLayout = r.IsDBNull(layoutOrd) ? "auto" : r.GetString(layoutOrd),
            OgTitle = Col("og_title"),
            OgDescription = Col("og_description"),
            OgImage = Col("og_image")
        };
    }

    private async Task<string> UniqueSlugAsync(string displayName)
    {
        var base_ = SlugFrom(displayName);
        var candidate = base_;
        int n = 2;
        while (await FindBySlugAsync(candidate) is not null)
            candidate = $"{base_}{n++}";
        return candidate;
    }

    private static string SlugFrom(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_') sb.Append('-');
        }
        var s = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(s) ? "user" : s;
    }

    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? "" : email.Trim().ToLowerInvariant();

    private static string BuildPseudoEmail(string provider, string identityId)
    {
        var safeProvider = SlugFrom(provider);
        var safeId = SlugFrom(identityId);
        return $"{safeProvider}-{safeId}@agent.codemaru.local";
    }

    private static string GenerateSalt() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    private static string HashPassword(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
