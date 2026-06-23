using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =============================================
// 유지보수 테이블 행 하나
// 헤더 컬럼 순서:
// 작업ID | 장비명 | 할당 매뉴얼 | 작업 타입 | 작업자명 | 결과 | 예정시각 | 완료시각
// =============================================
public class MaintenanceRowUI : MonoBehaviour
{
    [Header("컬럼 텍스트 (헤더 순서대로 연결)")]
    public TMP_Text idText;             // 작업 id
    public TMP_Text equipmentNameText;  // 장비명
    public TMP_Text manualTitleText;    // 할당 매뉴얼
    public TMP_Text workTypeText;       // 작업 타입
    public TMP_Text technicianIdText;   // 작업자명
    public TMP_Text resultText;         // 결과
    public TMP_Text scheduledAtText;    // 예정시각
    public TMP_Text completedAtText;    // 완료시각

    [Header("상태 색상 바 (선택사항)")]
    public Image statusBar;

    // =============================================
    // 데이터 설정
    // =============================================
    public void SetData(MaintenanceData data)
    {
        // 작업 ID (8자리까지만 표시)
        if (idText)
            idText.text = data.id?.Length > 8 ? data.id[..8] + "…" : data.id ?? "-";

        if (equipmentNameText)
            equipmentNameText.text = data.equipment?.name ?? "-";

        if (manualTitleText)
            manualTitleText.text = data.manual?.title ?? "-";

        if (workTypeText)
            workTypeText.text = data.work_type ?? "-";

        if (technicianIdText)
            technicianIdText.text = data.technician_name ?? "-";

        if (resultText)
            resultText.text = string.IsNullOrEmpty(data.result) ? "-" : data.result;

        if (scheduledAtText)
            scheduledAtText.text = FormatDate(data.scheduled_at);

        if (completedAtText)
            completedAtText.text = FormatDate(data.completed_at);

        if (statusBar)
            statusBar.color = GetStatusColor(data.status);
    }

    // =============================================
    // 날짜 포맷 (MM/dd HH:mm)
    // =============================================
    string FormatDate(string isoDate)
    {
        if (string.IsNullOrEmpty(isoDate)) return "-";
        if (DateTime.TryParse(isoDate, out DateTime dt))
            return dt.ToLocalTime().ToString("MM/dd HH:mm");
        return isoDate;
    }

    // =============================================
    // 상태 색상
    // =============================================
    Color GetStatusColor(string status) => status switch
    {
        "in_progress" => new Color(0.22f, 0.54f, 0.87f),   // 파랑
        "scheduled" => new Color(0.70f, 0.70f, 0.70f),   // 회색
        "completed" => new Color(0.39f, 0.60f, 0.13f),   // 초록
        "cancelled" => new Color(0.90f, 0.10f, 0.10f),   // 빨강
        _ => new Color(0.70f, 0.70f, 0.70f),
    };
}