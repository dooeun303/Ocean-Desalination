using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 고장예지(6-1) 페이지: 검색 입력/버튼으로 MockStore를 조회해
/// 결과를 테이블(body)에 행 템플릿을 복제하며 표출한다.
/// </summary>
public class FaultPredictionTableController : MonoBehaviour, IPageRefreshable
{
    public void OnPageRefresh()
    {
        OnSearchClicked();
    }

    [Header("UI 참조")]
    [Tooltip("검색어 입력 필드 (고장예지 검색/입력)")]
    [SerializeField] private TMP_InputField searchInput;
    [Tooltip("검색 버튼 (고장예지 검색/검색버튼)")]
    [SerializeField] private Button searchButton;
    [Tooltip("새로고침 버튼 (선택)")]
    [SerializeField] private Button refreshButton;
    [Tooltip("행을 담는 컨테이너 (table/body)")]
    [SerializeField] private Transform bodyContainer;
    [Tooltip("복제할 행 템플릿 (table/body/행 (1))")]
    [SerializeField] private GameObject rowTemplate;

    [Header("빈 상태")]
    [Tooltip("결과가 없을 때 표시할 텍스트 (선택)")]
    [SerializeField] private TMP_Text emptyStateText;

    [Header("등급/상태 색상")]
    [SerializeField] private Color criticalColor = new Color32(0xEF, 0x44, 0x44, 0xFF); // 위험
    [SerializeField] private Color warningColor = new Color32(0xF5, 0x9E, 0x0B, 0xFF);  // 경고
    [SerializeField] private Color normalColor = new Color32(0x50, 0xB8, 0xB8, 0xFF);   // 정상
    [SerializeField] private Color releasedColor = new Color32(0x5F, 0x6F, 0x85, 0xFF); // 해제

    private readonly List<GameObject> _spawnedRows = new List<GameObject>();
    private bool _isQuerying;

    private void Start()
    {
        // 템플릿을 제외한 기존 샘플 행 정리 + 템플릿 비활성화
        if (bodyContainer != null && rowTemplate != null)
        {
            var toRemove = new List<GameObject>();
            foreach (Transform child in bodyContainer)
            {
                if (child.gameObject != rowTemplate)
                    toRemove.Add(child.gameObject);
            }
            foreach (var go in toRemove) Destroy(go);
            rowTemplate.SetActive(false);
        }

        if (searchButton != null) searchButton.onClick.AddListener(OnSearchClicked);
        if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
        if (searchInput != null) searchInput.onSubmit.AddListener(_ => OnSearchClicked());

        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);

