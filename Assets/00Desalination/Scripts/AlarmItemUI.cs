using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlarmItemUI : MonoBehaviour
{
    // 날짜, 내용 텍스트 
    [Header("텍스트")]
    public TMP_Text dateText;
    public TMP_Text contentText;

    [Header("심각도 색상 바")]
    public Image severityBar;

    // 데이터 세팅 (GetAllAlarm.cs 에서 alarm 객체를 받아서)
    public void SetData(AlarmData alarm)
    {
        if (dateText != null)
        {
            if (System.DateTime.TryParse(alarm.triggered_at, out System.DateTime dt))
            {
                dt = dt.ToLocalTime(); // UTC → 한국시간
                dateText.text = dt.ToString("yyyy년 M월 d일 HH:mm:ss");
            }
            else
            {
                dateText.text = alarm.triggered_at; // 파싱 실패시 원본 그대로
            }
        }

        if (contentText != null)
        {
            // equipment 이름, 알람 description
            contentText.text = $"{alarm.equipment?.name} - {alarm.description}";

            
        }

        // 심각도 색상
        if (severityBar != null)
        {
            // severity 를 색상으로 표현
            severityBar.color = alarm.severity switch
            {
                "critical" => new Color(0.9f, 0.1f, 0.1f), // red
                "warning" => new Color(1f, 0.5f, 0f), // orange
                _ => new Color(1f, 0.9f, 0f) // yellow
            };
        }
    }
}
