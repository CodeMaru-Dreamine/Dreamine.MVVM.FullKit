using System.IO;
using Microsoft.Data.Sqlite;

namespace DreamineVMS.Services.Auth;

/// <summary>
/// \brief VMS SQLite 데이터베이스 초기화 및 연결 팩토리입니다.
/// </summary>
public sealed class VmsDatabase
{
    private readonly string _connectionString;

    public VmsDatabase()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "vms.db");
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    public SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS users (
                id           TEXT PRIMARY KEY,
                email        TEXT UNIQUE NOT NULL,
                display_name TEXT NOT NULL,
                public_slug  TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                password_salt TEXT NOT NULL,
                created_at   TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS cameras (
                id             TEXT PRIMARY KEY,
                tenant_id      TEXT NOT NULL,
                name           TEXT NOT NULL,
                host           TEXT NOT NULL,
                rtsp_url       TEXT NOT NULL,
                display_order  INTEGER NOT NULL DEFAULT 0,
                enabled        INTEGER NOT NULL DEFAULT 1,
                auto_reconnect INTEGER NOT NULL DEFAULT 1,
                is_public      INTEGER NOT NULL DEFAULT 0
            );
            """;
        cmd.ExecuteNonQuery();

        // 기존 DB에 is_public 컬럼이 없으면 추가 (마이그레이션)
        using var alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE cameras ADD COLUMN is_public INTEGER NOT NULL DEFAULT 0";
        try { alter.ExecuteNonQuery(); } catch { /* 이미 있으면 무시 */ }
    }
}
