using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectionItemUI : MonoBehaviour
{
    [Header("UI 연결 (알람한개 프리팹 내부)")]
    public TMP_Text equipmentNameText;
    public TMP_Text locationText;
    public TMP_Text statusText;
    public TMP_Text alarmSeverityText;
    public TMP_Text scheduledAtText;
    public Button actionButton;

    [Header("색상")]
    public Color colorCritical = new Color(0.97f, 0.32f, 0.29f);
    public Color colorWarning = new Color(0.94f, 0.71f, 0.16f);
    public Color colorNormal = new Color(0.25f, 0.73f, 0.31f);
    public Color colorGray = new Color(0.54f, 0.58f, 0.62f);

    private InspectionItem _data;

    public void Bind(InspectionItem data)
    {
        _data = data;

        if (equipmentNameText) equipmentNameText.text = data.equipment_name;
        if (locationText)
        {
            var marker = FindEquipmentMarker(data.equipment_id);
            locationText.text = marker != null ? marker.name : data.location;
        }
        if (scheduledAtText) scheduledAtText.text = FormatTime(data.scheduled_at);

        if (statusText)
        {
            statusText.text = data.inspection_status switch
            {
                "completed" => "완료",
                "in_progress" => "진행 중",
                "pending" => "대기",
                _ => "미정"
            };
            statusText.color = data.inspection_status switch
            {
                "completed" => colorNormal,
                "in_progress" => colorWarning,
                "pending" => colorGray,
                _ => colorGray
            };
        }

        if (alarmSeverityText)
        {
            alarmSeverityText.text = data.latest_alarm_severity switch
            {
                "critical" => "Critical",
                "warning" => "Warning",
                _ => "-"
            };
            alarmSeverityText.color = data.latest_alarm_severity switch
            {
                "critical" => colorCritical,
                "warning" => colorWarning,
                _ => colorGray
            };
        }

        actionButton?.onClick.RemoveAllListeners();
        actionButton?.onClick.AddListener(OnActionClick);
    }

    private void OnActionClick()
    {
        InspectionPopup.Instance?.Open(_data);
    }

    private EquipmentMarker FindEquipmentMarker(string equipmentId)
    {
        EquipmentMarker[] markers = FindObjectsOfType<EquipmentMarker>();
        foreach (var marker in markers)
            if (marker.equipmentId == equipmentId)
                return marker;
        return null;
    }

    private string FormatTime(string isoTime)
    {
        if (string.IsNullOrEmpty(isoTime)) return "-";
        if (System.DateTime.TryParse(isoTime, out var dt))
            return dt.ToLocalTime().ToString("HH:mm");
        return "-";
    }
}