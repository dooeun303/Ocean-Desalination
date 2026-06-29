using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정비관리(6-2) 페이지: 검색어 입력/버튼으로 MockStore를 조회한다.
/// 결과를 테이블(body)에 템플릿을 복제하며 출력한다.
/// </summary>
public class MaintenanceTableController : MonoBehaviour, IPageRefreshable
{
    [Header("UI 참조")]
    [Tooltip("검색어 입력 필드")]
    [SerializeField] private TMP_InputField searchInput;
    [Tooltip("검색 버튼")]
    [SerializeField] private Button searchButton;
    [Tooltip("행을 담는 컨테이너 (table/body)")]
    [SerializeField] private Transform bodyContainer;
    [Tooltip("복제할 템플릿")]
    [SerializeField] private GameObject rowTemplate;

    [Header("등록 기능 참조")]
    [Tooltip("정비 등록 팝업을 여는 버튼")]
    [SerializeField] private Button registerOpenButton;
    [Tooltip("정비 등록 팝업 컴포넌트")]
    [SerializeField] private MaintenanceRegisterPopup registerPopup;

    [Header("결과 상태")]
    [Tooltip("결과가 없을 때 표시할 텍스트")]
    [SerializeField] private TMP_Text emptyStateText;

    [Header("텍스트 색상")]
    [SerializeField] private Color defaultTextColor = new Color32(126, 157, 171, 255); // #7E9DAB
    [SerializeField] private Color pendingColor = new Color32(245, 158, 11, 255);       // #F59E0B (대기)
    [SerializeField] private Color inProgressColor = new Color32(80, 184, 184, 255);     // #50B8B8 (진행중)
    [SerializeField] private Color completedColor = new Color32(80, 184, 184, 255);      // #50B8B8 (완료)
    [SerializeField] private Color cancelledColor = new Color32(239, 68, 68, 255);       // #EF4444 (취소)

    private readonly List<GameObject> _spawnedRows = new List<GameObject>();
    private bool _isQuerying;

    // 선택 기능 추가
    private MaintenanceRecord _selectedRecord = null;
    private GameObject _selectedRowObject = null;
    private Color _normalRowColor = new Color32(255, 255, 255, 0); // 투명
    private Color _selectedRowColor = new Color32(230, 240, 250, 255); // 연한 파란색

    public void OnPageRefresh()
    {
        OnSearchClicked();
    }

