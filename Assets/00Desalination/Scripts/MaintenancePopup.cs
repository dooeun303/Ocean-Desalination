using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class MaintenancePopup : MonoBehaviour
{
    // 패널
    [Header("패널")]
    public GameObject popupPanel;
    public GameObject moveConfirmPanel;   // 설비로 이동 확인 알림
    public GameObject saveConfirmPanel;   // 결과 저장 확인 알림
    public GameObject resultAlertPanel;   // 저장 완료/실패 알림

    // 텍스트
    [Header("텍스트")]
    public TMP_Text equipmentNameText;
    public TMP_Text workTypeText;
    public TMP_Text statusText;
    public TMP_Text scheduledAtText;
    public TMP_Text descriptionText;
    public TMP_Text manualTitleText;
    public TMP_Text alarmText;
    public TMP_Text resultAlertText;      // 저장 완료/실패 메시지

    [Header("완료 결과 조회")]
    public GameObject resultSection;      // 완료된 경우 결과 표시 영역
    public TMP_Text resultText;         // 작업 결과 텍스트
    public TMP_Text aiSummaryText;      // AI 요약 텍스트

    [Header("결과 선택 드롭다운")]
    public TMP_Dropdown categoryDropdown;    // 대분류 드롭다운

    // 버튼
    [Header("버튼 - 시작 전")]
    public Button startButton;            // 시작

    [Header("버튼 - 시작 후")]
    public Button completeButton;         // 완료
    public Button videoCallButton;        // 화상통화

    [Header("버튼 - 공통")]
    public Button closeButton;            // 닫기

    [Header("이동 확인 알림 버튼")]
    public Button moveConfirmYesButton;   // 설비로 이동
    public Button moveConfirmNoButton;    // 닫기

    [Header("저장 확인 알림 버튼")]
    public Button saveConfirmYesButton;   // 예
    public Button saveConfirmNoButton;    // 아니오

    // 서버 / XR
    public string serverUrl => ServerConfig.BaseUrl + "/api/maintenances/";

    [Header("XR Rig")]
    public Transform xrRig;

    [Header("이동 시 닫을 패널")]
    public GameObject menuPanelToHide;           // 메뉴 패널
    public GameObject maintenanceListPanelToHide; // 유지보수 리스트 패널

    [Header("화상통화")]
    public GameObject videoCallPanel;            // 화상통화 패널 (JoinChannelVideoToken 붙어있는 오브젝트)

    private MaintenanceData _data;
    private List<ResultCategory> _categories = new List<ResultCategory>();

    void Start()
    {
        closeButton?.onClick.AddListener(OnCloseButton);
        startButton?.onClick.AddListener(OnStartButton);
        completeButton?.onClick.AddListener(OnCompleteButton);

        moveConfirmYesButton?.onClick.AddListener(OnMoveConfirmYes);
        moveConfirmNoButton?.onClick.AddListener(OnMoveConfirmNo);

        saveConfirmYesButton?.onClick.AddListener(OnSaveConfirmYes);
        saveConfirmNoButton?.onClick.AddListener(OnSaveConfirmNo);

        popupPanel?.SetActive(false);
        moveConfirmPanel?.SetActive(false);
        saveConfirmPanel?.SetActive(false);
        resultAlertPanel?.SetActive(false);

        // 화상통화 거절 이벤트 구독
        VideoCallSignalingMR.OnCallRejected += OnVideoCallRejected;
    }

    void OnDestroy()
    {
        VideoCallSignalingMR.OnCallRejected -= OnVideoCallRejected;
    }

    // 팝업 열기
    public void Open(MaintenanceData data)
    {
        _data = data;

        // 패널 먼저 활성화
        popupPanel?.SetActive(true);
        ResetDropdowns();
        LoadCategories();
        moveConfirmPanel?.SetActive(false);
        saveConfirmPanel?.SetActive(false);
        resultAlertPanel?.SetActive(false);
        resultSection?.SetActive(false);

        RefreshUI();
    }

    // UI 새로고침
    void RefreshUI()
    {
        if (equipmentNameText) equipmentNameText.text = _data.equipment?.name ?? "-";
        if (workTypeText) workTypeText.text = _data.work_type ?? "-";
        if (statusText) statusText.text = _data.status switch
        {
            "scheduled" => "대기",
            "in_progress" => "진행중",
            "completed" => "완료",
            "cancelled" => "취소",
            _ => _data.status
        };

        if (scheduledAtText)
        {
            if (System.DateTime.TryParse(_data.scheduled_at, out System.DateTime dt))
                scheduledAtText.text = dt.ToLocalTime().ToString("yyyy년 M월 d일 HH:mm");
            else
                scheduledAtText.text = _data.scheduled_at;
        }

        if (descriptionText) descriptionText.text = _data.description ?? "-";
        if (manualTitleText) manualTitleText.text = _data.manual?.title ?? "매뉴얼 없음";
        if (alarmText)
        {
            alarmText.text = _data.latest_alarm != null
                ? $"[{_data.latest_alarm.severity}] {_data.latest_alarm.description}"
                : "최근 알람 없음";
        }

        bool isCompleted = _data.status == "completed";
        bool isPending = _data.status == "scheduled";

        // 완료된 작업 — 결과 표시, 시작 버튼 숨기기
        if (isCompleted)
        {
            // 완료 — 결과 표시, 드롭다운 숨기기
            resultSection?.SetActive(true);
            if (resultText) resultText.text = string.IsNullOrEmpty(_data.result) ? "-" : _data.result;
            if (aiSummaryText) aiSummaryText.text = string.IsNullOrEmpty(_data.ai_summary) ? "AI 요약 없음" : _data.ai_summary;
            categoryDropdown?.gameObject.SetActive(false);
        }
        else
        {
            // 미완료 — 드롭다운 표시, 결과 섹션 숨기기
            resultSection?.SetActive(false);
            categoryDropdown?.gameObject.SetActive(true);
            ResetDropdowns();
            LoadCategories();
        }

        // 버튼 상태
        RefreshButtons(isStarted: !isPending && !isCompleted);
    }

    // 버튼 상태 전환
    private void RefreshButtons(bool isStarted)
    {
        bool isCompleted = _data.status == "completed";

        startButton?.gameObject.SetActive(!isStarted && !isCompleted);
        completeButton?.gameObject.SetActive(isStarted);
        videoCallButton?.gameObject.SetActive(isStarted);
        // 닫기 버튼은 항상 표시
    }

    // 시작 버튼 → 이동 확인 알림
    public void OnStartButton()
    {
        moveConfirmPanel?.SetActive(true);
    }

    // 이동 확인: 설비로 이동
    public void OnMoveConfirmYes()
    {
        moveConfirmPanel?.SetActive(false);
        MoveToEquipment();

        // 메뉴 & 리스트 비활성화
        menuPanelToHide?.SetActive(false);
        maintenanceListPanelToHide?.SetActive(false);

        _data.status = "in_progress";
        RefreshButtons(isStarted: true);
    }

    // 이동 확인: 닫기
    public void OnMoveConfirmNo()
    {
        moveConfirmPanel?.SetActive(false);
    }

    // 설비 위치로 이동
    private void MoveToEquipment()
    {
        if (string.IsNullOrEmpty(_data?.equipment?.id))
        {
            Debug.LogWarning("[Maintenance] equipment_id 없음");
            return;
        }

        EquipmentMarker[] markers = FindObjectsOfType<EquipmentMarker>();
        foreach (var marker in markers)
        {
            if (marker.equipmentId == _data.equipment.id)
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
                    Debug.LogWarning("[Maintenance] 텔레포트 포인트 없음: " + marker.name);
                return;
            }
        }
        Debug.LogWarning("[Maintenance] 해당 equipment 못 찾음: " + _data.equipment.id);
    }

    // 완료 버튼 → 저장 확인 알림
    public void OnCompleteButton()
    {
        saveConfirmPanel?.SetActive(true);
    }

    // 저장 확인: 예
    public void OnSaveConfirmYes()
    {
        saveConfirmPanel?.SetActive(false);

        // 녹음 종료 + 서버 전송
        SummaryDisplay.Instance?.ShowLoading();
        AudioRecorder.Instance?.StopAndSend(_data.id);

        StartCoroutine(UpdateStatus("completed", GetSelectedResult()));
    }

    // 저장 확인: 아니오
    public void OnSaveConfirmNo()
    {
        saveConfirmPanel?.SetActive(false);
    }

    // 닫기 버튼
    public void OnCloseButton()
    {
        popupPanel?.SetActive(false);
        moveConfirmPanel?.SetActive(false);
        saveConfirmPanel?.SetActive(false);
        resultAlertPanel?.SetActive(false);
    }

    // 화상통화 버튼
    // videoCallPanel은 켜두되(엔진 준비용), 실제 채널 join은 AR이 수락한 뒤에 일어남
    public void OnVideoCallButton()
    {
        Debug.Log("[Maintenance] 화상통화 요청 → maintenance_id: " + _data.id);
        AudioRecorder.Instance?.StartRecording();
        videoCallPanel?.SetActive(true);
        VideoCallSignalingMR.Instance?.RequestCall(_data.id);
    }

    // 화상통화 거절됨 → 대기 중이던 패널 닫기
    private void OnVideoCallRejected(string channelName)
    {
        if (_data != null && channelName == _data.id)
        {
            Debug.Log("[Maintenance] 화상통화 거절됨: " + channelName);
            videoCallPanel?.SetActive(false);
        }
    }

    // 상태 업데이트
    IEnumerator UpdateStatus(string status, string result)
    {
        string url = serverUrl + _data.id;
        string json = result != null
            ? $"{{\"status\":\"{status}\",\"result\":\"{result}\"}}"
            : $"{{\"status\":\"{status}\"}}";

        UnityWebRequest req = new UnityWebRequest(url, "PATCH");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Maintenance] 상태 업데이트 완료: " + status);
            _data.status = status;
            _data.result = result;
            RefreshUI();
            ShowResultAlert("저장이 완료되었습니다.");
        }
        else
        {
            Debug.LogError("[Maintenance] 업데이트 실패: " + req.error);
            ShowResultAlert("저장에 실패했습니다. 다시 시도해주세요.");
        }
    }

    // 드롭다운 초기화
    private void ResetDropdowns()
    {
        if (categoryDropdown)
        {
            categoryDropdown.ClearOptions();
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("대분류 선택"));
        }
    }

    // 대분류 목록 로드
    private void LoadCategories()
    {
        StartCoroutine(FetchCategories());
    }

    private IEnumerator FetchCategories()
    {
        // 정비 결과 대분류 = system_code RESP10 (유지관리결과 구분)
        string url = ServerConfig.BaseUrl + "/api/codes?group=RESP10";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Maintenance] 대분류 로드 실패: " + req.error);
            yield break;
        }

        var response = JsonUtility.FromJson<ResultCategoryResponse>(req.downloadHandler.text);
        if (!response.success || response.data == null) yield break;

        _categories = new List<ResultCategory>(response.data);

        if (categoryDropdown)
        {
            categoryDropdown.ClearOptions();
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("대분류 선택"));
            foreach (var cat in _categories)
                categoryDropdown.options.Add(new TMP_Dropdown.OptionData(cat.name));
            categoryDropdown.RefreshShownValue();
        }
    }

    // 선택된 결과 텍스트 반환 (대분류만)
    private string GetSelectedResult()
    {
        if (categoryDropdown == null) return "작업 완료";

        int catIndex = categoryDropdown.value;
        if (catIndex == 0) return "작업 완료";

        return categoryDropdown.options[catIndex].text;
    }

    // 저장 결과 알림 (3초 후 자동 닫힘)
    private void ShowResultAlert(string message)
    {
        if (resultAlertText) resultAlertText.text = message;
        resultAlertPanel?.SetActive(true);
        StartCoroutine(AutoHideAlert());
    }

    private IEnumerator AutoHideAlert()
    {
        yield return new WaitForSeconds(3f); // 3초 기다림
        resultAlertPanel?.SetActive(false);
    }
}


// ─────────────────────────────────────────
// 결과 카테고리 데이터 모델
// ─────────────────────────────────────────
[System.Serializable]
public class ResultCategory
{
    public string id;
    public string name;
}

[System.Serializable]
public class ResultCategoryResponse
{
    public bool success;
    public ResultCategory[] data;
}