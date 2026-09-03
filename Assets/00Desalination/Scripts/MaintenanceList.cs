using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class MaintenanceList : MonoBehaviour
{
    [Header("서버 주소")]
    public string serverUrl = "http://192.168.0.66:3000/api/maintenance";

    [Header("아이템 프리팹 & 컨테이너")]
    public GameObject itemPrefab;
    public Transform itemContainer;

    [Header("팝업")]
    public MaintenancePopup popup;

    [Header("통계 텍스트")]
    public TMP_Text statsText;            // "총 N건 · 진행중 N건 · 대기 N건"

    [Header("페이지네이션")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageText;
    public int itemsPerPage = 5;

    private List<MaintenanceData> _allItems = new();
    private int _currentPage = 0;

    void Start()
    {
        prevButton?.onClick.AddListener(OnPrevPage);
        nextButton?.onClick.AddListener(OnNextPage);
    }

    void OnEnable()
    {
        Debug.Log("[Maintenance] OnEnable 호출됨");
        StartCoroutine(FetchList());
    }

    public void OnRefreshButton()
    {
        StartCoroutine(FetchList());
    }

    // ─────────────────────────────────────────
    // 목록 불러오기
    // ─────────────────────────────────────────
    public IEnumerator FetchList()
    {
        Debug.Log("[Maintenance] FetchList 시작");
        List<MaintenanceData> result = new List<MaintenanceData>();

        string[] statuses = { "scheduled", "in_progress", "completed" };

        foreach (string status in statuses)
        {
            string url = serverUrl + "?status=" + UnityWebRequest.EscapeURL(status);
            Debug.Log("[Maintenance] 요청 URL: " + url);

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[Maintenance] 요청 실패: " + req.error);
                    continue;
                }

                MaintenanceListResponse response = JsonUtility.FromJson<MaintenanceListResponse>(req.downloadHandler.text);
                if (response != null && response.success)
                    result.AddRange(response.data);
            }
        }

        _allItems = result;

        // 데모용 고정 항목 - MR에서 유지보수 목록을 열면 항상 맨 위에 이 건(펌프 베어링 마모 의심
        // 점검)이 보여야 한다는 요청 - 실제 정렬(scheduled_at DESC)과 무관하게 앞으로 당겨온다.
        const string PinnedDemoItemId = "c7ddf74a-6bd1-444c-875d-f40fcc4ada6f";
        int pinnedIdx = _allItems.FindIndex(i => i.id == PinnedDemoItemId);
        if (pinnedIdx > 0)
        {
            var pinned = _allItems[pinnedIdx];
            _allItems.RemoveAt(pinnedIdx);
            _allItems.Insert(0, pinned);
        }

        _currentPage = 0;
        RenderPage();
    }

    // ─────────────────────────────────────────
    // 페이지 렌더링
    // ─────────────────────────────────────────
    private void RenderPage()
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        int total = _allItems.Count;
        int totalPages = Mathf.CeilToInt((float)total / itemsPerPage);
        int start = _currentPage * itemsPerPage;
        int end = Mathf.Min(start + itemsPerPage, total);

        for (int i = start; i < end; i++)
        {
            GameObject item = Instantiate(itemPrefab, itemContainer);
            MaintenanceItemUI itemUI = item.GetComponent<MaintenanceItemUI>();
            if (itemUI != null)
                itemUI.SetData(_allItems[i], OnItemClicked);
        }

        // 통계
        int inProgress = _allItems.FindAll(i => i.status == "in_progress").Count;
        int scheduled = _allItems.FindAll(i => i.status == "scheduled").Count;
        int completed = _allItems.FindAll(i => i.status == "completed").Count;
        if (statsText)
            statsText.text = $"총 {total}건 · 진행중 {inProgress}건 · 대기 {scheduled}건 · 완료 {completed}건";

        if (pageText)
            pageText.text = total == 0 ? "데이터 없음" : $"{_currentPage + 1} / {totalPages}";

        if (prevButton) prevButton.interactable = _currentPage > 0;
        if (nextButton) nextButton.interactable = _currentPage < totalPages - 1;
    }

    // ─────────────────────────────────────────
    // 페이지 이동
    // ─────────────────────────────────────────
    public void OnPrevPage()
    {
        if (_currentPage > 0) { _currentPage--; RenderPage(); }
    }

    public void OnNextPage()
    {
        int totalPages = Mathf.CeilToInt((float)_allItems.Count / itemsPerPage);
        if (_currentPage < totalPages - 1) { _currentPage++; RenderPage(); }
    }

    void OnItemClicked(MaintenanceData data)
    {
        popup?.Open(data);
    }
}


[System.Serializable]
public class MaintenanceData
{
    public string id;
    public string work_type;
    public string description;
    public string result;
    public string ai_summary;
    public string status;
    public string scheduled_at;
    public string completed_at;
    public string technician_id;
    public string technician_name;  // ← 추가
    public MaintenanceEquipment equipment;
    public MaintenanceManual manual;
    public MaintenanceAlarm latest_alarm;
}

[System.Serializable]
public class MaintenanceEquipment
{
    public string id;
    public string name;
    public string mr_space_id;
}

[System.Serializable]
public class MaintenanceManual
{
    public string id;
    public string title;
    public string category;
    public string content_url;
}

[System.Serializable]
public class MaintenanceAlarm
{
    public string id;
    public string alarm_code;
    public string severity;
    public string description;
    public string triggered_at;
}

[System.Serializable]
public class MaintenanceListResponse
{
    public bool success;
    public System.Collections.Generic.List<MaintenanceData> data;
}