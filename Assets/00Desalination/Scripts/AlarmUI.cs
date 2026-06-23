using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AlarmUI : MonoBehaviour
{

    [Header("팝업 패널")]
    public GameObject popupPanel;

    [Header("텍스트")]
    public TMP_Text timeText;
    public TMP_Text titleText;       // 알람 내용
    public TMP_Text descText;        // 설명

    [Header("심각도 색상 바")]
    public Image severityBar;

    [Header("XR Rig")]
    public Transform xrRig; // ← 인스펙터에서 XR Origin (XR Rig) 연결

    private string _currentEquipmentId; // 현 알람의 equipment_id

    // 알람 웹 소켓에서 알람을 받음이 확인되면 ShowAlarm()을 실행함
    void OnEnable() => AlarmWebSocket.OnAlarmReceived += ShowAlarm;
    void OnDisable() => AlarmWebSocket.OnAlarmReceived -= ShowAlarm;

    /// <summary>
    /// 알람보기
    /// </summary>
    /// <param name="alarm"></param>
    void ShowAlarm(AlarmData alarm)
    {
        // equipment_id 저장 (텔포용)
        _currentEquipmentId = alarm.equipment?.id;
        // 텍스트 세팅
        string formattedTime = DateTime.Parse(alarm.triggered_at).ToString("yyyy-MM-dd HH:mm:ss");
        timeText.text = formattedTime;
        titleText.text = $"{alarm.equipment?.name}의 고장예지 확률이 임계값을 초과하였습니다.";
        descText.text = alarm.description;

        // severity 에 따라 색상 변경 (serverity 의 종류에 따라 색상변경)
        severityBar.color = alarm.severity switch
        {
            "critical" => new Color(0.9f, 0.1f, 0.1f),   // 빨강
            "warning" => new Color(1f, 0.5f, 0f),     // 주황
            _ => new Color(1f, 0.9f, 0f),     // 노랑 (info)
        };

        // 팝업 표시
        popupPanel.SetActive(true);

        //// 5초 후 자동 닫기
        //StopAllCoroutines();
        //StartCoroutine(AutoClose(5f));
    }

    public void OnMoveToEquipment()
    {
        if (string.IsNullOrEmpty(_currentEquipmentId))
        {
            Debug.LogWarning("[AlarmUI] equipment_id 없음");
            return;
        }

        EquipmentMarker[] markers = FindObjectsOfType<EquipmentMarker>();
        foreach (var marker in markers)
        {
            if (marker.equipmentId == _currentEquipmentId)
            {
                if (marker.teleportPoint != null)
                {
                    // ↓ 기존 Teleport 스크립트 방식에 맞게 수정
                    Transform xrRig = GameObject.Find("XR Origin (XR Rig)").transform;
                    StartCoroutine(FadeController.Instance.FadeAndTeleport(() =>
                    {
                        xrRig.position = marker.teleportPoint.position;
                        xrRig.rotation = marker.teleportPoint.rotation;
                    }));
                }
                else
                    Debug.LogWarning("[AlarmUI] 텔레포트 포인트 없음: " + marker.name);

                popupPanel.SetActive(false);
                return;
            }
        }

        Debug.LogWarning("[AlarmUI] 해당 equipment 못 찾음: " + _currentEquipmentId);
    }

    // 자동닫기 함수
    IEnumerator AutoClose(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        popupPanel.SetActive(false);
    }

    // 닫기 버튼 클릭시 실행 함수
    public void OnCloseButton()
    {
        StopAllCoroutines();
        popupPanel.SetActive(false);
    }
}

