using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지원요청 목록의 행 1개 - MaintenanceItemUI.cs와 동일한 패턴(Inspector로 연결한 텍스트/버튼에
// SetData가 값을 채우고 클릭 콜백을 건다). status가 "ringing"(대기)일 때만 수락/거절 버튼이
// 눌리게 하고, 이미 처리된(진행중/거절/종료) 건은 버튼을 비활성화해서 과거 기록처럼 보이게 한다.
public class SupportCallItemUI : MonoBehaviour
{
    [Header("텍스트")]
    public TMP_Text equipmentNameText;
    public TMP_Text descriptionText;
    public TMP_Text requesterText;
    public TMP_Text timeText;

    [Header("상태 표시")]
    public Image statusDot;

    [Header("버튼")]
    public Button acceptButton;
    public Button rejectButton;
    public TMP_Text acceptButtonLabel; // 수락 처리 중 "처리 중..."으로 잠깐 바꿔주기 위함(선택)

    SupportCallData _data;

    public void SetData(SupportCallData data, Action<SupportCallData> onAccept, Action<SupportCallData> onReject)
    {
        _data = data;

        if (equipmentNameText != null)
            equipmentNameText.text = string.IsNullOrEmpty(data.equipment_name) ? "설비 미지정" : data.equipment_name;

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrEmpty(data.maintenance_description) ? "지원 요청" : data.maintenance_description;

        if (requesterText != null)
            requesterText.text = string.IsNullOrEmpty(data.initiator_name) ? data.initiator_id : data.initiator_name;

        if (timeText != null)
            timeText.text = FormatTime(data.started_at, data.status);

        if (statusDot != null)
        {
            statusDot.color = data.status switch
            {
                "ringing" => new Color(0.9f, 0.6f, 0.15f),   // 대기 - 주황
                "active" => new Color(0.22f, 0.54f, 0.87f),  // 진행중 - 파랑
                "ended" => new Color(0.39f, 0.6f, 0.13f),    // 종료 - 초록
                "missed" => new Color(0.9f, 0.1f, 0.1f),     // 거절/부재 - 빨강
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }

        bool pending = data.status == "ringing";
        if (acceptButton != null)
        {
            acceptButton.interactable = pending;
            acceptButton.onClick.RemoveAllListeners();
            if (pending) acceptButton.onClick.AddListener(() => onAccept?.Invoke(_data));
        }
        if (rejectButton != null)
        {
            rejectButton.interactable = pending;
            rejectButton.onClick.RemoveAllListeners();
            if (pending) rejectButton.onClick.AddListener(() => onReject?.Invoke(_data));
        }
        if (acceptButtonLabel != null)
            acceptButtonLabel.text = "수락";
    }

    // 처리 중(수락 버튼 누른 직후 서버 응답 기다리는 동안) 버튼을 잠깐 잠가서 중복 클릭을 막는다.
    public void SetBusy(bool busy)
    {
        if (acceptButton != null) acceptButton.interactable = !busy && _data?.status == "ringing";
        if (rejectButton != null) rejectButton.interactable = !busy && _data?.status == "ringing";
        if (acceptButtonLabel != null) acceptButtonLabel.text = busy ? "처리 중..." : "수락";
    }

    static string FormatTime(string isoTime, string status)
    {
        if (string.IsNullOrEmpty(isoTime))
            return status == "ringing" ? "방금 전" : "-";

        if (DateTime.TryParse(isoTime, out DateTime dt))
            return dt.ToLocalTime().ToString("M월 d일 HH:mm");

        return isoTime;
    }
}
