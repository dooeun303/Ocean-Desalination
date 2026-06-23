using System;

/// <summary>
/// 유지관리 테이블 한 행을 표현하는 데이터 모델.
/// aaa.maintenance_log + aaa.equipment + aaa.manual + aaa.technician 조인 결과에 대응한다.
/// </summary>
public class MaintenanceRecord
{
    public string Id;               // maintenance_log.id
    public DateTime Date;           // COALESCE(completed_at, scheduled_at) -> 날짜 (호환용)
    public DateTime ScheduledAt;    // 예정시각
    public DateTime? CompletedAt;   // 완료시각
    public string EquipmentName;    // equipment.name -> 설비명
    public string Description;      // maintenance_log.description -> 이상 원인
    public string ManualTitle;      // manual.title -> 할당 매뉴얼
    public string TechnicianName;   // technician.name -> 담당자
    public string Status;           // maintenance_log.status -> 상태 (scheduled / in_progress / completed / cancelled)

    /// <summary>M- 형식의 가독성 좋은 짧은 ID 반환.</summary>
    public string DisplayId
    {
        get
        {
            if (string.IsNullOrEmpty(Id)) return "M-00000";
            return "M-" + Id;
        }
    }

    /// <summary>상태 한글 라벨.</summary>
    public string StatusLabel
    {
        get
        {
            switch ((Status ?? "").ToLowerInvariant())
            {
                case "scheduled": return "대기";
                case "in_progress": return "진행중";
                case "completed": return "완료";
                case "cancelled": return "취소";
                default: return "대기";
            }
        }
    }
}
