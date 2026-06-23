using System;

/// <summary>
/// 원격점검 테이블 한 행을 표현하는 데이터 모델.
/// aaa.remote_inspection + aaa.technician 조인 결과에 대응한다.
/// </summary>
public class InspectionRecord
{
    public string Id;              // remote_inspection.id
    public DateTime InspectionDate; // COALESCE(completed_at, scheduled_at) -> 날짜
    public string InspectorName;   // technician.name -> 담당자
    public string Result;          // remote_inspection.result -> 상태 판단 기준
    public string Note;            // remote_inspection.note -> 비고

    /// <summary>R- 형식의 가독성 좋은 짧은 ID 반환.</summary>
    public string DisplayId
    {
        get
        {
            if (string.IsNullOrEmpty(Id)) return "R-00000";
            return "R-" + Id;
        }
    }

    /// <summary>상태 한글 라벨 (진행중/대기/완료).</summary>
    public string StatusLabel
    {
        get
        {
            switch ((Result ?? "").ToLowerInvariant())
            {
                case "pending": return "대기";
                case "in_progress": return "진행중";
                case "normal":
                case "issue_found":
                default:
                    return "완료";
            }
        }
    }
}
