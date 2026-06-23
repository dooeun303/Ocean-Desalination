using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

// =============================================
// ����͸� �������� ���̺�
// - ��ü / ���º� ���͸� ��ȸ
// - ��ũ�� ��� (���������̼� ����)
// - �� ���� MaintenanceRowUI Prefab���� ����
// =============================================
public class MonitoringMaintenanceTable : MonoBehaviour, IPageRefreshable
{
    public void OnPageRefresh()
    {
        OnRefreshButton();
    }

    [Header("���� ����")]
    public string serverUrl = "http://localhost:3000/api/maintenance";

    [Header("���̺� UI")]
    public Transform rowContainer;      // Scroll View > Viewport > Content
    public GameObject rowPrefab;        // MaintenanceRowUI �پ��ִ� Prefab

    [Header("���� ���� ��Ӵٿ�")]
    public TMP_Dropdown statusDropdown; // ��ü / ���� / ���� �� / �Ϸ�
    [Header("�Ǽ� �ؽ�Ʈ")]
    public TMP_Text countText;  // ��) "�� 24��"   


    string _currentFilter = null;       // null = ��ü

    // =============================================
    // �ʱ�ȭ
    // =============================================
    void Start()
    {
        statusDropdown?.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnEnable()
    {
        StartCoroutine(FetchAll());
    }

    // =============================================
    // ��Ӵٿ� ���� �� ȣ��
    // =============================================
    void OnDropdownChanged(int index)
    {
        string[] statuses = { null, "scheduled", "in_progress", "completed" };
        _currentFilter = statuses[index];
        StartCoroutine(FetchAll());
    }

    // =============================================
    // ���ΰ�ħ ��ư�� (���û���)
    // =============================================
    public void OnRefreshButton()
    {
        StartCoroutine(FetchAll());
    }

    // =============================================
    // ������ ��ȸ + �� ����
    // =============================================
    IEnumerator FetchAll()
    {
        // ���� �� ����
        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);

        List<MaintenanceData> allItems = new();

        if (_currentFilter == null)
        {
            // ��ü: scheduled �� in_progress �� completed ����
            string[] statuses = { "scheduled", "in_progress", "completed" };
            foreach (string status in statuses)
            {
                string url = serverUrl + "?status=" + UnityWebRequest.EscapeURL(status);
                yield return StartCoroutine(FetchStatus(url, allItems));
            }
        }
        else
        {
            // ���� ����
            string url = serverUrl + "?status=" + UnityWebRequest.EscapeURL(_currentFilter);
            yield return StartCoroutine(FetchStatus(url, allItems));
        }

        // �� ����
        foreach (var item in allItems)
        {
            GameObject row = Instantiate(rowPrefab, rowContainer);
            row.GetComponent<MaintenanceRowUI>()?.SetData(item);
        }

        // �Ǽ� ������Ʈ  �� �� �� �߰�
        if (countText) countText.text = $"�� {allItems.Count}��";
    }

    // =============================================
    // ���� ���� API ȣ��
    // =============================================
    IEnumerator FetchStatus(string url, List<MaintenanceData> result)
    {
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[MaintenanceTable] ��û ����: " + req.error);
            yield break;
        }

        var response = JsonUtility.FromJson<MaintenanceListResponse>(req.downloadHandler.text);
        if (response != null && response.success && response.data != null)
            result.AddRange(response.data);
    }
}