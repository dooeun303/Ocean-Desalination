using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// PostgreSQL(aaa 스키마)에서 원격점검 데이터를 비동기로 조회하는 서비스.
/// </summary>
public class PostgresInspectionService
{
    private readonly string _connectionString;

    public PostgresInspectionService(string host, int port, string database, string user, string password, string schema)
    {
        // Unity Mono DNS 버그를 방지하기 위해 localhost 우회 처리
        if ((host ?? "").ToLowerInvariant() == "localhost")
        {
            host = "127.0.0.1";
        }

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = user,
            Password = password,
            SearchPath = string.IsNullOrEmpty(schema) ? null : schema,
            SslMode = Npgsql.SslMode.Disable,
            Timeout = 15,
            CommandTimeout = 15,
            ServerCompatibilityMode = Npgsql.ServerCompatibilityMode.NoTypeLoading
        };
        _connectionString = builder.ConnectionString;
    }

    /// <summary>
    /// 검색어로 작업 ID, 담당자명, 상태, 비고 등을 부분검색한다.
    /// </summary>
    public async Task<List<InspectionRecord>> SearchInspectionsAsync(string keyword, int limit = 200)
    {
        var results = new List<InspectionRecord>();
        keyword = keyword == null ? "" : keyword.Trim();
        string like = "%" + keyword + "%";

        const string sql = @"
SELECT r.inspection_id,
       COALESCE(r.inspection_completed_at, r.inspection_scheduled_at, NOW()) AS inspection_date,
       COALESCE(t.technician_name, r.inspector_id, '미지정') AS inspector_name,
       COALESCE(r.inspection_result, 'pending') AS result,
       COALESCE(r.inspection_note, '') AS note
FROM aaa.inspection r
LEFT JOIN aaa.technician t ON r.inspector_id = t.technician_id::text
WHERE (@kw = '')
   OR r.inspection_id::text ILIKE @like
   OR COALESCE(t.technician_name, r.inspector_id, '') ILIKE @like
   OR COALESCE(r.inspection_note, '') ILIKE @like
   OR (CASE 
         WHEN r.inspection_result = 'pending' THEN '대기'
         WHEN r.inspection_result = 'in_progress' THEN '진행중'
         ELSE '완료'
       END) ILIKE @like
ORDER BY COALESCE(r.inspection_completed_at, r.inspection_scheduled_at, NOW()) DESC
LIMIT @limit;";

        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("kw", keyword);
                cmd.Parameters.AddWithValue("like", like);
                cmd.Parameters.AddWithValue("limit", limit);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new InspectionRecord
                        {
                            Id = reader.IsDBNull(0) ? "" : reader.GetInt64(0).ToString(),
                            InspectionDate = reader.GetDateTime(1),
                            InspectorName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Result = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Note = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 원격점검 데이터를 수정합니다.
    /// </summary>
    public async Task<bool> UpdateInspectionAsync(string id, string equipmentId, string inspectorId, string result, string note, System.DateTime scheduledAt)
    {
        string sql = @"
UPDATE aaa.inspection 
SET equipment_id = @equipment_id::bigint,
    inspector_id = @inspector_id,
    inspection_result = @result,
    inspection_note = @note,
    inspection_scheduled_at = @scheduled_at::timestamptz
WHERE inspection_id = @id::bigint;";

        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("id", long.Parse(id));
                cmd.Parameters.AddWithValue("equipment_id", long.Parse(equipmentId));
                cmd.Parameters.AddWithValue("inspector_id", inspectorId);
                cmd.Parameters.AddWithValue("result", result);
                cmd.Parameters.AddWithValue("note", note);
                cmd.Parameters.AddWithValue("scheduled_at", scheduledAt);

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
        }
    }

    /// <summary>
    /// 원격점검 데이터를 삭제합니다.
    /// </summary>
    public async Task<bool> DeleteInspectionAsync(string id)
    {
        string sql = "DELETE FROM aaa.inspection WHERE inspection_id = @id::bigint;";
        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("id", long.Parse(id));
                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
        }
    }
}
