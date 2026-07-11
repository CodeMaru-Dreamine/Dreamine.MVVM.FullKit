using System.Security.Cryptography;
using System.Text;

namespace WeddingThankYou.Services;

/// <summary>슈퍼 어드민에서 개별 계정 관리 화면으로 이동할 때 사용하는 짧은 수명의 서명 토큰을 발급하고 검증합니다.</summary>
public interface ISuperAdminSessionTokenService
{
    /// <summary>현재 설정의 슈퍼 어드민 비밀값으로 서명된 세션 토큰을 생성합니다.</summary>
    string CreateToken();

    /// <summary>세션 토큰의 서명과 만료 시간을 검증합니다.</summary>
    bool ValidateToken(string? token);
}

public sealed class SuperAdminSessionTokenService : ISuperAdminSessionTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    private readonly WeddingOptions _options;

    public SuperAdminSessionTokenService(WeddingOptions options)
    {
        _options = options;
    }

    public string CreateToken()
    {
        var expires = DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds();
        var payload = $"v1:{expires}:{Guid.NewGuid():N}";
        var signature = Sign(payload);
        return $"{Encode(payload)}.{Encode(signature)}";
    }

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

    private byte[] Sign(string payload)
    {
        var secret = string.IsNullOrWhiteSpace(_options.SuperAdminPassword)
            ? "WeddingThankYou.SuperAdminSession.EmptySecret"
            : _options.SuperAdminPassword;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("WeddingThankYou.SuperAdminSession:" + secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static string Encode(string value) => Encode(Encoding.UTF8.GetBytes(value));

    private static string Encode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string DecodeString(string value)
    {
        var bytes = DecodeBytes(value);
        return bytes.Length == 0 ? "" : Encoding.UTF8.GetString(bytes);
    }

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