        // 최초 진입 시 전체 로드
        LoadData("");
    }

    public void OnSearchClicked()
    {
        LoadData(searchInput != null ? searchInput.text : "");
    }

    public void OnRefreshClicked()
    {
        if (searchInput != null) searchInput.text = "";
        LoadData("");
    }

    private async void LoadData(string keyword)
    {
        if (_isQuerying) return;
        _isQuerying = true;
        if (searchButton != null) searchButton.interactable = false;

        try
        {
            List<AlarmRecord> records = await MonitoringMockDataStore.SearchAlarms("");
            BuildRows(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FaultPrediction] Mock 조회 실패: {e.Message}\n{e}");
            BuildRows(new List<AlarmRecord>());
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(true);
                emptyStateText.text = "데이터를 불러오지 못했습니다";
            }
        }
        finally
        {
            _isQuerying = false;
            if (searchButton != null) searchButton.interactable = true;
        }
    }

    private void BuildRows(List<AlarmRecord> records)
    {
        // 이전 결과 제거
        foreach (var go in _spawnedRows)
            if (go != null) Destroy(go);
        _spawnedRows.Clear();

        if (bodyContainer == null || rowTemplate == null)
        {
            Debug.LogError("[FaultPrediction] bodyContainer 또는 rowTemplate이 연결되지 않았습니다.");
            return;
        }

        bool empty = records == null || records.Count == 0;
        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(empty);
            if (empty) emptyStateText.text = "데이터가 없습니다";
        }
        if (empty) return;

        foreach (var rec in records)
        {
            GameObject row = Instantiate(rowTemplate, bodyContainer);
            row.SetActive(true);
            row.transform.SetAsLastSibling();
            FillRow(row, rec);
            _spawnedRows.Add(row);
        }
    }

    private void CreateHeaderRow()
    {
        Transform tableRoot = transform;
        Transform scrollView = transform.Find("ScrollView");
        if (scrollView == null)
        {
            // fallback search in children
            scrollView = GetComponentInChildren<ScrollRect>(true)?.transform;
            if (scrollView != null && scrollView.parent != transform)
            {
                Transform p = scrollView;
                while (p != null && p.parent != transform)
                {
                    p = p.parent;
                }
                scrollView = p;
            }
        }

        GameObject header;
        if (tableRoot != null && scrollView != null)
        {
            header = Instantiate(rowTemplate, tableRoot);
            header.name = "TableHeader";
            header.SetActive(true);
            header.transform.SetSiblingIndex(scrollView.GetSiblingIndex()); // Position right above ScrollView
        }
        else
        {
            header = Instantiate(rowTemplate, bodyContainer);
            header.name = "TableHeader";
            header.SetActive(true);
            header.transform.SetAsFirstSibling();
        }
        
        var btn = header.GetComponent<Button>();
        if (btn != null) Destroy(btn);
        
        var rowImg = header.GetComponent<UnityEngine.UI.Image>();
        if (rowImg != null)
        {
            rowImg.color = new Color32(234, 243, 245, 255); // #EAF3F5 (옅은 배경)
            rowImg.raycastTarget = false;
        }

        Transform group = header.transform.Find("셀그룹");
        if (group == null) group = header.transform;

        int index = 0;
        string[] headers = { "알람 ID", "발생일시", "설비명", "위치", "고장예지 내용", "심각도", "상태" };

        foreach (Transform cell in group)
        {
            var txt = cell.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                if (index < headers.Length)
                {
                    cell.gameObject.SetActive(true);
                    txt.text = headers[index];
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = new Color32(26, 32, 45, 255); // #1A202D (중요 텍스트)
                    index++;
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
            }
            
            foreach (var g in cell.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                g.raycastTarget = false;
            }
        }
        
        ConfigureRowLayout(header);
        _spawnedRows.Add(header);
    }

    private void ConfigureRowLayout(GameObject row)
    {
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false; // MUST be false so flexibleWidth=0 items (separators) don't stretch!
        }

        Transform group = row.transform.Find("셀그룹");
        if (group == null) group = row.transform;

        int textCellIndex = 0;
        // 알람 ID, 발생일시, 설비명, 위치, 고장예지 내용, 심각도, 상태
        float[] flexWidths = { 1.0f, 1.3f, 1.4f, 1.0f, 3.8f, 1.0f, 1.0f };

        foreach (Transform child in group)
        {
            var txt = child.GetComponentInChildren<TMP_Text>(true);
            var le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.gameObject.AddComponent<LayoutElement>();

            if (txt != null)
            {
                if (child.gameObject.activeSelf && textCellIndex < flexWidths.Length)
                {
                    // Enable ellipsis and disable word wrap for clean single-line truncation
                    txt.overflowMode = TextOverflowModes.Ellipsis;
                    txt.textWrappingMode = TextWrappingModes.NoWrap;

                    le.flexibleWidth = flexWidths[textCellIndex];
                    le.preferredWidth = 0f; // Force layout calculation strictly by flexible weights for perfect alignment
                    le.minWidth = 30f;
                    textCellIndex++;
                }
                else
                {
                    child.gameObject.SetActive(false);
                    le.flexibleWidth = 0f;
                    le.preferredWidth = 0f;
                    le.minWidth = 0f;
                }
            }
            else if (child.name.Contains("구분선"))
            {
                if (textCellIndex < flexWidths.Length)
                {
                    child.gameObject.SetActive(true);
                    le.flexibleWidth = 0f;
                    le.preferredWidth = 1f;
                    le.minWidth = 1f;
                }
                else
                {
                    child.gameObject.SetActive(false);
                    le.flexibleWidth = 0f;
                    le.preferredWidth = 0f;
                    le.minWidth = 0f;
                }
            }
        }
    }

    private void FillRow(GameObject row, AlarmRecord rec)
    {
        Transform group = row.transform.Find("셀그룹");
        if (group == null) group = row.transform; // 안전장치

        // 셀그룹 직계 자식(셀, 셀(1)...셀(6))의 TMP_Text를 순서대로 채운다
        var cellTexts = new List<TMP_Text>();
        int cellCount = 0;
        foreach (Transform cell in group)
        {
            var txt = cell.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                if (cellCount < 7)
                {
                    cell.gameObject.SetActive(true);
                    cellTexts.Add(txt);
                    cellCount++;
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
            }
        }

        if (cellTexts.Count < 7)
        {
            Debug.LogWarning($"[FaultPrediction] 셀 개수가 부족합니다 ({cellTexts.Count}/7).");
            return;
        }

        cellTexts[0].text = rec.AlarmCode;
        cellTexts[0].color = new Color32(61, 90, 106, 255); // #3D5A6A (일반 본문)
        
        cellTexts[1].text = rec.TriggeredAt.ToString("yyyy.MM.dd HH:mm");
        cellTexts[1].color = new Color32(122, 158, 171, 255); // #7A9EAB (보조 텍스트)
        
        cellTexts[2].text = rec.EquipmentName;
        cellTexts[2].color = new Color32(61, 90, 106, 255);
        
        cellTexts[3].text = rec.Location;
        cellTexts[3].color = new Color32(122, 158, 171, 255);
        
        cellTexts[4].text = rec.Description;
        cellTexts[4].color = new Color32(61, 90, 106, 255);

        cellTexts[5].text = rec.SeverityLabel;
        cellTexts[5].color = GetSeverityColor(rec.Severity);

        cellTexts[6].text = rec.StatusLabel;
        cellTexts[6].color = rec.IsActive ? criticalColor : releasedColor;

        foreach (var graphic in row.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        ConfigureRowLayout(row);
    }

    private Color GetSeverityColor(string severity)
    {
        switch ((severity ?? "").ToLowerInvariant())
        {
            case "critical": return criticalColor;
            case "warning": return warningColor;
            default: return normalColor;
        }
    }
}
