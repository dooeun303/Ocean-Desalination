using System.Collections;
using UnityEngine;
using TMPro;

// 화상통화 요약 결과 표시 스크립트
public class SummaryDisplay : MonoBehaviour
{
    public static SummaryDisplay Instance;

    [Header("UI 연결")]
    public GameObject summaryPanel;     // 요약 결과 패널
    public TMP_Text summaryText;      // 요약 텍스트
    public TMP_Text transcriptText;   // 원문 텍스트 (선택)
    public GameObject loadingPanel;     // 로딩 패널 ("요약 중..." 표시)
    public TMP_Text loadingText;      // 로딩 텍스트
    public UnityEngine.UI.Button closeButton; // 닫기 버튼

    // SummaryDisplay 싱글톤
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 요약 패널이랑 로딩 패널 닫아두기.
    // 닫기버튼 리스너 연결
    void Start()
    {
        closeButton?.onClick.AddListener(Close);
        summaryPanel?.SetActive(false);
        loadingPanel?.SetActive(false); 
    }

    // 로딩 표시 (전송 중)
    public void ShowLoading()
    {
        summaryPanel?.SetActive(false);
        loadingPanel?.SetActive(true); // 로딩패널 활성화
        if (loadingText) loadingText.text = "화상통화 내용을 요약하는 중...";
    }

    // 요약 결과 표시
    public void ShowSummary(string summary, string transcript = "")
    {
        loadingPanel?.SetActive(false);// 로딩패널 비활성화
        summaryPanel?.SetActive(true); // 요약패널 활성화

        if (summaryText) summaryText.text = summary; // 요약 텍스트
        if (transcriptText) transcriptText.text = transcript; // 원본 필사 텍스트
    }

    // 오류 표시
    public void ShowError(string message)
    {
        loadingPanel?.SetActive(false); // 로딩패널 활성화
        summaryPanel?.SetActive(true); // 요약패널 활성화
        if (summaryText) summaryText.text = $"오류: {message}";
    }

    // 닫기
    public void Close()
    {
        summaryPanel?.SetActive(false); // 요약, 로딩 패널 비활성화
        loadingPanel?.SetActive(false);
    }
}