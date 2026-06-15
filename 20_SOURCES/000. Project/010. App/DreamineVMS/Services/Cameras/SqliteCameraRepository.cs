using System.IO;
using DreamineVMS.Models;
using DreamineVMS.Services.Auth;
using Microsoft.Data.Sqlite;

namespace DreamineVMS.Services.Cameras;

/// <summary>
/// \brief SQLite 기반 멀티테넌트 카메라 저장소입니다.
/// </summary>
public sealed class SqliteCameraRepository : IVmsCameraRepository
{
    private readonly VmsDatabase _db;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<CameraDevice> _cache;

    public event EventHandler<CameraRepositoryChangedEventArgs>? Changed;

    public SqliteCameraRepository(VmsDatabase db)
    {
        _db = db;
        _cache = LoadAll();
        MigrateFromJson();
    }

    public IReadOnlyList<CameraDevice> GetAll() => _cache.AsReadOnly();

    public IReadOnlyList<CameraDevice> GetByTenant(string tenantId) =>
        _cache.Where(c => c.TenantId == tenantId).ToList().AsReadOnly();

    public async Task<CameraDevice> AddAsync(CameraDevice camera)
    {
        await _lock.WaitAsync();
        try
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO cameras (id, tenant_id, name, host, rtsp_url, display_order, enabled, auto_reconnect, is_public)
                VALUES ($id, $tid, $name, $host, $rtsp, $order, $enabled, $ar, $pub)
                """;
            BindCamera(cmd, camera);
            await cmd.ExecuteNonQueryAsync();
            _cache = LoadAll();
        }
        finally { _lock.Release(); }

        Changed?.Invoke(this, new CameraRepositoryChangedEventArgs
        {
            CameraId = camera.Id, Kind = CameraRepositoryChangeKind.Added, Camera = camera
        });
        return camera;
    }

    public async Task UpdateAsync(CameraDevice camera)
    {
        await _lock.WaitAsync();
        try
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE cameras
                SET tenant_id=$tid, name=$name, host=$host, rtsp_url=$rtsp,
                    display_order=$order, enabled=$enabled, auto_reconnect=$ar, is_public=$pub
                WHERE id=$id
                """;
            BindCamera(cmd, camera);
            await cmd.ExecuteNonQueryAsync();
            _cache = LoadAll();
        }
        finally { _lock.Release(); }

        Changed?.Invoke(this, new CameraRepositoryChangedEventArgs
        {
            CameraId = camera.Id, Kind = CameraRepositoryChangeKind.Updated, Camera = camera
        });
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            using var conn = _db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cameras WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            await cmd.ExecuteNonQueryAsync();
            _cache = LoadAll();
        }
        finally { _lock.Release(); }

        Changed?.Invoke(this, new CameraRepositoryChangedEventArgs
        {
            CameraId = id, Kind = CameraRepositoryChangeKind.Deleted
        });
    }

    // ── private ──────────────────────────────────────────────────────────────

    private List<CameraDevice> LoadAll()
    {
        var list = new List<CameraDevice>();
        using var conn = _db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM cameras ORDER BY display_order";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(ReadCamera(reader));
        return list;
    }

    private static void BindCamera(SqliteCommand cmd, CameraDevice c)
    {
        cmd.Parameters.AddWithValue("$id", c.Id);
        cmd.Parameters.AddWithValue("$tid", c.TenantId);
        cmd.Parameters.AddWithValue("$name", c.Name);
        cmd.Parameters.AddWithValue("$host", c.Host);
        cmd.Parameters.AddWithValue("$rtsp", c.RtspUrl);
        cmd.Parameters.AddWithValue("$order", c.DisplayOrder);
        cmd.Parameters.AddWithValue("$enabled", c.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$ar", c.AutoReconnect ? 1 : 0);
        cmd.Parameters.AddWithValue("$pub", c.IsPublic ? 1 : 0);
    }

    private static CameraDevice ReadCamera(SqliteDataReader r) => new()
    {
        Id = r.GetString(r.GetOrdinal("id")),
        TenantId = r.GetString(r.GetOrdinal("tenant_id")),
        Name = r.GetString(r.GetOrdinal("name")),
        Host = r.GetString(r.GetOrdinal("host")),
        RtspUrl = r.GetString(r.GetOrdinal("rtsp_url")),
        DisplayOrder = r.GetInt32(r.GetOrdinal("display_order")),
        Enabled = r.GetInt32(r.GetOrdinal("enabled")) == 1,
        AutoReconnect = r.GetInt32(r.GetOrdinal("auto_reconnect")) == 1,
        IsPublic = r.GetInt32(r.GetOrdinal("is_public")) == 1
    };

    /// <summary>cameras.json이 있으면 SQLite로 마이그레이션합니다.</summary>
    private void MigrateFromJson()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "cameras.json");
        if (!File.Exists(jsonPath) || _cache.Count > 0) return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            var stored = System.Text.Json.JsonSerializer.Deserialize<List<JsonCam>>(json) ?? [];
            foreach (var cam in stored)
            {
                using var conn = _db.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    INSERT OR IGNORE INTO cameras (id, tenant_id, name, host, rtsp_url, display_order, enabled, auto_reconnect, is_public)
                    VALUES ($id, '', $name, $host, $rtsp, $order, $enabled, $ar, 0)
                    """;
                cmd.Parameters.AddWithValue("$id", cam.Id ?? Guid.NewGuid().ToString("N"));
                cmd.Parameters.AddWithValue("$name", cam.Name ?? "");
                cmd.Parameters.AddWithValue("$host", cam.Host ?? "");
                cmd.Parameters.AddWithValue("$rtsp", cam.RtspUrl ?? "");
                cmd.Parameters.AddWithValue("$order", cam.DisplayOrder);
                cmd.Parameters.AddWithValue("$enabled", cam.Enabled ? 1 : 0);
                cmd.Parameters.AddWithValue("$ar", cam.AutoReconnect ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
            _cache = LoadAll();
            File.Move(jsonPath, jsonPath + ".migrated");
        }
        catch { /* 마이그레이션 실패 시 무시 */ }
    }

    private sealed class JsonCam
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Host { get; set; }
        public string? RtspUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool Enabled { get; set; } = true;
        public bool AutoReconnect { get; set; } = true;
    }
}
