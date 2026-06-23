using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원격점검(6-3) 페이지: 검색 입력/버튼으로 PostgreSQL을 조회해
/// 결과를 테이블(body)에 행 템플릿을 복제하며 표출한다.
/// </summary>
public class RemoteInspectionTableController : MonoBehaviour, IPageRefreshable
{
    public void OnPageRefresh()
    {
        OnSearchClicked();
    }

    [Header("DB 접속 정보")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5433;
    [SerializeField] private string database = "test";
    [SerializeField] private string user = "postgres";
    [SerializeField] private string password = "0000";
    [SerializeField] private string schema = "aaa";

    [Header("UI 참조")]
    [Tooltip("검색어 입력 필드 (고장예지 검색/입력)")]
    [SerializeField] private TMP_InputField searchInput;
    [Tooltip("검색 버튼 (고장예지 검색/검색버튼)")]
    [SerializeField] private Button searchButton;
    [Tooltip("행을 담는 컨테이너 (table/body)")]
    [SerializeField] private Transform bodyContainer;
    [Tooltip("복제할 행 템플릿 (table/body/행 (1))")]
    [SerializeField] private GameObject rowTemplate;

    [Header("빈 상태")]
    [Tooltip("결과가 없을 때 표시할 텍스트 (선택)")]
    [SerializeField] private TMP_Text emptyStateText;

    [Header("텍스트 색상")]
    [SerializeField] private Color defaultTextColor = new Color32(126, 157, 171, 255); // #7E9DAB
    [SerializeField] private Color pendingColor = new Color32(245, 158, 11, 255);       // #F59E0B (대기)
    [SerializeField] private Color inProgressColor = new Color32(80, 184, 184, 255);     // #50B8B8 (진행중)
    [SerializeField] private Color completedColor = new Color32(80, 184, 184, 255);      // #50B8B8 (완료)

    private PostgresInspectionService _service;
    private readonly List<GameObject> _spawnedRows = new List<GameObject>();
    private bool _isQuerying;

    // 선택 기능 추가
    private InspectionRecord _selectedRecord = null;
    private GameObject _selectedRowObject = null;
    private Color _normalRowColor = new Color32(255, 255, 255, 0); // 투명
    private Color _selectedRowColor = new Color32(230, 240, 250, 255); // 연한 파란색

    private void Awake()
    {
        _service = new PostgresInspectionService(host, port, database, user, password, schema);

        // Ensure KPI and Charts are attached and have DB credentials at runtime
        var kpi = GetComponentInChildren<RemoteInspectionKPI>(true);
        if (kpi != null)
        {
            kpi.host = host; kpi.port = port; kpi.database = database; kpi.user = user; kpi.password = password; kpi.schema = schema;
        }

        var line = GetComponentInChildren<RemoteInspectionLineChart>(true);
        if (line != null)
        {
            line.host = host; line.port = port; line.database = database; line.user = user; line.password = password; line.schema = schema;
        }

        var bar = GetComponentInChildren<RemoteInspectionBarChart>(true);
        if (bar != null)
        {
            bar.host = host; bar.port = port; bar.database = database; bar.user = user; bar.password = password; bar.schema = schema;
        }

        var pie = GetComponentInChildren<RemoteInspectionStatusChart>(true);
        if (pie != null)
        {
            pie.host = host; pie.port = port; pie.database = database; pie.user = user; pie.password = password; pie.schema = schema;
        }
    }

    private void Start()
    {
        // 템플릿을 제외한 기존 샘플 행 정리 및 템플릿 비활성화
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
        if (searchInput != null) searchInput.onSubmit.AddListener(_ => OnSearchClicked());

        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);

        // 원격점검 버튼 안전 장치 및 로직 연결
        Button[] globalButtons = Object.FindObjectsOfType<Button>(true);
        foreach (var btn in globalButtons)
        {
            var txt = btn.GetComponentInChildren<TMP_Text>(true);
            var legacyTxt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            string btnText = txt != null ? txt.text.Trim() : (legacyTxt != null ? legacyTxt.text.Trim() : "");
            
            if (string.IsNullOrEmpty(btnText) || btn == searchButton) continue;

            // 팝업 내부에 있는 제출(등록/수정) 버튼 등은 제외
            if (btn.GetComponentInParent<MaintenanceRegisterPopup>(true) != null) continue;
            if (btn.GetComponentInParent<DeleteConfirmPopup>(true) != null) continue;
            if (btn.GetComponentInParent<RemoteInspectionRegisterPopup>(true) != null) continue;

            // 유지보수용 버튼은 원격점검 패널에서 열지 않도록 예외 처리
            Transform p = btn.transform.parent;
            bool isMaintenance = false;
            while (p != null)
            {
                if (p.name.Contains("유지") || p.name.Contains("Maintenance")) { isMaintenance = true; break; }
                p = p.parent;
            }
            if (isMaintenance) continue;

            if (btnText.Contains("수정"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[RemoteInspection] 상단 수정 버튼 클릭됨. 선택된 레코드: {(_selectedRecord != null ? _selectedRecord.DisplayId : "없음")}");
                    if (_selectedRecord == null)
                    {
                        Debug.LogWarning("[RemoteInspection] 수정할 항목을 먼저 선택하세요.");
                        return;
                    }
                    var popup = Object.FindObjectOfType<RemoteInspectionRegisterPopup>(true);
                    if (popup != null) popup.Open(_selectedRecord);
                });
            }
            else if (btnText.Contains("삭제"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[RemoteInspection] 상단 삭제 버튼 클릭됨. 선택된 레코드: {(_selectedRecord != null ? _selectedRecord.DisplayId : "없음")}");
                    if (_selectedRecord == null)
                    {
                        Debug.LogWarning("[RemoteInspection] 삭제할 항목을 먼저 선택하세요.");
                        return;
                    }
                    
                    var confirmPopup = Object.FindObjectOfType<DeleteConfirmPopup>(true);
                    if (confirmPopup != null)
                    {
                        confirmPopup.Show("원격점검 삭제", $"선택하신 기록(ID: {_selectedRecord.DisplayId})을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", async () =>
                        {
                            bool success = await _service.DeleteInspectionAsync(_selectedRecord.Id);
                            if (success) LoadData(searchInput != null ? searchInput.text : "");
                        });
                    }
                    else
                    {
                        async void DeleteDirectly() {
                            bool success = await _service.DeleteInspectionAsync(_selectedRecord.Id);
                            if (success) LoadData(searchInput != null ? searchInput.text : "");
                        }
                        DeleteDirectly();
                    }
                });
            }
            else if (btnText.Contains("등록"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => 
                {
                    var popup = Object.FindObjectOfType<RemoteInspectionRegisterPopup>(true);
                    if (popup != null) popup.Open();
                    else Debug.LogError("RemoteInspectionRegisterPopup을 씬에서 찾을 수 없습니다.");
                });
            }
        }

        // 최초 진입 시 전체 로드
        LoadData("");
    }

    public void OnSearchClicked()
    {
        LoadData(searchInput != null ? searchInput.text : "");
    }

    private async void LoadData(string keyword)
    {
        if (_isQuerying || _service == null) return;
        _isQuerying = true;
        if (searchButton != null) searchButton.interactable = false;

        try
        {
            List<InspectionRecord> records = await _service.SearchInspectionsAsync(keyword);
            BuildRows(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RemoteInspection] DB 조회 실패: {e.Message}\n{e}");
            BuildRows(new List<InspectionRecord>());
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

    private void BuildRows(List<InspectionRecord> records)
    {
        foreach (var go in _spawnedRows)
            if (go != null) Destroy(go);
        _spawnedRows.Clear();
        _selectedRecord = null;
        _selectedRowObject = null;

        if (bodyContainer == null || rowTemplate == null)
        {
            Debug.LogError("[RemoteInspection] bodyContainer 또는 rowTemplate이 연결되지 않았습니다.");
            return;
        }

        // 1. 헤더 행 생성
        CreateHeaderRow();

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
        string[] headers = { "작업 ID", "점검일시", "점검자", "상태", "비고" };

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
            hlg.childForceExpandWidth = false; // Prevent separators from stretching!
        }

        Transform group = row.transform.Find("셀그룹");
        if (group == null) group = row.transform;

        int textCellIndex = 0;
        // 작업 ID, 점검일시, 점검자, 상태, 비고 (5열)
        float[] flexWidths = { 1.0f, 1.4f, 1.2f, 1.0f, 3.0f };

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

    private void FillRow(GameObject row, InspectionRecord rec)
    {
        Transform group = row.transform.Find("셀그룹");
        if (group == null) group = row.transform;

        var cellTexts = new List<TMP_Text>();
        int cellCount = 0;
        foreach (Transform cell in group)
        {
            var txt = cell.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                if (cellCount < 5)
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

        if (cellTexts.Count < 5)
        {
            Debug.LogWarning($"[RemoteInspection] 셀 개수가 부족합니다 ({cellTexts.Count}/5).");
            // Fallback: try finding all TMP_Texts in children
            if (cellTexts.Count < 5)
            {
                cellTexts.Clear();
                cellTexts.AddRange(row.GetComponentsInChildren<TMP_Text>(true));
            }
        }

        if (cellTexts.Count < 5)
        {
            Debug.LogWarning($"[RemoteInspection] 셀 개수가 최종적으로 부족합니다 ({cellTexts.Count}/5).");
            return;
        }

        // 1. 작업 ID
        cellTexts[0].text = rec.DisplayId;
        cellTexts[0].color = defaultTextColor;

        // 2. 날짜
        cellTexts[1].text = rec.InspectionDate.ToString("yyyy.MM.dd HH:mm");
        cellTexts[1].color = defaultTextColor;

        // 3. 담당자
        cellTexts[2].text = rec.InspectorName;
        cellTexts[2].color = defaultTextColor;

        // 4. 상태
        cellTexts[3].text = rec.StatusLabel;
        cellTexts[3].color = GetStatusColor(rec.Result);

        // 5. 비고
        cellTexts[4].text = rec.Note;
        cellTexts[4].color = defaultTextColor;

        // 자식 UI 요소들(텍스트, 배경 등)이 클릭(Raycast)을 가로채지 못하도록 모두 끕니다.
        foreach (var graphic in row.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        // 행 자체에 배경 이미지와 버튼을 추가하여 클릭을 받습니다.
        var rowImage = row.GetComponent<Image>();
        if (rowImage == null) rowImage = row.AddComponent<Image>();
        rowImage.color = _normalRowColor;
        rowImage.raycastTarget = true; // 행 자체는 클릭을 받아야 함

        var btn = row.GetComponent<Button>();
        if (btn == null) btn = row.AddComponent<Button>();
        
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            // 기존 선택된 행 색상 원복
            if (_selectedRowObject != null)
            {
                var oldImg = _selectedRowObject.GetComponent<Image>();
                if (oldImg != null) oldImg.color = _normalRowColor;
            }

            // 현재 행 선택
            _selectedRecord = rec;
            _selectedRowObject = row;
            rowImage.color = _selectedRowColor;
            Debug.Log($"[RemoteInspection] 행 선택됨: {rec.DisplayId}");
        });

        ConfigureRowLayout(row);
    }

    private Color GetStatusColor(string result)
    {
        switch ((result ?? "").ToLowerInvariant())
        {
            case "pending": return pendingColor;
            case "in_progress": return inProgressColor;
            case "normal":
            case "issue_found":
            default:
                return completedColor;
        }
    }
}
