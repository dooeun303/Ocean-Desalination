using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class InspectionList : MonoBehaviour
{
    public static InspectionList Instance;

    public string baseUrl => ServerConfig.BaseUrl;

    [Header("��� UI")]
    public Transform itemContainer;
    public GameObject itemPrefab;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public GameObject loadingIndicator;

    [Header("���������̼�")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageText;
    public int itemsPerPage = 5;

    private List<InspectionItem> _allItems = new();
    private int _currentPage = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        prevButton?.onClick.AddListener(OnPrevPage);
        nextButton?.onClick.AddListener(OnNextPage);
    }

    void OnEnable()
    {
        LoadList();
    }

    public void LoadList()
    {
        if (loadingIndicator) loadingIndicator.SetActive(true);

        StartCoroutine(Get<InspectionListResponse>(
            "/api/inspections/today",
            onSuccess: (response) =>
            {
                if (loadingIndicator) loadingIndicator.SetActive(false);

                if (!response.success || response.data == null)
                {
                    if (descriptionText) descriptionText.text = "�����͸� �ҷ��� �� �����ϴ�.";
                    return;
                }

                _allItems = new List<InspectionItem>(response.data);
                _currentPage = 0;

                int total = _allItems.Count;
                int pending = _allItems.FindAll(i => i.inspection_status == "pending").Count;
                int completed = _allItems.FindAll(i => i.inspection_status == "completed").Count;

                if (titleText) titleText.text = "������ ��������";
                if (descriptionText) descriptionText.text =
                    $"�� {total}�� �� ������ {pending}�� �� ��� {total - completed - pending}��";

                RenderPage();
            },
            onError: (err) =>
            {
                if (loadingIndicator) loadingIndicator.SetActive(false);
                if (descriptionText) descriptionText.text = "���� ���� ����";
                Debug.LogError("[InspectionList] " + err);
            }
        ));
    }

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
            var go = Instantiate(itemPrefab, itemContainer);
            go.GetComponent<InspectionItemUI>()?.Bind(_allItems[i]);
        }

        if (pageText)
            pageText.text = total == 0 ? "������ ����" : $"{_currentPage + 1} / {totalPages}";

        if (prevButton) prevButton.interactable = _currentPage > 0;
        if (nextButton) nextButton.interactable = _currentPage < totalPages - 1;
    }

    public void OnPrevPage()
    {
        if (_currentPage > 0) { _currentPage--; RenderPage(); }
    }

    public void OnNextPage()
    {
        int totalPages = Mathf.CeilToInt((float)_allItems.Count / itemsPerPage);
        if (_currentPage < totalPages - 1) { _currentPage++; RenderPage(); }
    }

    public void GetSensors(MonoBehaviour caller, string equipmentId, Action<SensorListResponse> onSuccess, Action<string> onError)
    {
        caller.StartCoroutine(Get<SensorListResponse>(
            $"/api/equipment/{equipmentId}/sensors", onSuccess, onError));
    }

    public void SaveResult(MonoBehaviour caller, SaveInspectionRequest request, Action<SaveInspectionResponse> onSuccess, Action<string> onError)
    {
        caller.StartCoroutine(Patch<SaveInspectionResponse>($"/api/inspections/{request.inspection_id}", request, onSuccess, onError));
    }

    private IEnumerator Get<T>(string path, Action<T> onSuccess, Action<string> onError)
    {
        using var req = UnityWebRequest.Get(baseUrl + path);
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try { onSuccess?.Invoke(JsonUtility.FromJson<T>(req.downloadHandler.text)); }
            catch (Exception e) { onError?.Invoke("JSON �Ľ� ����: " + e.Message); }
        }
        else onError?.Invoke($"GET {path} ����: {req.error}");
    }



    private IEnumerator Patch<T>(string path, object body, Action<T> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest(baseUrl + path, "PATCH");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try { onSuccess?.Invoke(JsonUtility.FromJson<T>(req.downloadHandler.text)); }
            catch (Exception e) { onError?.Invoke("JSON �Ľ� ����: " + e.Message); }
        }
        else onError?.Invoke($"PATCH {path} ����: {req.error}");
    }
}