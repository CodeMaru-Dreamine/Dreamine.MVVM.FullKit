using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public class AdminAuthService
{
    private readonly string _filePath;
    private readonly string _fallbackPassword;
    private string? _overridePassword;

    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

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

    public bool Verify(string password) =>
        password == (_overridePassword ?? _fallbackPassword);

    public async Task ChangePasswordAsync(string newPassword)
    {
        _overridePassword = newPassword;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath,
            JsonSerializer.Serialize(new AdminAuthData { Password = newPassword }, _json));
    }

    private sealed class AdminAuthData
    {
        public string Password { get; set; } = string.Empty;
    }
}
