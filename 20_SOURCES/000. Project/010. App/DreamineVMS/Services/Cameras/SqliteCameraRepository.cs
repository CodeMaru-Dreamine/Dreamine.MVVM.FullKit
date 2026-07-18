using System.IO;
using DreamineVMS.Models;
using DreamineVMS.Services.Auth;
using Microsoft.Data.Sqlite;

namespace DreamineVMS.Services.Cameras;

/// <summary>
/// \if KO
/// <para>\brief SQLite 기반 멀티테넌트 카메라 저장소입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates sqlite camera repository functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SqliteCameraRepository : IVmsCameraRepository
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
    /// <para>lock 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the lock value.</para>
    /// \endif
    /// </summary>
    private readonly SemaphoreSlim _lock = new(1, 1);
    /// <summary>
    /// \if KO
    /// <para>cache 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cache value.</para>
    /// \endif
    /// </summary>
    private List<CameraDevice> _cache;

    /// <summary>
    /// \if KO
    /// <para>Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler<CameraRepositoryChangedEventArgs>? Changed;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="SqliteCameraRepository"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SqliteCameraRepository"/> class with the specified settings.</para>
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
    public SqliteCameraRepository(VmsDatabase db)
    {
        _db = db;
        _cache = LoadAll();
        MigrateFromJson();
    }

    /// <summary>
    /// \if KO
    /// <para>All 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All 작업에서 생성한 <c>IReadOnlyList&lt;CameraDevice&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;CameraDevice&gt;</c> result produced by the get all operation.</para>
    /// \endif
    /// </returns>
    public IReadOnlyList<CameraDevice> GetAll() => _cache.AsReadOnly();

    /// <summary>
    /// \if KO
    /// <para>By Tenant 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the by tenant value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenantId">
    /// \if KO
    /// <para>tenant Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tenant id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get By Tenant 작업에서 생성한 <c>IReadOnlyList&lt;CameraDevice&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;CameraDevice&gt;</c> result produced by the get by tenant operation.</para>
    /// \endif
    /// </returns>
    public IReadOnlyList<CameraDevice> GetByTenant(string tenantId) =>
        _cache.Where(c => c.TenantId == tenantId).ToList().AsReadOnly();

    /// <summary>
    /// \if KO
    /// <para>Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the async item.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Add Async 작업에서 생성한 <c>Task&lt;CameraDevice&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CameraDevice&gt;</c> result produced by the add async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Update Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="camera">
    /// \if KO
    /// <para>camera에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for camera.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Update Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the update async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
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
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>All 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads all data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Load All 작업에서 생성한 <c>List&lt;CameraDevice&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;CameraDevice&gt;</c> result produced by the load all operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Bind Camera 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the bind camera operation.</para>
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
    /// <param name="c">
    /// \if KO
    /// <para>c에 사용할 <c>CameraDevice</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> value used for c.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Camera 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads camera data.</para>
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
    /// <para>Read Camera 작업에서 생성한 <c>CameraDevice</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CameraDevice</c> result produced by the read camera operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>cameras.json이 있으면 SQLite로 마이그레이션합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the migrate from json operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Json Cam 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates json cam functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class JsonCam
    {
        /// <summary>
        /// \if KO
        /// <para>Id 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the id value.</para>
        /// \endif
        /// </summary>
        public string? Id { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Name 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the name value.</para>
        /// \endif
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Host 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the host value.</para>
        /// \endif
        /// </summary>
        public string? Host { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Rtsp Url 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the rtsp url value.</para>
        /// \endif
        /// </summary>
        public string? RtspUrl { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Display Order 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the display order value.</para>
        /// \endif
        /// </summary>
        public int DisplayOrder { get; set; }
        /// <summary>
        /// \if KO
        /// <para>Enabled 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the enabled value.</para>
        /// \endif
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// \if KO
        /// <para>Auto Reconnect 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the auto reconnect value.</para>
        /// \endif
        /// </summary>
        public bool AutoReconnect { get; set; } = true;
    }
}
