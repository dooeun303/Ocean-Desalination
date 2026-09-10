using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectionPopup : MonoBehaviour
{
    public static InspectionPopup Instance;

    // 패널
    [Header("패널")]
    public GameObject popupPanel;           // 원격점검_상세 루트
    public GameObject confirmPanel;         // "결과를 저장하시겠습니까?" 알림
    public GameObject moveConfirmPanel;     // "해당 설비로 이동하시겠습니까?" 알림

    // 헤더
    [Header("헤더")]
    public TMP_Text statusText;             // 상태 텍스트
    public TMP_Text equipmentNameText;      // 장비명 텍스트
    public TMP_Text equipmentTypeText;      // 장비타입 텍스트

    // 구분정보
    [Header("구분정보")]
    public TMP_Text locationText;           // equipment 위치 (ar_anchor_id 기반)
    public TMP_Text scheduledAtText;        // 예정시각

    // 센서 UI (순서: temperature / pressure / vibration / rpm)
    [Header("센서 UI (순서: temperature / pressure / vibration / rpm)")]
    public TMP_Text[] sensorValueTexts; // 센서값
    public TMP_Text[] sensorStatusTexts; // 센서 상태

    // 결과 저장
    [Header("결과 저장")]
    public TMP_InputField noteInputField;   // 메모 입력 (미완료 시)

    [Header("저장 결과 알림")]
    public GameObject resultAlertPanel;     // 성공/실패 공용 알림 패널
    public TMP_Text resultAlertText;      // 알림 메시지 텍스트

    [Header("완료 결과 조회")]
    public GameObject resultSection;        // 완료된 경우 결과 표시 영역
    public TMP_Text resultText;           // 점검 결과 (이상없음 / 이상발견)
    public TMP_Text resultNoteText;       // 점검 메모

    // 버튼
    [Header("버튼 - 시작 전")]
    public Button startButton;              // 시작 (처음 상태)

    [Header("버튼 - 시작 후")]
    public Button normalButton;             // 이상 없음
    public Button issueButton;              // 이상 발견
    public Button videoCallButton;          // 화상 통화

    [Header("버튼 - 공통")]
    public Button closeButton;              // 닫기

    [Header("이동 확인 알림 버튼")]
    public Button moveConfirmYesButton;     // 해당 설비로 이동
    public Button moveConfirmNoButton;      // 닫기

    [Header("결과 확인 알림 버튼")]
    public Button confirmYesButton;         // 예
    public Button confirmNoButton;          // 아니오

    // 색상
    [Header("색상")]
    public Color colorCritical = new Color(0.97f, 0.32f, 0.29f);
    public Color colorWarning = new Color(0.94f, 0.71f, 0.16f);
    public Color colorNormal = new Color(0.25f, 0.73f, 0.31f);
    public Color colorGray = new Color(0.54f, 0.58f, 0.62f);

    // XR Rig
    [Header("XR Rig")]
    public Transform xrRig;

    // 화상통화
    [Header("화상통화")]
    public GameObject videoCallPanel;       // 화상통화 패널

    // 내부 상태
    private InspectionItem _currentItem;
    private string _pendingResult = null;   // 저장 대기 중인 결과
    private readonly string[] _sensorOrder = { "temperature", "pressure", "vibration", "rpm" };
    
    // Awake: popup 싱글톤
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start: 버튼들 연결
    void Start()
    {
        // 버튼 이벤트 등록
        startButton?.onClick.AddListener(OnStartButton); // 시작
        normalButton?.onClick.AddListener(() => OnResultButton("normal")); // result가 이상없음
        issueButton?.onClick.AddListener(() => OnResultButton("issue_found")); // result가 이상있음
        videoCallButton?.onClick.AddListener(OnVideoCallButton); // 화상통화
        closeButton?.onClick.AddListener(Close); // 닫기

        moveConfirmYesButton?.onClick.AddListener(OnMoveConfirmYes); // 설비 이동 yes
        moveConfirmNoButton?.onClick.AddListener(OnMoveConfirmNo); // 설비 이동 no

        confirmYesButton?.onClick.AddListener(OnConfirmYes); // 저장 yes
        confirmNoButton?.onClick.AddListener(OnConfirmNo); // 저장 no

        // 첨엔 팝업이랑 설비이동/저장 패널 꺼두기
        popupPanel?.SetActive(false);
        confirmPanel?.SetActive(false);
        moveConfirmPanel?.SetActive(false);

        // 화상통화 거절 이벤트 구독 (수락되면 JoinChannelVideoToken이 알아서 join함)
        VideoCallSignalingMR.OnCallRejected += OnVideoCallRejected;
    }

    void OnDestroy()
    {
        VideoCallSignalingMR.OnCallRejected -= OnVideoCallRejected;
    }

    // 1. 팝업 열기
    public void Open(InspectionItem item)
    {
        _currentItem = item;
        _pendingResult = null;
        
        
        // 패널 먼저 활성화 (코루틴 실행을 위해 GameObject가 active여야 함)
        popupPanel?.SetActive(true);        // 팝업패널 활성화
        confirmPanel?.SetActive(false);     // 컨펌패널 x
        moveConfirmPanel?.SetActive(false); // 이동패널 x

        // UI 상태 초기화 (이전 열기 상태 잔류 방지)
        resultAlertPanel?.SetActive(false); // 결과알람 패널
        resultSection?.SetActive(false);    // 결과칸 x
        if (noteInputField != null) noteInputField.gameObject.SetActive(true); // 노트 입력 필드 true
        noteInputField?.SetTextWithoutNotify("");
        if (resultText) resultText.text = "";
        if (resultNoteText) resultNoteText.text = "";

        BindHeader(item); // 바인드 헤더
        BindInfo(item); // 바인드 정보
        ClearSensors(); // 센서값 클리어
        LoadSensors(item.equipment_id); // 센서로드 

        // 완료되엇는지 확인
        bool isCompleted = item.inspection_status == "completed";

        if (isCompleted) // 완료된 작업이라면
        {
            ShowCompletedResult(item); // 결과 칸 보이기
            RefreshButtons(mode: ButtonMode.Completed);
        }
        else
        {
            HideCompletedResult(); // 결과칸 숨기기
            RefreshButtons(mode: ButtonMode.NotStarted);
        }
    }

    // 2. 팝업 닫기
    public void Close()
    {
        popupPanel?.SetActive(false); // 팝업패널 닫기
        confirmPanel?.SetActive(false); // 확인 패널 닫기
        moveConfirmPanel?.SetActive(false); // 이동 패널 닫기
    }

    // 3. 헤더 바인딩
    private void BindHeader(InspectionItem item)
    {
        if (statusText)
        {
            statusText.text = item.inspection_status switch
            {
                "completed" => "완료",
                "in_progress" => "진행중",
                "pending" => "대기",
                _ => "미정"
            };
            statusText.color = item.inspection_status switch
            {
                "completed" => colorNormal,
                "in_progress" => colorWarning,
                "pending" => colorGray,
                _ => colorGray
            };
        }
        if (equipmentNameText) equipmentNameText.text = item.equipment_name;
        if (equipmentTypeText) equipmentTypeText.text = item.equipment_type;
    }

    // 4. 구분정보 바인딩
    private void BindInfo(InspectionItem item)
    {
        // equipment 위치 (ar_anchor_id 기반 마커에서 가져오거나 별도 필드 사용)
        if (locationText)
        {
            var marker = FindEquipmentMarker(item.equipment_id);
            locationText.text = marker != null ? marker.name : item.equipment_id;
        }

        if (scheduledAtText)
        {
            if (System.DateTime.TryParse(item.scheduled_at, out var dt))
                scheduledAtText.text = dt.ToLocalTime().ToString("yyyy년 M월 d일 HH:mm");
            else
                scheduledAtText.text = "-";
        }
    }

    // 5. 버튼 상태 전환
    private enum ButtonMode { NotStarted, Started, Completed }

    // 6. 새로고침
    private void RefreshButtons(ButtonMode mode)
    {
        startButton?.gameObject.SetActive(mode == ButtonMode.NotStarted);
        normalButton?.gameObject.SetActive(mode == ButtonMode.Started);
        issueButton?.gameObject.SetActive(mode == ButtonMode.Started);
        videoCallButton?.gameObject.SetActive(mode == ButtonMode.Started);
        // 닫기 버튼은 항상 표시
    }

    // 7. 시작 버튼 → 이동 확인 알림
    public void OnStartButton()
    {
        moveConfirmPanel?.SetActive(true);
    }

    // 8. 이동 확인: 해당 설비로 이동
    public void OnMoveConfirmYes()
    {
        moveConfirmPanel?.SetActive(false);
        MoveToEquipment();
        RefreshButtons(mode: ButtonMode.Started);
    }

    // 9. 이동 확인: 닫기
    public void OnMoveConfirmNo()
    {
        moveConfirmPanel?.SetActive(false);
    }

    // 10. 설비 위치로 이동
    private void MoveToEquipment()
    {
        if (string.IsNullOrEmpty(_currentItem?.equipment_id))
        {
            Debug.LogWarning("[InspectionPopup] equipment_id 없음");
            return;
        }

        EquipmentMarker[] markers = FindObjectsOfType<EquipmentMarker>();
        foreach (var marker in markers)
        {
            if (marker.equipmentId == _currentItem.equipment_id)
            {
                if (marker.teleportPoint != null)
                {
                    Transform targetPoint = marker.teleportPoint;
                    Transform rig = xrRig;

                    FadeController.Instance.StartCoroutine(
                        FadeController.Instance.FadeAndTeleport(() =>
                        {
                            rig.position = targetPoint.position;
                            rig.rotation = targetPoint.rotation;
                        })
                    );
                }
                else
                    Debug.LogWarning("[InspectionPopup] 텔레포트 포인트 없음: " + marker.name);
                return;
            }
        }
        Debug.LogWarning("[InspectionPopup] 해당 equipment 못 찾음: " + _currentItem.equipment_id);
    }

    // 11. 이상없음 / 이상발견 버튼 → 저장 확인 알림
    public void OnResultButton(string result)
    {
        _pendingResult = result;
        confirmPanel?.SetActive(true);
    }

    // 12. 저장 확인: 예
    public void OnConfirmYes()
    {
        confirmPanel?.SetActive(false);

        var request = new SaveInspectionRequest
        {
            inspection_id = _currentItem.inspection_id,   // 기존 레코드 UPDATE
            equipment_id = _currentItem.equipment_id,
            inspector_id = "inspector-001",              // 실제 로그인 사용자 ID로 교체
            result = _pendingResult,
            note = noteInputField?.text ?? ""
        };

        normalButton.interactable = false; // 정상 버튼
        issueButton.interactable = false; // 이상 없음 버튼

        // 점검리스트 (결과 저장하기) 
        InspectionList.Instance.SaveResult(
            this,
            request,
            onSuccess: (_) =>
            {
                normalButton.interactable = true;
                issueButton.interactable = true;
                Debug.Log("[InspectionPopup] 저장 완료");
                ShowResultAlert("점검 결과가 저장되었습니다.");
                InspectionList.Instance.LoadList();
            },
            onError: (err) =>
            {
                normalButton.interactable = true;
                issueButton.interactable = true;
                Debug.LogError("[InspectionPopup] 저장 실패: " + err);
                ShowResultAlert("저장에 실패했습니다. 다시 시도해주세요.");
            }
        );
    }

    // 13. 저장 확인: 아니오
    public void OnConfirmNo()
    {
        _pendingResult = null;
        confirmPanel?.SetActive(false);
    }

    // 14. 화상통화 버튼
    // videoCallPanel은 켜두되(엔진 준비용), 실제 채널 join은 AR이 수락한 뒤에 일어남
    public void OnVideoCallButton()
    {
        Debug.Log("[InspectionPopup] 화상통화 요청 → equipment_id: " + _currentItem.equipment_id);
        videoCallPanel?.SetActive(true);
        VideoCallSignalingMR.Instance?.RequestCall(_currentItem.equipment_id);
    }

    // 화상통화 거절됨 → 대기 중이던 패널 닫기
    private void OnVideoCallRejected(string channelName)
    {
        if (_currentItem != null && channelName == _currentItem.equipment_id)
        {
            Debug.Log("[InspectionPopup] 화상통화 거절됨: " + channelName);
            videoCallPanel?.SetActive(false);
        }
    }

    // 15. 완료 결과 표시
    private void ShowCompletedResult(InspectionItem item)
    {
        resultSection?.SetActive(true);
        if (noteInputField != null) noteInputField.gameObject.SetActive(false);

        if (resultText)
        {
            resultText.text = item.inspection_result switch
            {
                "normal" => "이상 없음",
                "issue_found" => "이상 발견",
                _ => "-"
            };
            resultText.color = item.inspection_result switch
            {
                "normal" => colorNormal,
                "issue_found" => colorCritical,
                _ => colorGray
            };
        }

        if (resultNoteText)
            resultNoteText.text = string.IsNullOrEmpty(item.inspection_note)
                ? "-"
                : item.inspection_note;
    }

    // 16. 완료 결과 숨기기
    private void HideCompletedResult()
    {
        resultSection?.SetActive(false);
        if (noteInputField != null) noteInputField.gameObject.SetActive(true);
    }

    // 저장 결과 알림
    private void ShowResultAlert(string message)
    {
        if (resultAlertText) resultAlertText.text = message;
        resultAlertPanel?.SetActive(true);
        StartCoroutine(HideResultAlertAfterDelay(3f));
    }

    private System.Collections.IEnumerator HideResultAlertAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        resultAlertPanel?.SetActive(false);
    }

    // 센서 초기화
    private void ClearSensors()
    {
        for (int i = 0; i < _sensorOrder.Length; i++)
        {
            if (sensorValueTexts != null && i < sensorValueTexts.Length && sensorValueTexts[i])
                sensorValueTexts[i].text = "-";
            if (sensorStatusTexts != null && i < sensorStatusTexts.Length && sensorStatusTexts[i])
                sensorStatusTexts[i].text = "-";
        }
    }

    // 센서 로드
    private void LoadSensors(string equipmentId)
    {
        InspectionList.Instance.GetSensors(
            this,
            equipmentId,
            onSuccess: (response) =>
            {
                if (!response.success || response.data == null) return;

                for (int i = 0; i < _sensorOrder.Length; i++)
                {
                    var sensor = System.Array.Find(response.data, s => s.sensor_type == _sensorOrder[i]);
                    if (sensor == null) continue;

                    if (sensorValueTexts != null && i < sensorValueTexts.Length && sensorValueTexts[i])
                        sensorValueTexts[i].text = $"{sensor.value:F1} {sensor.unit}";

                    if (sensorStatusTexts != null && i < sensorStatusTexts.Length && sensorStatusTexts[i])
                    {
                        sensorStatusTexts[i].text = sensor.status switch
                        {
                            "critical" => "초과",
                            "warning" => "주의",
                            _ => "정상"
                        };
                        sensorStatusTexts[i].color = sensor.status switch
                        {
                            "critical" => colorCritical,
                            "warning" => colorWarning,
                            _ => colorNormal
                        };
                    }
                }
            },
            onError: (err) => Debug.LogError("[InspectionPopup] 센서 로드 실패: " + err)
        );
    }

    // 유틸: EquipmentMarker 찾기
    private EquipmentMarker FindEquipmentMarker(string equipmentId)
    {
        EquipmentMarker[] markers = FindObjectsOfType<EquipmentMarker>();
        foreach (var marker in markers)
            if (marker.equipmentId == equipmentId)
                return marker;
        return null;
    }
}