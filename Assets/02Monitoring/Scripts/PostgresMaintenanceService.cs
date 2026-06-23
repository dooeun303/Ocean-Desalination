using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// PostgreSQL(aaa 스키마)에서 유지관리 데이터를 비동기로 조회하는 서비스.
/// </summary>
public class PostgresMaintenanceService
{
    private readonly string _connectionString;

    public PostgresMaintenanceService(string host, int port, string database, string user, string password, string schema)
    {
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
    /// <summary>
    /// 검색어로 작업 ID, 설비명, 이상 원인, 할당 매뉴얼, 담당자명, 상태 등을 통합 부분 검색한다.
    /// </summary>
    public async Task<List<MaintenanceRecord>> SearchMaintenancesAsync(string keyword, int limit = 200)
    {
        var results = new List<MaintenanceRecord>();
        keyword = keyword == null ? "" : keyword.Trim();
        string like = "%" + keyword + "%";

        const string sql = @"
SELECT ml.maintenance_id,
       COALESCE(ml.maintenance_completed_at, ml.maintenance_scheduled_at, NOW()) AS completed_or_scheduled_at,
       ml.maintenance_scheduled_at,
       ml.maintenance_completed_at,
       e.equipment_name,
       COALESCE(ml.maintenance_description, '') AS description,
       COALESCE(m.manual_title, '미정') AS manual_title,
       COALESCE(t.technician_name, ml.technician_id::text, '미지정') AS technician_name,
       COALESCE(ml.maintenance_status, 'scheduled') AS status
FROM aaa.maintenance ml
JOIN aaa.equipment e ON ml.equipment_id = e.equipment_id
LEFT JOIN aaa.manual m ON ml.manual_id = m.manual_id
LEFT JOIN aaa.technician t ON ml.technician_id = t.technician_id
WHERE (@kw = '')
   OR ml.maintenance_id::text ILIKE @like
   OR e.equipment_name ILIKE @like
   OR COALESCE(ml.maintenance_description, '') ILIKE @like
   OR COALESCE(m.manual_title, '') ILIKE @like
   OR COALESCE(t.technician_name, ml.technician_id::text, '') ILIKE @like
   OR (CASE 
         WHEN ml.maintenance_status = 'scheduled' THEN '대기'
         WHEN ml.maintenance_status = 'in_progress' THEN '진행중'
         WHEN ml.maintenance_status = 'completed' THEN '완료'
         WHEN ml.maintenance_status = 'cancelled' THEN '취소'
         ELSE '대기'
       END) ILIKE @like
ORDER BY COALESCE(ml.maintenance_completed_at, ml.maintenance_scheduled_at, NOW()) DESC
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
                        results.Add(new MaintenanceRecord
                        {
                            Id = reader.IsDBNull(0) ? "" : reader.GetInt64(0).ToString(),
                            Date = reader.GetDateTime(1),
                            ScheduledAt = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2),
                            CompletedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            EquipmentName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            ManualTitle = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            TechnicianName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Status = reader.IsDBNull(8) ? "" : reader.GetString(8)
                        });
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 유지관리 데이터를 수정합니다. (작업 타입, 스케줄, 담당자, 매뉴얼 등)
    /// </summary>
    public async Task<bool> UpdateMaintenanceAsync(string id, string equipmentId, string manualId, string workType, string technicianId, string status, System.DateTime scheduledAt, System.DateTime? completedAt)
    {
        string sql = @"
UPDATE aaa.maintenance 
SET equipment_id = @equipment_id::bigint,
    manual_id = @manual_id::bigint,
    maintenance_work_type = @work_type,
    technician_id = @technician_id::bigint,
    maintenance_status = @status,
    maintenance_scheduled_at = @scheduled_at::timestamptz,
    maintenance_completed_at = @completed_at::timestamptz
WHERE maintenance_id = @id::bigint;";

        using (var conn = new Npgsql.NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("id", long.Parse(id));
                cmd.Parameters.AddWithValue("equipment_id", long.Parse(equipmentId));
                cmd.Parameters.AddWithValue("manual_id", string.IsNullOrEmpty(manualId) ? (object)System.DBNull.Value : long.Parse(manualId));
                cmd.Parameters.AddWithValue("work_type", workType);
                cmd.Parameters.AddWithValue("technician_id", string.IsNullOrEmpty(technicianId) ? (object)System.DBNull.Value : long.Parse(technicianId));
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("scheduled_at", scheduledAt);

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
        }
    }

    /// <summary>
    /// 유지관리 데이터를 삭제합니다.
    /// </summary>
    public async Task<bool> DeleteMaintenanceAsync(string id)
    {
        string sql = "DELETE FROM aaa.maintenance WHERE maintenance_id = @id::bigint;";
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
