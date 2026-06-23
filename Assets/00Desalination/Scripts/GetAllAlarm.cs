using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class GetAllAlarm : MonoBehaviour
{
    [Header("서버 주소")]
    public string serverUrl = "http://192.168.0.66:3000/api/alarm";

    [Header("탭")]
    public bool activeOnly = false;

    [Header("아이템 프리팹 & 부모 오브젝트")]
    public GameObject alarmItemPrefab;  // 알람리스트(1) 프리팹
    public Transform itemContainer;     // 아이템들이 들어갈 부모 오브젝트

    [Header("페이지 버튼")]
    public Button prevButton;
    public Button nextButton;

    [Header("페이지 표시 텍스트 (선택)")]
    public TMP_Text pageText;

    
    private List<AlarmData> _alarms = new List<AlarmData>(); // DB에 있는 알람 리스트
    private int _currentPage = 0; // 현재 페이지
    private const int ITEMS_PER_PAGE = 4; // 페이지당 항목수 4

    // 패널에 붙인 스크립트니까, 패널이 켜지면 코루틴이 실행됨
    void OnEnable()
    {
        StartCoroutine(FetchAlarms());
    }

    // 새로고침 버튼 누를때도, 코루틴이 실행됨
    public void OnRefreshButton()
    {
        StartCoroutine(FetchAlarms());
    }

    // 서버에서 알람 목록 조회
    public IEnumerator FetchAlarms()
    {
        // 서버 url
        string url = activeOnly ? serverUrl + "?active=true" : serverUrl;

        // 서버 url 로 get 요청
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            // 요청을 서버로 보냄
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[GetAllAlarm] 요청 실패: " + req.error);
                yield break;
            }

            // get요청 해서 받아온 모든 알람 데이터를 JSON -> AlarmListResponse 로 변환함
            AlarmListResponse response = JsonUtility.FromJson<AlarmListResponse>(req.downloadHandler.text);

            if (response == null || !response.success)
            {
                Debug.LogError("[GetAllAlarm] 응답 파싱 실패");
                yield break;
            }

            // alarms 는 response의 data
            _alarms = response.data;
            _currentPage = 0; // 현재 페이지 0으로 설정하고 페이지 렌더링하기
            RenderPage();
        }
    }


    // 현재 페이지 렌더링
    void RenderPage()
    {
        // 기존 아이템 모두 제거
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        // 현재 페이지에 해당하는 알람 슬라이스
        int startIndex = _currentPage * ITEMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, _alarms.Count);


        for (int i = startIndex; i < endIndex; i++)
        {
            AlarmData alarm = _alarms[i];

            // itemContainer 안에 alarmItemPrefab을 생성
            GameObject item = Instantiate(alarmItemPrefab, itemContainer);

            // alarmItemPrefab에 alarmItemUI 스크립트를 찾음
            AlarmItemUI itemUI = item.GetComponent<AlarmItemUI>();

            if (itemUI != null)
                itemUI.SetData(alarm); // alarmItemUI 스크립트의 SetData 함수에 alarm 현재 alarm 을 전달
        }

        // 버튼 활성화 여부
        int totalPages = Mathf.CeilToInt((float)_alarms.Count / ITEMS_PER_PAGE);
        prevButton.interactable = _currentPage > 0;
        nextButton.interactable = _currentPage < totalPages - 1;

        // 페이지 텍스트 (선택)
        if (pageText != null)
            pageText.text = _alarms.Count == 0 ? "알람 없음" : $"{_currentPage + 1} / {totalPages}";
    }
    

    // < 버튼(이전)
    public void OnPrevPage()
    {
        // 현재 페이지가 0보다 크면
        if (_currentPage > 0)
        {
            // 현 페이지 -1
            _currentPage--;
            RenderPage();
        }
    }

    // > 버튼(다음)
    public void OnNextPage()
    {
        // 총 알람수 /4 = 모든 페이지수
        int totalPages = Mathf.CeilToInt((float)_alarms.Count / ITEMS_PER_PAGE);
        if (_currentPage < totalPages - 1) // 현재 페이지가 모든 페이지-1 보다 작으면
        {
            // 현 페이지 +1
            _currentPage++;
            RenderPage();
        }
    }
}

// 서버 응답 전체를 담는 클래스
[System.Serializable]
public class AlarmListResponse
{
    public bool success;
    public List<AlarmData> data;
}