    private void Start()
    {
        // 템플릿을 제외한 기존 자식 오브젝트 정리 및 템플릿 비활성화
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

        // 1. 인스펙터에 명시적으로 할당된 버튼이 있다면 무조건 우선적으로 연결합니다. (가장 안전함)
        if (registerOpenButton != null)
        {
            Debug.Log("[MaintenanceTable] 명시적 할당된 '등록' 버튼 연결");
            registerOpenButton.onClick.RemoveAllListeners();
            registerOpenButton.onClick.AddListener(() => 
            {
                if (registerPopup != null && registerPopup.gameObject.scene.IsValid()) registerPopup.Open();
                else Object.FindObjectOfType<MaintenanceRegisterPopup>(true)?.Open();
            });
        }
        else
        {
            Debug.LogWarning("[MaintenanceTable] registerOpenButton이 인스펙터에 할당되지 않았습니다. 텍스트 검색으로 버튼을 찾습니다.");
        }

        // 2. 만약 인스펙터에 할당 안 된 버튼들이 있다면 씬 전체를 뒤져서 이름(텍스트)으로 찾습니다.
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

            // Maintenance 패널 안의 버튼인지 (또는 부모가 툴바인지) 대략적으로 필터링
            // 만약 Remote 버튼이라면 건너뜀
            Transform p = btn.transform.parent;
            bool isRemote = false;
            while (p != null)
            {
                if (p.name.Contains("원격") || p.name.Contains("Remote")) { isRemote = true; break; }
                p = p.parent;
            }
            if (isRemote) continue;

            if (btnText.Contains("수정"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[Maintenance] 상단 수정 버튼 클릭됨.");
                    if (registerPopup != null) registerPopup.Open(null);
                    else 
                    {
                        var popup = Object.FindObjectOfType<MaintenanceRegisterPopup>(true);
                        if (popup != null) popup.Open(null);
                    }
                });
            }
            else if (btnText.Contains("등록"))
            {
                Debug.Log($"[MaintenanceTable] '등록' 버튼 감지 및 연결: {btn.name}");
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => 
                {
                    Debug.Log("[MaintenanceTable] '등록' 버튼 클릭됨!");
                    
                    if (registerPopup != null && registerPopup.gameObject.scene.IsValid()) 
                    {
                        Debug.Log("[MaintenanceTable] 인스펙터에 연결된 팝업 열기 시도");
                        registerPopup.Open();
                    }
                    else 
                    {
                        var popup = Object.FindObjectOfType<MaintenanceRegisterPopup>(true);
                        if (popup != null) 
                        {
                            Debug.Log("[MaintenanceTable] 씬에서 찾은 팝업 열기 시도");
                            popup.Open();
                        }
                        else
                        {
                            Debug.LogError("[MaintenanceTable] 씬에 MaintenanceRegisterPopup 컴포넌트가 존재하지 않습니다!");
                        }
                    }
                });
            }
            else if (btnText.Contains("삭제"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[Maintenance] 상단 삭제 버튼 클릭됨.");
                    
                    var confirmPopup = Object.FindObjectOfType<DeleteConfirmPopup>(true);
                    if (confirmPopup != null)
                    {
                        confirmPopup.Show("유지관리 삭제", "선택하신 기록을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", () =>
                        {
                            LoadData("");
                            NotifyPageRefreshed();
                        });
                    }
                    else
                    {
                        LoadData("");
                        NotifyPageRefreshed();
                    }
                });
            }
        }

        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);

        // 최초 진입 시 전체 로드
        LoadData("");
    }

    private void NotifyPageRefreshed()
    {
        foreach (var r in GetComponentsInChildren<IPageRefreshable>(true))
        {
            if (r != (IPageRefreshable)this)
            {
                r.OnPageRefresh();
            }
        }
    }

    public void OnSearchClicked()
    {
        LoadData(searchInput != null ? searchInput.text : "");
    }

    private async void LoadData(string keyword)
    {
        if (_isQuerying) return;
        _isQuerying = true;
        if (searchButton != null) searchButton.interactable = false;

        try
        {
            List<MaintenanceRecord> records = await MonitoringMockDataStore.SearchMaintenances("");
            BuildRows(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Maintenance] Mock 조회 실패: {e.Message}\n{e}");
            BuildRows(new List<MaintenanceRecord>());
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(true);
                emptyStateText.text = "데이터를 불러오지 못했습니다.";
            }
        }
        finally
        {
            _isQuerying = false;
            if (searchButton != null) searchButton.interactable = true;
        }
    }

    private void BuildRows(List<MaintenanceRecord> records)
    {
        foreach (var go in _spawnedRows)
            if (go != null) Destroy(go);
        _spawnedRows.Clear();
        _selectedRecord = null;
        _selectedRowObject = null;

        if (bodyContainer == null || rowTemplate == null)
        {
            Debug.LogError("[Maintenance] bodyContainer 또는 rowTemplate이 연결되지 않았습니다.");
            return;
        }

        bool empty = records == null || records.Count == 0;
        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(empty);
            if (empty) emptyStateText.text = "데이터가 없습니다.";
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
        string[] headers = { "작업 ID", "예정시각", "완료시각", "설비명", "이상 원인", "해당 매뉴얼", "담당자", "상태" };

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
        // 작업 ID, 예정시각, 완료시각, 설비명, 이상 원인, 해당 매뉴얼, 담당자, 상태 (8열)
        float[] flexWidths = { 1.0f, 1.3f, 1.3f, 1.2f, 2.5f, 1.5f, 1.0f, 1.0f };

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

    private void FillRow(GameObject row, MaintenanceRecord rec)
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
                if (cellCount < 8)
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

        if (cellTexts.Count < 8)
        {
            Debug.LogWarning($"[Maintenance] 셀 개수가 부족합니다 ({cellTexts.Count}/8).");
            if (cellTexts.Count < 8)
            {
                cellTexts.Clear();
                cellTexts.AddRange(row.GetComponentsInChildren<TMP_Text>(true));
            }
        }

        if (cellTexts.Count < 8)
        {
            Debug.LogWarning($"[Maintenance] 셀 개수가 최종적으로 부족합니다 ({cellTexts.Count}/8).");
            return;
        }

        // 1. 작업 ID
        cellTexts[0].text = rec.DisplayId;
        cellTexts[0].color = defaultTextColor;

        // 2. 예정시각
        cellTexts[1].text = rec.ScheduledAt.ToString("yyyy.MM.dd HH:mm");
        cellTexts[1].color = defaultTextColor;

        // 3. 완료시각
        cellTexts[2].text = rec.CompletedAt.HasValue ? rec.CompletedAt.Value.ToString("yyyy.MM.dd HH:mm") : "-";
        cellTexts[2].color = defaultTextColor;

        // 4. 설비명
        cellTexts[3].text = rec.EquipmentName;
        cellTexts[3].color = defaultTextColor;

        // 5. 이상 원인
        cellTexts[4].text = rec.Description;
        cellTexts[4].color = defaultTextColor;

        // 6. 해당 매뉴얼
        cellTexts[5].text = rec.ManualTitle;
        cellTexts[5].color = defaultTextColor;

        // 7. 담당자
        cellTexts[6].text = rec.TechnicianName;
        cellTexts[6].color = defaultTextColor;

        // 8. 상태
        cellTexts[7].text = rec.StatusLabel;
        cellTexts[7].color = GetStatusColor(rec.Status);

        // 자식 UI 요소들(텍스트, 배경 등)이 클릭(Raycast)을 가로채지 못하도록 모두 끕니다.
        foreach (var graphic in row.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        // 행 자체도 클릭을 받지 않도록 raycastTarget을 끕니다. (행 선택 비활성화)
        var rowImage = row.GetComponent<Image>();
        if (rowImage == null) rowImage = row.AddComponent<Image>();
        rowImage.color = _normalRowColor;
        rowImage.raycastTarget = false; 

        var btn = row.GetComponent<Button>();
        if (btn != null) Destroy(btn);

        ConfigureRowLayout(row);
    }

    private Color GetStatusColor(string status)
    {
        switch ((status ?? "").ToLowerInvariant())
        {
            case "scheduled": return pendingColor;
            case "in_progress": return inProgressColor;
            case "completed": return completedColor;
            case "cancelled": return cancelledColor;
            default: return pendingColor;
        }
    }
}
