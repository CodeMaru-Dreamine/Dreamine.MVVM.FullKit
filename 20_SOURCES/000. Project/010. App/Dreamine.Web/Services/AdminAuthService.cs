using System.IO;
using System.Text.Json;
using Dreamine.Identity;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Admin Auth Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates admin auth service functionality and related state.</para>
/// \endif
/// </summary>
public class AdminAuthService
{
    /// <summary>
    /// \if KO
    /// <para>file Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the file path value.</para>
    /// \endif
    /// </summary>
    private readonly string _filePath;
    /// <summary>
    /// \if KO
    /// <para>fallback Password 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the fallback password value.</para>
    /// \endif
    /// </summary>
    private readonly string _fallbackPassword;
    /// <summary>
    /// \if KO
    /// <para>override Password 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the override password value.</para>
    /// \endif
    /// </summary>
    private string? _overridePassword;

    /// <summary>
    /// \if KO
    /// <para>json 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="AdminAuthService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AdminAuthService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public AdminAuthService(DreamineOptions opts)
    {
        _fallbackPassword = opts.SuperAdminPassword;
        _filePath = Path.Combine(opts.ResolvedDataPath, "admin_auth.json");

        if (File.Exists(_filePath))
        {
            try
            {
                var doc = JsonSerializer.Deserialize<AdminAuthData>(File.ReadAllText(_filePath));
                _overridePassword = doc?.Password;
            }
            catch { }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Verify 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the verify operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Verify 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the verify condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool Verify(string password)
    {
        var storedPassword = _overridePassword ?? _fallbackPassword;
        var verification = DreaminePasswordHasher.VerifyPassword(password, storedPassword, out var upgradedHash);
        if (verification is PasswordHashVerificationResult.Failed)
        {
            return false;
        }

        if (_overridePassword is not null &&
            verification is PasswordHashVerificationResult.SuccessRehashNeeded &&
            upgradedHash is not null)
        {
            _overridePassword = upgradedHash;
            WriteOverridePassword(upgradedHash);
        }

        return true;
    }

    /// <summary>
    /// \if KO
    /// <para>Change Password Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the change password async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="newPassword">
    /// \if KO
    /// <para>new Password에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for new password.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Change Password Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the change password async operation.</para>
    /// \endif
    /// </returns>
    public async Task ChangePasswordAsync(string newPassword)
    {
        _overridePassword = DreaminePasswordHasher.HashPassword(newPassword);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath,
            JsonSerializer.Serialize(new AdminAuthData { Password = _overridePassword }, _json));
    }

    /// <summary>
    /// \if KO
    /// <para>Override Password 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes override password data.</para>
    /// \endif
    /// </summary>
    /// <param name="passwordHash">
    /// \if KO
    /// <para>password Hash에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for password hash.</para>
    /// \endif
    /// </param>
    private void WriteOverridePassword(string passwordHash)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(new AdminAuthData { Password = passwordHash }, _json));
    }

    /// <summary>
    /// \if KO
    /// <para>Admin Auth Data 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates admin auth data functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class AdminAuthData
    {
        /// <summary>
        /// \if KO
        /// <para>Password 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the password value.</para>
        /// \endif
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
