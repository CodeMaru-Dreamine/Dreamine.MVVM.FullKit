using System.IO;
using Microsoft.Data.Sqlite;

namespace DreamineVMS.Services.Auth;

/// <summary>
/// \if KO
/// <para>\brief VMS SQLite 데이터베이스 초기화 및 연결 팩토리입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates vms database functionality and related state.</para>
/// \endif
/// </summary>
public sealed class VmsDatabase
{
    /// <summary>
    /// \if KO
    /// <para>connection String 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the connection string value.</para>
    /// \endif
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="VmsDatabase"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VmsDatabase"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public VmsDatabase()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "vms.db");
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    /// <summary>
    /// \if KO
    /// <para>Open 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the open operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Open 작업에서 생성한 <c>SqliteConnection</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SqliteConnection</c> result produced by the open operation.</para>
    /// \endif
    /// </returns>
    public SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>
    /// \if KO
    /// <para>Initialize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize operation.</para>
    /// \endif
    /// </summary>
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
