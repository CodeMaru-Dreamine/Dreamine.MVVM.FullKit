using System.Security.Cryptography;
using System.Text;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>슈퍼 어드민에서 개별 계정 관리 화면으로 이동할 때 사용하는 짧은 수명의 서명 토큰을 발급하고 검증합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i super admin session token service functionality and related state.</para>
/// \endif
/// </summary>
public interface ISuperAdminSessionTokenService
{
    /// <summary>
    /// \if KO
    /// <para>현재 설정의 슈퍼 어드민 비밀값으로 서명된 세션 토큰을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the token value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Token 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the create token operation.</para>
    /// \endif
    /// </returns>
    string CreateToken();

    /// <summary>
    /// \if KO
    /// <para>세션 토큰의 서명과 만료 시간을 검증합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the token value.</para>
    /// \endif
    /// </summary>
    /// <param name="token">
    /// \if KO
    /// <para>token에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Validate Token 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the validate token condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    bool ValidateToken(string? token);
}

/// <summary>
/// \if KO
/// <para>Super Admin Session Token Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates super admin session token service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SuperAdminSessionTokenService : ISuperAdminSessionTokenService
{
    /// <summary>
    /// \if KO
    /// <para>Token Lifetime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the token lifetime value.</para>
    /// \endif
    /// </summary>
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    /// <summary>
    /// \if KO
    /// <para>options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the options value.</para>
    /// \endif
    /// </summary>
    private readonly WeddingOptions _options;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="SuperAdminSessionTokenService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SuperAdminSessionTokenService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
    /// \endif
    /// </param>
    public SuperAdminSessionTokenService(WeddingOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// \if KO
    /// <para>Token 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the token value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Token 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the create token operation.</para>
    /// \endif
    /// </returns>
    public string CreateToken()
    {
        var expires = DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds();
        var payload = $"v1:{expires}:{Guid.NewGuid():N}";
        var signature = Sign(payload);
        return $"{Encode(payload)}.{Encode(signature)}";
    }

    /// <summary>
    /// \if KO
    /// <para>Token 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the token value.</para>
    /// \endif
    /// </summary>
    /// <param name="token">
    /// \if KO
    /// <para>token에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Validate Token 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the validate token condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var payload = DecodeString(parts[0]);
        var signature = DecodeBytes(parts[1]);
        if (string.IsNullOrWhiteSpace(payload) || signature.Length == 0)
        {
            return false;
        }

        var fields = payload.Split(':', 3);
        if (fields.Length != 3 || fields[0] != "v1" || !long.TryParse(fields[1], out var expires))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
        {
            return false;
        }

        var expected = Sign(payload);
        return CryptographicOperations.FixedTimeEquals(expected, signature);
    }

    /// <summary>
    /// \if KO
    /// <para>Sign 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sign operation.</para>
    /// \endif
    /// </summary>
    /// <param name="payload">
    /// \if KO
    /// <para>payload에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for payload.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sign 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the sign operation.</para>
    /// \endif
    /// </returns>
    private byte[] Sign(string payload)
    {
        var secret = string.IsNullOrWhiteSpace(_options.SuperAdminPassword)
            ? "WeddingPlatform.SuperAdminSession.EmptySecret"
            : _options.SuperAdminPassword;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("WeddingPlatform.SuperAdminSession:" + secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    /// <summary>
    /// \if KO
    /// <para>Encode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the encode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Encode 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the encode operation.</para>
    /// \endif
    /// </returns>
    private static string Encode(string value) => Encode(Encoding.UTF8.GetBytes(value));

    /// <summary>
    /// \if KO
    /// <para>Encode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the encode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Encode 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the encode operation.</para>
    /// \endif
    /// </returns>
    private static string Encode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    /// <summary>
    /// \if KO
    /// <para>Decode String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the decode string operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Decode String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the decode string operation.</para>
    /// \endif
    /// </returns>
    private static string DecodeString(string value)
    {
        var bytes = DecodeBytes(value);
        return bytes.Length == 0 ? "" : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// \if KO
    /// <para>Decode Bytes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the decode bytes operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Decode Bytes 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the decode bytes operation.</para>
    /// \endif
    /// </returns>
    private static byte[] DecodeBytes(string value)
    {
        try
        {
            var normalized = value.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            return Convert.FromBase64String(normalized);
        }
        catch
        {
            return [];
        }
    }
}
