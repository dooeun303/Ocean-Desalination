using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

// 지원 요청 목록(MaintenanceList.cs와 동일한 패턴) - AR이 화상통화를 걸어오면(POST
// /api/support-calls) status='ringing'으로 DB에 쌓이는데, MR이 항상 헤드셋을 쓰고 있는 게
// 아니라서 1회성 팝업 대신 이 화면을 아무 때나 열어서 확인/수락/거절한다. 수락하면
// VideoCallSignalingMR.RaiseCallAccepted()로 기존 MrCallDockPanel의 화상통화 흐름을 그대로 태운다.
public class SupportCallList : MonoBehaviour
{
    public string serverUrl => ServerConfig.BaseUrl + "/api/support-calls";

    [Header("아이템 프리팹 & 컨테이너")]
    public GameObject itemPrefab;
    public Transform itemContainer;

    [Header("통계 텍스트")]
    public TMP_Text statsText; // "총 N건 · 진행중 N건 · 대기 N건"

    [Header("페이지네이션")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageText;
    public int itemsPerPage = 5;

    // 2026-08-26: "지원요청 수락하면 해당 설비의 디지털 트윈 앞으로 텔레포트한 이후에 지원요청
    // 패널이 떠야 함" - MaintenancePopup.cs의 MoveToEquipment()와 동일한 EquipmentMarker/
    // FadeController 조합을 그대로 재사용한다.
    [Header("텔레포트 (디지털 트윈 이동)")]
    public Transform xrRig;

    [Header("도착 연출 (2026-09-03)")]
    [Tooltip("비우면 Camera.main 에서 CameraArrivalMotion 을 찾는다")]
    public CameraArrivalMotion arrivalCamera;
    [Tooltip("수락 시 끌 오브젝트(메뉴 등). 통화 종료 시 자동으로 다시 켜진다")]
    public GameObject[] hideOnArrival;
    [Tooltip("유지보수(통화) 중 비활성화할 컴포넌트(이동/텔레포트 로코모션 등). 종료 시 자동 복구. " +
             "→ 시선(고개 돌리기)은 그대로, 위치 이동만 막음")]
    public Behaviour[] disableDuringCall;
    [Tooltip("도착 연출 테스트(우클릭 메뉴) 대상. 비우면 씬의 첫 EquipmentMarker")]
    public EquipmentMarker testTargetMarker;

    List<SupportCallData> _allItems = new();
    int _currentPage = 0;

    void Start()
    {
        prevButton?.onClick.AddListener(OnPrevPage);
        nextButton?.onClick.AddListener(OnNextPage);
    }

    void OnEnable()
    {
        StartCoroutine(FetchList());
    }

    public void OnRefreshButton()
    {
        StartCoroutine(FetchList());
    }

    IEnumerator FetchList()
    {
        using var req = UnityWebRequest.Get(serverUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[SupportCallList] 목록 조회 실패: " + req.error);
            yield break;
        }

        var response = JsonConvert.DeserializeObject<SupportCallListResponse>(req.downloadHandler.text);
        _allItems = (response != null && response.success && response.data != null) ? response.data : new List<SupportCallData>();
        _currentPage = 0;
        RenderPage();
    }

    void RenderPage()
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        int total = _allItems.Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)total / itemsPerPage));
        int start = _currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, total);

        for (int i = start; i < end; i++)
        {
            GameObject item = Instantiate(itemPrefab, itemContainer);
            var itemUI = item.GetComponent<SupportCallItemUI>();
            if (itemUI != null)
                itemUI.SetData(_allItems[i], OnAcceptClicked, OnRejectClicked);
        }

        int active = _allItems.FindAll(i => i.status == "active").Count;
        int ringing = _allItems.FindAll(i => i.status == "ringing").Count;
        if (statsText)
            statsText.text = $"총 {total}건 · 진행중 {active}건 · 대기 {ringing}건";

        if (pageText)
            pageText.text = total == 0 ? "요청 없음" : $"{_currentPage + 1} / {totalPages}";

        if (prevButton) prevButton.interactable = _currentPage > 0;
        if (nextButton) nextButton.interactable = _currentPage < totalPages - 1;
    }

    public void OnPrevPage()
    {
        if (_currentPage > 0) { _currentPage--; RenderPage(); }
    }

    public void OnNextPage()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_allItems.Count / itemsPerPage));
        if (_currentPage < totalPages - 1) { _currentPage++; RenderPage(); }
    }

    void OnAcceptClicked(SupportCallData data)
    {
        StartCoroutine(AcceptRequest(data));
    }

    IEnumerator AcceptRequest(SupportCallData data)
    {
        var body = new AcceptBody { receiver_id = "MR" };
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
        using var req = new UnityWebRequest(serverUrl + "/" + data.id + "/accept", "PATCH");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[SupportCallList] 수락 실패: " + req.error);
            yield return FetchList(); // 이미 다른 곳에서 처리됐을 수 있으니 최신 상태로 다시 그림
            yield break;
        }

        Debug.Log("[SupportCallList] 지원 요청 수락: " + data.id + " channel=" + data.channel_name);

        // 작업 가이드 "제목"을 실제 설비/유지보수 데이터로 (6단계 지시문은 하드코딩 유지)
        string _n = string.IsNullOrEmpty(data.equipment_name) ? "설비 미지정" : data.equipment_name;
        string _d = !string.IsNullOrEmpty(data.maintenance_description) ? data.maintenance_description
                  : (!string.IsNullOrEmpty(data.work_type) ? data.work_type : "원격 지원");
        WorkGuidePanel.JobTitle = _n + " · " + _d;

        yield return TeleportThenShowDock(data);

        gameObject.SetActive(false); // 목록 화면은 닫고 화상통화 화면으로 넘어간다

    }

    // 해당 설비의 EquipmentMarker(디지털 트윈)를 찾아 그 앞으로 페이드+텔레포트한 뒤에야
    // MrCallDockPanel 등 OnCallAccepted 구독자를 발화시킨다 - "텔레포트한 이후에 패널이 떠야
    // 한다"는 요청이라, 화면이 완전히 어두워진 시점(onMidpoint)에서 위치도 옮기고 패널도 같이
    // 띄운다(MaintenancePopup.MoveToEquipment()와 동일한 패턴).
    IEnumerator TeleportThenShowDock(SupportCallData data)
    {
        EquipmentMarker marker = FindEquipmentMarker(data.equipment_id);
        Transform targetPoint = marker != null ? marker.teleportPoint : null;

        if (targetPoint != null && xrRig != null && FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeAndTeleport(() =>
            {
                // 검은 화면인 동안: 설비 앞으로 순간이동 + 메뉴/이동잠금 + 설비 정보 패널 + 도크 발화.
                xrRig.position = targetPoint.position;
                xrRig.rotation = targetPoint.rotation;
                ApplyArrival(marker);
                // 기존 MrCallDockPanel 등 OnCallAccepted 구독자가 그대로 반응해서 화상통화 화면으로 이어진다.
                VideoCallSignalingMR.RaiseCallAccepted(data.channel_name, data.id);
            });

            // 페이드 인 직후: 카메라가 설비 앞에 이쁘게 자리잡는 짧은 모션(Rig는 고정, 카메라 로컬만).
            var arrivalCam = arrivalCamera != null
                ? arrivalCamera
                : (Camera.main != null ? Camera.main.GetComponentInParent<CameraArrivalMotion>() : null);
            arrivalCam?.Play();

            // 림 글로우 + 바닥 링 하이라이트. marker 자신에게 코루틴을 걸어야 한다 - 이 스크립트(패널)에
            // 걸면 바로 다음 줄 gameObject.SetActive(false)에서 코루틴이 시작하자마자 죽는다.
            marker.StartCoroutine(marker.Highlight());
        }
        else
        {
            if (targetPoint == null)
                Debug.LogWarning("[SupportCallList] 해당 설비의 디지털 트윈을 못 찾음(equipment_id=" + data.equipment_id + ") - 이동 없이 바로 화상통화 화면으로 전환");
            VideoCallSignalingMR.RaiseCallAccepted(data.channel_name, data.id);
        }
    }

    // 서버/AR 체인 없이 도착 연출(페이드+텔레포트+카메라 모션+하이라이트)만 확인하는 테스트.
    // Play 모드에서 컴포넌트 우클릭 → "▶ 도착 연출 테스트". 화상통화(Agora)는 타지 않는다.
    [ContextMenu("▶ 도착 연출 테스트 (씬의 첫 EquipmentMarker로)")]
    void TestArrival()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[SupportCallList] Play 모드에서 실행하세요."); return; }
        var marker = testTargetMarker != null ? testTargetMarker : FindObjectOfType<EquipmentMarker>();
        if (marker == null || marker.teleportPoint == null)
        {
            Debug.LogWarning("[SupportCallList] 테스트 대상 EquipmentMarker(+Teleport Point)가 없습니다. testTargetMarker 를 지정하세요.");
            return;
        }
        Debug.Log("[SupportCallList] 도착 연출 테스트 대상: " + marker.name + " (id=" + marker.equipmentId + ")");
        // 이 패널이 비활성일 수 있으므로(수락 시 SetActive(false)) 항상 켜져 있는 marker에 건다.
        marker.StartCoroutine(TestArrivalCo(marker));
    }

    IEnumerator TestArrivalCo(EquipmentMarker marker)
    {
        var targetPoint = marker.teleportPoint;
        if (xrRig != null && FadeController.Instance != null)
        {
            yield return FadeController.Instance.FadeAndTeleport(() =>
            {
                xrRig.position = targetPoint.position;
                xrRig.rotation = targetPoint.rotation;
                ApplyArrival(marker);
            });
        }
        else
        {
            Debug.LogWarning("[SupportCallList] Xr Rig 또는 FadeController 미설정 - 페이드/이동 없이 효과만.");
            ApplyArrival(marker);
        }
        var cam = arrivalCamera != null
            ? arrivalCamera
            : (Camera.main != null ? Camera.main.GetComponentInParent<CameraArrivalMotion>() : null);
        cam?.Play();
        marker.StartCoroutine(marker.Highlight());
    }

    // 도착 시: 메뉴 숨김 + 유지보수 중 이동잠금(시선은 유지) + 해당 설비 정보 패널 표시.
    // 통화 종료 시 MrArrivalState.RestoreArrival() → 원복.
    void ApplyArrival(EquipmentMarker marker)
    {
        foreach (var go in hideOnArrival)
            if (go != null) go.SetActive(false);
        foreach (var b in disableDuringCall)
            if (b != null) b.enabled = false;

        marker.ShowInfo(xrRig); // 데이터 갱신 + XR Rig 기준 왼쪽에 패널을 월드 고정 표시

        var hides = hideOnArrival;
        var locks = disableDuringCall;
        MrArrivalState.OnRestore = () =>
        {
            foreach (var go in hides) if (go != null) go.SetActive(true);
            foreach (var b in locks) if (b != null) b.enabled = true;
            if (marker != null) marker.HideInfo();
        };
    }

    EquipmentMarker FindEquipmentMarker(string equipmentId)
    {
        if (string.IsNullOrEmpty(equipmentId)) return null;
        foreach (var marker in FindObjectsOfType<EquipmentMarker>())
        {
            if (marker.equipmentId == equipmentId)
                return marker;
        }
        return null;
    }

    void OnRejectClicked(SupportCallData data)
    {
        StartCoroutine(RejectRequest(data));
    }

    IEnumerator RejectRequest(SupportCallData data)
    {
        using var req = new UnityWebRequest(serverUrl + "/" + data.id + "/reject", "PATCH");
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("[SupportCallList] 거절 실패: " + req.error);

        yield return FetchList();
    }

    [Serializable]
    class AcceptBody { public string receiver_id; }
}

[Serializable]
public class SupportCallData
{
    public string id;
    public string initiator_id;
    public string initiator_name;
    public string receiver_id;
    public string initiator_platform;
    public string receiver_platform;
    public string status;
    public string channel_name;
    public string started_at;
    public string ended_at;
    public string maintenance_log_id;
    public string equipment_id;
    public string equipment_name;
    public string maintenance_description;
    public string work_type;
}

[Serializable]
public class SupportCallListResponse
{
    public bool success;
    public List<SupportCallData> data;
}
