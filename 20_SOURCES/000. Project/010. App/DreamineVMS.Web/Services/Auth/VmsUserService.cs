using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Dreamine.Identity;
using DreamineVMS.Web.Models;
using Microsoft.Data.Sqlite;

namespace DreamineVMS.Web.Services.Auth;

/// <summary>
/// \if KO
/// <para>Vms User Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms user service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsUserService
{
    /// <summary>
    /// \if KO
    /// <para>db 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the db value.</para>
    /// \endif
    /// </summary>
    private readonly VmsDatabase _db;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="VmsUserService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VmsUserService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="db">
    /// \if KO
    /// <para>db에 사용할 <c>VmsDatabase</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsDatabase</c> value used for db.</para>
    /// \endif
    /// </param>
    public VmsUserService(VmsDatabase db) => _db = db;

    /// <summary>
    /// \if KO
    /// <para>Register Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="email">
    /// \if KO
    /// <para>email에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for email.</para>
    /// \endif
    /// </param>
    /// <param name="displayName">
    /// \if KO
    /// <para>display Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for display name.</para>
    /// \endif
    /// </param>
    /// <param name="password">
    /// \if KO
    /// <para>password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Register Async 작업에서 생성한 <c>Task&lt;(bool Ok, string Error, VmsUser? User)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(bool Ok, string Error, VmsUser? User)&gt;</c> result produced by the register async operation.</para>
    /// \endif
    /// </returns>
    public async Task<(bool Ok, string Error, VmsUser? User)> RegisterAsync(
        string email, string displayName, string password)
    {
        email = email.Trim().ToLowerInvariant();
        displayName = displayName.Trim();

        if (string.IsNullOrWhiteSpace(email)) return (false, "이메일을 입력하세요.", null);
        if (string.IsNullOrWhiteSpace(displayName)) return (false, "이름을 입력하세요.", null);
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return (false, "비밀번호는 8자 이상이어야 합니다.", null);

        if (await FindByEmailAsync(email) is not null)
            return (false, "이미 사용 중인 이메일입니다.", null);

        var hash = DreaminePasswordHasher.HashPassword(password);
        var slug = await UniqueSlugAsync(displayName);

        var user = new VmsUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = email,
            DisplayName = displayName,
            PublicSlug = slug,
            PasswordHash = hash,
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

        return (true, "", user);
    }

    /// <summary>
    /// \if KO
    /// <para>Login Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="email">
    /// \if KO
    /// <para>email에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for email.</para>
    /// \endif
    /// </param>
    /// <param name="password">
    /// \if KO
    /// <para>password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Login Async 작업에서 생성한 <c>Task&lt;(bool Ok, string Error, VmsUser? User)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(bool Ok, string Error, VmsUser? User)&gt;</c> result produced by the login async operation.</para>
    /// \endif
    /// </returns>
    public async Task<(bool Ok, string Error, VmsUser? User)> LoginAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await FindByEmailAsync(email);
        if (user is null) return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return (false, "에이전트 연결 비밀번호가 아직 설정되지 않았습니다.", null);

        var verification = DreaminePasswordHasher.VerifyPassword(password, user.PasswordHash, out var upgradedHash);
        if (verification is PasswordHashVerificationResult.Failed &&
            !string.IsNullOrWhiteSpace(user.PasswordSalt) &&
            VerifyLegacySaltedSha256(password, user.PasswordSalt, user.PasswordHash))
        {
            verification = PasswordHashVerificationResult.SuccessRehashNeeded;
            upgradedHash = DreaminePasswordHasher.HashPassword(password);
        }

        if (verification is PasswordHashVerificationResult.Failed)
            return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);

        if (verification is PasswordHashVerificationResult.SuccessRehashNeeded && upgradedHash is not null)
        {
            await UpdatePasswordHashAsync(user.Id, upgradedHash, "");
            user = WithPasswordHash(user, upgradedHash, "");
        }

        return (true, "", user);
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure External User Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure external user async operation.</para>
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
    /// <para>Ensure External User Async 작업에서 생성한 <c>Task&lt;VmsUser?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;VmsUser?&gt;</c> result produced by the ensure external user async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Agent Password Async 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the agent password async value.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="password">
    /// \if KO
    /// <para>password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password.</para>
    /// \endif
    /// </param>
    /// <param name="confirmPassword">
    /// \if KO
    /// <para>confirm Password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for confirm password.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Set Agent Password Async 작업에서 생성한 <c>Task&lt;(bool Ok, string Error, VmsUser? User)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(bool Ok, string Error, VmsUser? User)&gt;</c> result produced by the set agent password async operation.</para>
    /// \endif
    /// </returns>
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

        var hash = DreaminePasswordHasher.HashPassword(password);

        await UpdatePasswordHashAsync(userId, hash, "");

        return (true, "", await FindByIdAsync(userId));
    }

    /// <summary>
    /// \if KO
    /// <para>By Id Async 항목을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the by id async item.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Find By Id Async 작업에서 생성한 <c>Task&lt;VmsUser?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;VmsUser?&gt;</c> result produced by the find by id async operation.</para>
    /// \endif
    /// </returns>
    public async Task<VmsUser?> FindByIdAsync(string id)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE id = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id);
        return await ReadOneAsync(cmd);
    }

    /// <summary>
    /// \if KO
    /// <para>공개 카메라(is_public=1, enabled=1)가 있는 유저 목록을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the users with public cameras value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Users With Public Cameras 작업에서 생성한 <c>List&lt;VmsUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;VmsUser&gt;</c> result produced by the get users with public cameras operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>By Slug Async 항목을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the by slug async item.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Find By Slug Async 작업에서 생성한 <c>Task&lt;VmsUser?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;VmsUser?&gt;</c> result produced by the find by slug async operation.</para>
    /// \endif
    /// </returns>
    public async Task<VmsUser?> FindBySlugAsync(string slug)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE public_slug = $slug LIMIT 1";
        cmd.Parameters.AddWithValue("$slug", slug.ToLowerInvariant());
        return await ReadOneAsync(cmd);
    }

    /// <summary>
    /// \if KO
    /// <para>By Email Async 항목을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the by email async item.</para>
    /// \endif
    /// </summary>
    /// <param name="email">
    /// \if KO
    /// <para>email에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for email.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Find By Email Async 작업에서 생성한 <c>Task&lt;VmsUser?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;VmsUser?&gt;</c> result produced by the find by email async operation.</para>
    /// \endif
    /// </returns>
    private async Task<VmsUser?> FindByEmailAsync(string email)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE email = $email LIMIT 1";
        cmd.Parameters.AddWithValue("$email", email);
        return await ReadOneAsync(cmd);
    }

    /// <summary>
    /// \if KO
    /// <para>Layout Async 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the layout async value.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="layout">
    /// \if KO
    /// <para>layout에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for layout.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Set Layout Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set layout async operation.</para>
    /// \endif
    /// </returns>
    public async Task SetLayoutAsync(string userId, string layout)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET live_layout = $layout WHERE id = $id";
        cmd.Parameters.AddWithValue("$layout", layout);
        cmd.Parameters.AddWithValue("$id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>Update Og Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update og async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="ogTitle">
    /// \if KO
    /// <para>og Title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for og title.</para>
    /// \endif
    /// </param>
    /// <param name="ogDescription">
    /// \if KO
    /// <para>og Description에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for og description.</para>
    /// \endif
    /// </param>
    /// <param name="ogImage">
    /// \if KO
    /// <para>og Image에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for og image.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Update Og Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the update og async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>One Async 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads one async data.</para>
    /// \endif
    /// </summary>
    /// <param name="cmd">
    /// \if KO
    /// <para>cmd에 사용할 <c>SqliteCommand</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SqliteCommand</c> value used for cmd.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Read One Async 작업에서 생성한 <c>Task&lt;VmsUser?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;VmsUser?&gt;</c> result produced by the read one async operation.</para>
    /// \endif
    /// </returns>
    private static async Task<VmsUser?> ReadOneAsync(SqliteCommand cmd)
    {
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return ReadUser(reader);
    }

    /// <summary>
    /// \if KO
    /// <para>User 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads user data.</para>
    /// \endif
    /// </summary>
    /// <param name="r">
    /// \if KO
    /// <para>r에 사용할 <c>SqliteDataReader</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SqliteDataReader</c> value used for r.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Read User 작업에서 생성한 <c>VmsUser</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser</c> result produced by the read user operation.</para>
    /// \endif
    /// </returns>
    private static VmsUser ReadUser(SqliteDataReader r)
    {
        #pragma warning disable CS1587
        /// \cond LOCAL_FUNCTION_DOCUMENTATION
        /// <summary>
        /// \if KO
        /// <para>현재 데이터 행에서 이름으로 문자열 열을 안전하게 읽습니다.</para>
        /// \endif
        /// \if EN
        /// <para>Safely reads a string column by name from the current data row.</para>
        /// \endif
        /// </summary>
        /// <param name="name">
        /// \if KO
        /// <para>읽을 열 이름입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The name of the column to read.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>열 값이며, 데이터베이스 값이 null이면 빈 문자열입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The column value, or an empty string when the database value is null.</para>
        /// \endif
        /// </returns>
        /// \endcond
        string Col(string name) => r.IsDBNull(r.GetOrdinal(name)) ? "" : r.GetString(r.GetOrdinal(name));
        #pragma warning restore CS1587
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

    /// <summary>
    /// \if KO
    /// <para>With Password Hash 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the with password hash operation.</para>
    /// \endif
    /// </summary>
    /// <param name="source">
    /// \if KO
    /// <para>source에 사용할 <c>VmsUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser</c> value used for source.</para>
    /// \endif
    /// </param>
    /// <param name="passwordHash">
    /// \if KO
    /// <para>password Hash에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password hash.</para>
    /// \endif
    /// </param>
    /// <param name="passwordSalt">
    /// \if KO
    /// <para>password Salt에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password salt.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>With Password Hash 작업에서 생성한 <c>VmsUser</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VmsUser</c> result produced by the with password hash operation.</para>
    /// \endif
    /// </returns>
    private static VmsUser WithPasswordHash(VmsUser source, string passwordHash, string passwordSalt) => new()
    {
        Id = source.Id,
        Email = source.Email,
        DisplayName = source.DisplayName,
        PublicSlug = source.PublicSlug,
        PasswordHash = passwordHash,
        PasswordSalt = passwordSalt,
        CreatedAt = source.CreatedAt,
        LiveLayout = source.LiveLayout,
        OgTitle = source.OgTitle,
        OgDescription = source.OgDescription,
        OgImage = source.OgImage
    };

    /// <summary>
    /// \if KO
    /// <para>Unique Slug Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unique slug async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="displayName">
    /// \if KO
    /// <para>display Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for display name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unique Slug Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the unique slug async operation.</para>
    /// \endif
    /// </returns>
    private async Task<string> UniqueSlugAsync(string displayName)
    {
        var base_ = SlugFrom(displayName);
        var candidate = base_;
        int n = 2;
        while (await FindBySlugAsync(candidate) is not null)
            candidate = $"{base_}{n++}";
        return candidate;
    }

    /// <summary>
    /// \if KO
    /// <para>Slug From 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the slug from operation.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Slug From 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the slug from operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Normalize Email 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize email operation.</para>
    /// \endif
    /// </summary>
    /// <param name="email">
    /// \if KO
    /// <para>email에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for email.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Email 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize email operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? "" : email.Trim().ToLowerInvariant();

    /// <summary>
    /// \if KO
    /// <para>Pseudo Email 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the pseudo email value.</para>
    /// \endif
    /// </summary>
    /// <param name="provider">
    /// \if KO
    /// <para>provider에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for provider.</para>
    /// \endif
    /// </param>
    /// <param name="identityId">
    /// \if KO
    /// <para>identity Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for identity id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Pseudo Email 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build pseudo email operation.</para>
    /// \endif
    /// </returns>
    private static string BuildPseudoEmail(string provider, string identityId)
    {
        var safeProvider = SlugFrom(provider);
        var safeId = SlugFrom(identityId);
        return $"{safeProvider}-{safeId}@agent.codemaru.local";
    }

    /// <summary>
    /// \if KO
    /// <para>Update Password Hash Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update password hash async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="hash">
    /// \if KO
    /// <para>hash에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for hash.</para>
    /// \endif
    /// </param>
    /// <param name="salt">
    /// \if KO
    /// <para>salt에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for salt.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Update Password Hash Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the update password hash async operation.</para>
    /// \endif
    /// </returns>
    private async Task UpdatePasswordHashAsync(string userId, string hash, string salt)
    {
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET password_hash = $hash, password_salt = $salt WHERE id = $id";
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$salt", salt);
        cmd.Parameters.AddWithValue("$id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>Verify Legacy Salted Sha256 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the verify legacy salted sha256 operation.</para>
    /// \endif
    /// </summary>
    /// <param name="password">
    /// \if KO
    /// <para>password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password.</para>
    /// \endif
    /// </param>
    /// <param name="salt">
    /// \if KO
    /// <para>salt에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for salt.</para>
    /// \endif
    /// </param>
    /// <param name="storedHash">
    /// \if KO
    /// <para>stored Hash에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for stored hash.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Verify Legacy Salted Sha256 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the verify legacy salted sha256 condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool VerifyLegacySaltedSha256(string password, string salt, string storedHash)
    {
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return string.Equals(actual, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
