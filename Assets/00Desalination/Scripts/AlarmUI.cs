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

    [Header("데모/테스트용 알람 (버튼 연결)")]
    public string demoEquipmentId;                 // 테스트할 설비의 EquipmentMarker.equipmentId와 동일하게 설정
    public string demoEquipmentName = "펌프 P-101";
    public string demoDescription = "진동 및 온도 이상 패턴이 감지되어 고장 가능성이 높습니다.";
    public string demoSeverity = "warning"; // critical / warning / info

    private string _currentEquipmentId; // 현 알람의 equipment_id

    // 알람 웹 소켓에서 알람을 받음이 확인되면 ShowAlarm()을 실행함
    void OnEnable() => AlarmWebSocket.OnAlarmReceived += ShowAlarm;
    void OnDisable() => AlarmWebSocket.OnAlarmReceived -= ShowAlarm;

    // 버튼에서 직접 호출하는 테스트/데모용 알람 트리거 (서버 웹소켓 없이 발생)
    public void TriggerDemoAlarm()
    {
        var demoAlarm = new AlarmData
        {
            type = "ALARM",
            alarm_id = "demo",
            alarm_code = "demo",
            severity = demoSeverity,
            description = demoDescription,
            triggered_at = DateTime.Now.ToString("o"),
            equipment = new EquipmentData { id = demoEquipmentId, name = demoEquipmentName },
            mr_space_id = ""
        };

        ShowAlarm(demoAlarm);
    }

    /// <summary>
    /// 알람보기
    /// </summary>
    /// <param name="alarm"></param>
    void ShowAlarm(AlarmData alarm)
    {
        // equipment_id 저장 (텔포용)
        _currentEquipmentId = alarm.equipment?.id;
        // 텍스트 세팅
        // 서버(Postgres)에서 오는 triggered_at 포맷이 .NET DateTime.Parse가 못 읽는 형태로 올 때가
        // 있어(FormatException 실기 확인) - 네트워크로 들어오는 외부 값이라 파싱 실패를 가정하고
        // 방어적으로 처리한다. 실패하면 원본 문자열을 그대로 보여줘서 최소한 알람 자체는 안 놓치게 함.
        string formattedTime = DateTime.TryParse(alarm.triggered_at, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var parsedTime)
            ? parsedTime.ToString("yyyy-MM-dd HH:mm:ss")
            : alarm.triggered_at;
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

