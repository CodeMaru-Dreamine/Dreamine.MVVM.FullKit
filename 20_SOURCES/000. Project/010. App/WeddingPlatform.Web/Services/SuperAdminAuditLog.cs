using System.IO;
using System.Text.Json;

namespace WeddingPlatform.Services;

/// <summary>
/// 슈퍼관리자의 테넌트 관리 접근과 주요 저장 작업을 서버 로컬 JSONL 파일에 기록합니다.
/// </summary>
public interface ISuperAdminAuditLog
{
    Task WriteAsync(
        string action,
        string slug,
        string? detail = null,
        CancellationToken ct = default);
}

/// <summary>
/// App_Data/Audit/super-admin.jsonl에 감사 레코드를 순차적으로 저장합니다.
/// </summary>
public sealed class SuperAdminAuditLog : ISuperAdminAuditLog
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SuperAdminAuditLog(WeddingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var appDataRoot = Directory.GetParent(
                Path.TrimEndingDirectorySeparator(options.ResolvedDataPath))
            ?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve the App_Data parent of '{options.ResolvedDataPath}'.");
        _logPath = Path.Combine(
            appDataRoot,
            "Audit",
            "super-admin.jsonl");
    }

    public async Task WriteAsync(
        string action,
        string slug,
        string? detail = null,
        CancellationToken ct = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_logPath)!;
            Directory.CreateDirectory(directory);

            var record = JsonSerializer.Serialize(new
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Actor = "SuperAdmin",
                Action = action,
                Slug = slug,
                Detail = detail ?? ""
            });

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await File.AppendAllTextAsync(
                    _logPath,
                    record + Environment.NewLine,
                    ct).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await Console.Error.WriteLineAsync(
                    $"[SuperAdminAuditLog] Failed to write audit record: {ex.Message}")
                .ConfigureAwait(false);
        }
    }
}
