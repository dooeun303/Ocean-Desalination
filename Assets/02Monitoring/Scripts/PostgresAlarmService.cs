using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// PostgreSQL(aaa 스키마)에서 고장예지 알람 데이터를 비동기로 조회하는 서비스.
/// Unity 메인 스레드에서 호출하면 await 이후 컨티뉴에이션도 메인 스레드로 복귀한다.
/// </summary>
public class PostgresAlarmService
{
    private readonly string _connectionString;

    public PostgresAlarmService(string host, int port, string database, string user, string password, string schema)
    {
        // Unity Mono DNS 버그(Illegal byte sequence)를 우회하기 위해 localhost를 IP로 직접 변환
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
            // Unity(IL2CPP/Mono)에서 타입 로딩 리플렉션 이슈를 피하기 위한 안전 옵션
            ServerCompatibilityMode = Npgsql.ServerCompatibilityMode.NoTypeLoading
        };
        _connectionString = builder.ConnectionString;
    }

    /// <summary>
    /// 키워드로 전체 컬럼(설비명/위치/내용/알람코드/등급/상태)을 부분검색한다.
    /// 키워드가 비어 있으면 최신순 전체를 반환한다.
    /// </summary>
    public async Task<List<AlarmRecord>> SearchAlarmsAsync(string keyword, int limit = 200)
    {
        var results = new List<AlarmRecord>();
        keyword = keyword == null ? "" : keyword.Trim();
        string like = "%" + keyword + "%";

        const string sql = @"
SELECT a.alarm_id,
       a.alarm_code,
       a.created_at,
       e.equipment_name,
       COALESCE(p.plant_location, '') AS location,
       COALESCE(a.alarm_description, '') AS description,
       a.alarm_severity,
       a.alarm_is_active
FROM aaa.alarm a
JOIN aaa.equipment e ON a.equipment_id = e.equipment_id
LEFT JOIN aaa.plant p ON e.plant_id = p.plant_id
WHERE (@kw = '')
   OR e.equipment_name ILIKE @like
   OR COALESCE(p.plant_location, '') ILIKE @like
   OR COALESCE(a.alarm_description, '') ILIKE @like
   OR a.alarm_code ILIKE @like
   OR a.alarm_severity ILIKE @like
   OR (CASE WHEN a.alarm_is_active THEN '활성' ELSE '해제' END) ILIKE @like
   OR (CASE
         WHEN a.alarm_severity = 'critical' THEN '위험'
         WHEN a.alarm_severity = 'warning' THEN '경고'
         ELSE '정상'
       END) ILIKE @like
ORDER BY a.created_at DESC
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
                        results.Add(new AlarmRecord
                        {
                            Id = reader.IsDBNull(0) ? "" : reader.GetInt64(0).ToString(),
                            AlarmCode = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            TriggeredAt = reader.GetDateTime(2),
                            EquipmentName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Location = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Severity = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            IsActive = !reader.IsDBNull(7) && reader.GetBoolean(7)
                        });
                    }
                }
            }
        }

        return results;
    }
}
