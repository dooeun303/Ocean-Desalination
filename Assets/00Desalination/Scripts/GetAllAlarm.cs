using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class GetAllAlarm : MonoBehaviour
{
    public string serverUrl => ServerConfig.BaseUrl + "/api/alarms";

    [Header("��")]
    public bool activeOnly = false;

    [Header("������ ������ & �θ� ������Ʈ")]
    public GameObject alarmItemPrefab;  // �˶�����Ʈ(1) ������
    public Transform itemContainer;     // �����۵��� �� �θ� ������Ʈ

    [Header("������ ��ư")]
    public Button prevButton;
    public Button nextButton;

    [Header("������ ǥ�� �ؽ�Ʈ (����)")]
    public TMP_Text pageText;

    
    private List<AlarmData> _alarms = new List<AlarmData>(); // DB�� �ִ� �˶� ����Ʈ
    private int _currentPage = 0; // ���� ������
    private const int ITEMS_PER_PAGE = 4; // �������� �׸�� 4

    // �гο� ���� ��ũ��Ʈ�ϱ�, �г��� ������ �ڷ�ƾ�� �����
    void OnEnable()
    {
        StartCoroutine(FetchAlarms());
    }

    // ���ΰ�ħ ��ư ��������, �ڷ�ƾ�� �����
    public void OnRefreshButton()
    {
        StartCoroutine(FetchAlarms());
    }

    // �������� �˶� ��� ��ȸ
    public IEnumerator FetchAlarms()
    {
        // ���� url
        string url = activeOnly ? serverUrl + "?active=true" : serverUrl;

        // ���� url �� get ��û
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            // ��û�� ������ ����
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[GetAllAlarm] ��û ����: " + req.error);
                yield break;
            }

            // get��û �ؼ� �޾ƿ� ��� �˶� �����͸� JSON -> AlarmListResponse �� ��ȯ��
            AlarmListResponse response = JsonUtility.FromJson<AlarmListResponse>(req.downloadHandler.text);

            if (response == null || !response.success)
            {
                Debug.LogError("[GetAllAlarm] ���� �Ľ� ����");
                yield break;
            }

            // alarms �� response�� data
            _alarms = response.data;
            _currentPage = 0; // ���� ������ 0���� �����ϰ� ������ �������ϱ�
            RenderPage();
        }
    }


    // ���� ������ ������
    void RenderPage()
    {
        // ���� ������ ��� ����
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // ���� �������� �ش��ϴ� �˶� �����̽�
        int startIndex = _currentPage * ITEMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, _alarms.Count);


        for (int i = startIndex; i < endIndex; i++)
        {
            AlarmData alarm = _alarms[i];

            // itemContainer �ȿ� alarmItemPrefab�� ����
            GameObject item = Instantiate(alarmItemPrefab, itemContainer);

            // alarmItemPrefab�� alarmItemUI ��ũ��Ʈ�� ã��
            AlarmItemUI itemUI = item.GetComponent<AlarmItemUI>();

            if (itemUI != null)
                itemUI.SetData(alarm); // alarmItemUI ��ũ��Ʈ�� SetData �Լ��� alarm ���� alarm �� ����
        }

        // ��ư Ȱ��ȭ ����
        int totalPages = Mathf.CeilToInt((float)_alarms.Count / ITEMS_PER_PAGE);
        prevButton.interactable = _currentPage > 0;
        nextButton.interactable = _currentPage < totalPages - 1;

        // ������ �ؽ�Ʈ (����)
        if (pageText != null)
            pageText.text = _alarms.Count == 0 ? "�˶� ����" : $"{_currentPage + 1} / {totalPages}";
    }
    

    // < ��ư(����)
    public void OnPrevPage()
    {
        // ���� �������� 0���� ũ��
        if (_currentPage > 0)
        {
            // �� ������ -1
            _currentPage--;
            RenderPage();
        }
    }

    // > ��ư(����)
    public void OnNextPage()
    {
        // �� �˶��� /4 = ��� ��������
        int totalPages = Mathf.CeilToInt((float)_alarms.Count / ITEMS_PER_PAGE);
        if (_currentPage < totalPages - 1) // ���� �������� ��� ������-1 ���� ������
        {
            // �� ������ +1
            _currentPage++;
            RenderPage();
        }
    }
}

// ���� ���� ��ü�� ��� Ŭ����
[System.Serializable]
public class AlarmListResponse
{
    public bool success;
    public List<AlarmData> data;
}