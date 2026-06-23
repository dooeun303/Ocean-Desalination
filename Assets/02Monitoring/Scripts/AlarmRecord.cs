using System;

/// <summary>
/// 고장예지(알람) 테이블 한 행을 표현하는 데이터 모델.
/// aaa.alarm + aaa.equipment + aaa.plant 조인 결과에 대응한다.
/// </summary>
public class AlarmRecord
{
    public string Id;              // alarm.alarm_id
    public string AlarmCode;       // alarm.alarm_code  -> 알람 ID
    public DateTime TriggeredAt;   // alarm.created_at -> 발생일시
    public string EquipmentName;   // equipment.equipment_name -> 설비명
    public string Location;        // plant.plant_location -> 위치
    public string Description;     // alarm.alarm_description -> 고장예지 내용
    public string Severity;        // alarm.alarm_severity -> info | warning | critical
    public bool IsActive;          // alarm.alarm_is_active -> 활성/해제

    /// <summary>등급 한글 라벨 (정상/경고/위험).</summary>
    public string SeverityLabel
    {
        get
        {
            switch ((Severity ?? "").ToLowerInvariant())
            {
                case "critical": return "위험";
                case "warning": return "경고";
                default: return "정상";
            }
        }
    }

    /// <summary>알람 상태 한글 라벨.</summary>
    public string StatusLabel => IsActive ? "활성" : "해제";
}
