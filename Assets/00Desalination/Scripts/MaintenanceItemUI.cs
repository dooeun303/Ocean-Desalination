using System;
using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 유지보수 리스트에 들어가는 아이템 1개
public class MaintenanceItemUI : MonoBehaviour
{
    [Header("텍스트")]
    public TMP_Text equipmentNameText;
    public TMP_Text workTypeText;
    public TMP_Text scheduledAtText;

    [Header("상태 뱃지")]
    public Image statusDot;

    [Header("클릭 버튼")]
    public Button arrowButton;

    private MaintenanceData _data;

    // 데이터 설정하는 함수 (데이터, 클릭 이벤트)
    public void SetData(MaintenanceData data, Action<MaintenanceData> onClicked)
    {
        _data = data;

        // 장비명
        if (equipmentNameText != null)
            equipmentNameText.text = data.equipment?.name ?? "-";

        // 작업 타입
        if (workTypeText != null)
            workTypeText.text = data.work_type ?? "-";

        // 예정 시간
        if (scheduledAtText != null)
            if (DateTime.TryParse(data.scheduled_at, out DateTime dt))
                scheduledAtText.text = dt.ToLocalTime().ToString("M월 d일 HH:mm");
            else
                scheduledAtText.text = data.scheduled_at;

        if (statusDot != null)
        {
            statusDot.color = data.status switch
            {
                "in_progress" => new Color(0.22f, 0.54f, 0.87f),
                "scheduled" => new Color(0.7f, 0.7f, 0.7f),
                "completed" => new Color(0.39f, 0.6f, 0.13f),
                "cancelled" => new Color(0.9f, 0.1f, 0.1f),
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }

        if (arrowButton != null)
        {
            arrowButton.onClick.AddListener(() => onClicked?.Invoke(_data));
        }

        
    }
}
