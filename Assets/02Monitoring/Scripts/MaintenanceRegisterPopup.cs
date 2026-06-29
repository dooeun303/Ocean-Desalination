using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// 정비 등록 팝업 (MockDataStore 버전)
// =============================================
public class MaintenanceRegisterPopup : MonoBehaviour
{
    // ─── 내부 데이터 모델 (전역 충돌 방지용 nested class) ──────────────────
    [Serializable] public class EquipData     { public string id, name, model, location; }
    [Serializable] public class ManualData    { public string id, title, category; }
    [Serializable] public class TechData      { public string id, name, role, department; }

    [Header("팝업 UI")]
    public GameObject popupPanel;
    public GameObject successPanel;

    [Header("입력 필드")]
    public TMP_Dropdown equipmentDropdown;
    public TMP_Dropdown workTypeDropdown;
    public TMP_Dropdown manualDropdown;
    public TMP_Dropdown technicianDropdown;
    public TMP_InputField scheduledAtInput;
    public TMP_Dropdown statusDropdown;
    public TMP_InputField completedAtInput;

    [Header("버튼")]
    public Button registerButton;
    public Button closeButton;

    [Header("테이블 새로고침용 컨트롤러")]
    public MaintenanceTableController tableController;

    // ─── 내부 상태 ──────────────────────────────────────────────────────────
    readonly List<EquipData>  _equipList  = new();
    readonly List<ManualData> _manualList = new();
    readonly List<TechData>   _techList   = new();

    private bool _isOpened = false;

    // ─── Unity 이벤트 ───────────────────────────────────────────────────────
    void Start()
    {
        closeButton?.onClick.AddListener(OnCloseButton);
        registerButton?.onClick.AddListener(OnRegisterButton);
        equipmentDropdown?.onValueChanged.AddListener(_ => RefreshManuals());
        workTypeDropdown?.onValueChanged.AddListener(_ => RefreshManuals());
        
        if (!_isOpened)
        {
            popupPanel?.SetActive(false);
            successPanel?.SetActive(false);
        }

        // 팝업 패널들에 Fade-Up 애니메이션 자동 부착
        UIFadeUp.ApplyTo(popupPanel);
        UIFadeUp.ApplyTo(successPanel);
    }

    private MaintenanceRecord _editingRecord = null;

    // ─── Public API ─────────────────────────────────────────────────────────
    public async void Open(MaintenanceRecord editData = null)
    {
        _editingRecord = editData;
        Debug.Log($"[MaintenanceRegisterPopup] Open() called! Mode: {(_editingRecord == null ? "Register" : "Edit")}");
        _isOpened = true;
        
        gameObject.SetActive(true);
        gameObject.transform.SetAsLastSibling(); // 다른 UI를 가리도록 맨 앞으로 가져옵니다.

        if (popupPanel != null) popupPanel.SetActive(true);
        if (successPanel != null) successPanel.SetActive(false);
        ResetInputs();
        await FetchAllAsync();
        
        // 팝업 타이틀과 버튼 텍스트 변경 시도
        var titleTransform = transform.Find("PopupCard/TitleText");
        if (titleTransform != null)
        {
            var titleTxt = titleTransform.GetComponent<TMP_Text>();
            if (titleTxt != null) titleTxt.text = _editingRecord == null ? "정비 등록" : "정비 수정";
        }

        if (registerButton != null)
        {
            var btnTxt = registerButton.GetComponentInChildren<TMP_Text>(true);
            if (btnTxt != null) btnTxt.text = _editingRecord == null ? "등록" : "수정";
        }
        
        if (_editingRecord != null)
        {
            // 수정 모드: 상태와 완료 시각 활성화
            if (statusDropdown != null) statusDropdown.gameObject.SetActive(true);
            if (completedAtInput != null) completedAtInput.gameObject.SetActive(true);

            // 기존 데이터 채워넣기 (이름으로 매칭)
            int equipIdx = _equipList.FindIndex(e => e.name == _editingRecord.EquipmentName);
            if (equipIdx >= 0) equipmentDropdown.value = equipIdx + 1;

            int techIdx = _techList.FindIndex(t => t.name == _editingRecord.TechnicianName);
            if (techIdx >= 0) technicianDropdown.value = techIdx + 1;

            if (scheduledAtInput != null)
                scheduledAtInput.text = _editingRecord.ScheduledAt.ToString("yyyy-MM-dd HH:mm");

            // 상태 세팅
            if (statusDropdown != null)
            {
                string statusLower = (_editingRecord.Status ?? "").ToLowerInvariant();
                if (statusLower == "scheduled") statusDropdown.value = 1;
                else if (statusLower == "in_progress") statusDropdown.value = 2;
                else if (statusLower == "completed") statusDropdown.value = 3;
                else if (statusLower == "cancelled") statusDropdown.value = 4;
                else statusDropdown.value = 0;
            }

            // 완료시각 세팅
            if (completedAtInput != null)
            {
                completedAtInput.text = _editingRecord.CompletedAt.HasValue 
                    ? _editingRecord.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm") 
                    : "";
            }
        }
        else
        {
            // 등록 모드: 상태와 완료 시각 비활성화
            if (statusDropdown != null) statusDropdown.gameObject.SetActive(false);
            if (completedAtInput != null) completedAtInput.gameObject.SetActive(false);
        }
    }

    // ─── 입력 초기화 ────────────────────────────────────────────────────────
    void ResetInputs()
    {
        equipmentDropdown?.ClearOptions();
        manualDropdown?.ClearOptions();
        technicianDropdown?.ClearOptions();

        workTypeDropdown?.ClearOptions();
        workTypeDropdown?.AddOptions(new List<string>
            { "작업 타입 선택", "REGULAR", "EMERGENCY", "PARTS_REPLACEMENT" });

        statusDropdown?.ClearOptions();
        statusDropdown?.AddOptions(new List<string>
            { "작업 상태 선택", "대기 (scheduled)", "진행중 (in_progress)", "완료 (completed)", "취소 (cancelled)" });

        if (scheduledAtInput)
            scheduledAtInput.text = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm");

        if (completedAtInput)
            completedAtInput.text = "";

        manualDropdown?.AddOptions(new List<string> { "먼저 작업 타입을 선택하세요" });
        if (manualDropdown) manualDropdown.interactable = false;
    }

    // ─── API 조회 ────────────────────────────────────────────────────────────
    private async System.Threading.Tasks.Task FetchAllAsync()
    {
        await FetchEquipmentsAsync();
        await FetchTechniciansAsync();
    }

    private async System.Threading.Tasks.Task FetchEquipmentsAsync()
    {
        try
        {
            _equipList.Clear();
            foreach (var e in MonitoringMockDataStore.Equipments)
            {
                _equipList.Add(new EquipData
                {
                    id = e.id,
                    name = e.name,
                    model = e.model,
                    location = e.location
                });
            }

            equipmentDropdown?.ClearOptions();
            var opts = new List<string> { "설비 선택" };
            foreach (var e in _equipList) opts.Add(e.name);
            equipmentDropdown?.AddOptions(opts);
            await System.Threading.Tasks.Task.Yield();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Register] 설비 조회 실패: " + e.Message);
            equipmentDropdown?.ClearOptions();
            equipmentDropdown?.AddOptions(new List<string> { "설비 목록 로드 실패" });
        }
    }

    private async System.Threading.Tasks.Task FetchTechniciansAsync()
    {
        try
        {
            _techList.Clear();
            foreach (var t in MonitoringMockDataStore.Technicians)
            {
                _techList.Add(new TechData
                {
                    id = t.id,
                    name = t.name,
                    role = t.role,
                    department = t.department
                });
            }

            technicianDropdown?.ClearOptions();
            var opts = new List<string> { "작업자 선택" };
            foreach (var t in _techList) opts.Add($"{t.name} ({t.role})");
            technicianDropdown?.AddOptions(opts);
            await System.Threading.Tasks.Task.Yield();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Register] 작업자 조회 실패: " + e.Message);
            technicianDropdown?.ClearOptions();
            technicianDropdown?.AddOptions(new List<string> { "작업자 목록 로드 실패" });
        }
    }

    private async void RefreshManuals()
    {
        int equipIdx = equipmentDropdown ? equipmentDropdown.value : 0;
        int typeIdx  = workTypeDropdown  ? workTypeDropdown.value  : 0;

        if (equipIdx == 0 || typeIdx == 0)
        {
            manualDropdown?.ClearOptions();
            manualDropdown?.AddOptions(new List<string> { "먼저 작업 타입을 선택하세요" });
            if (manualDropdown) manualDropdown.interactable = false;
            return;
        }

        if (_equipList == null || equipIdx - 1 < 0 || equipIdx - 1 >= _equipList.Count)
        {
            Debug.LogWarning("[Register] Selected equipment index is out of bounds or list is empty.");
            manualDropdown?.ClearOptions();
            manualDropdown?.AddOptions(new List<string> { "유효하지 않은 설비 선택" });
            if (manualDropdown) manualDropdown.interactable = false;
            return;
        }

        string[] types = { "", "REGULAR", "EMERGENCY", "PARTS_REPLACEMENT" };
        if (typeIdx < 0 || typeIdx >= types.Length)
        {
            Debug.LogWarning("[Register] Selected work type index is out of bounds.");
            return;
        }

        await FetchManualsAsync(_equipList[equipIdx - 1].id, types[typeIdx]);
    }

    private async System.Threading.Tasks.Task FetchManualsAsync(string equipId, string category)
    {
        try
        {
            _manualList.Clear();
            foreach (var m in MonitoringMockDataStore.Manuals)
            {
                if (m.equipmentId == equipId && (m.category ?? "").ToLowerInvariant() == category.ToLowerInvariant())
                {
                    _manualList.Add(new ManualData
                    {
                        id = m.id,
                        title = m.title,
                        category = m.category
                    });
                }
            }

            manualDropdown?.ClearOptions();

            if (_manualList.Count == 0)
            {
                manualDropdown?.AddOptions(new List<string> { "해당 매뉴얼 없음" });
                if (manualDropdown) manualDropdown.interactable = false;
                return;
            }

            var opts = new List<string> { "매뉴얼 선택 (필수)" };
            foreach (var m in _manualList) opts.Add(m.title);
            manualDropdown?.AddOptions(opts);
            if (manualDropdown) manualDropdown.interactable = true;
            await System.Threading.Tasks.Task.Yield();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Register] 매뉴얼 조회 실패: " + e.Message);
            manualDropdown?.ClearOptions();
            manualDropdown?.AddOptions(new List<string> { "매뉴얼 로드 실패" });
            if (manualDropdown) manualDropdown.interactable = false;
        }
    }

    // ─── 등록 버튼 ──────────────────────────────────────────────────────────
    private async void OnRegisterButton()
    {
        if (equipmentDropdown == null || equipmentDropdown.value == 0)
        { Debug.LogWarning("[Register] 설비를 선택해 주세요"); return; }
        if (workTypeDropdown == null || workTypeDropdown.value == 0)
        { Debug.LogWarning("[Register] 작업 타입을 선택해 주세요"); return; }
        if (string.IsNullOrEmpty(scheduledAtInput?.text))
        { Debug.LogWarning("[Register] 예정 시간을 입력해 주세요"); return; }

        await PostMaintenanceAsync();
    }

    private async System.Threading.Tasks.Task PostMaintenanceAsync()
    {
        if (_equipList == null || equipmentDropdown == null || 
            equipmentDropdown.value - 1 < 0 || equipmentDropdown.value - 1 >= _equipList.Count)
        {
            Debug.LogWarning("[Register] 올바른 설비를 선택해 주세요.");
            ShowMessage("올바른 설비를 선택해 주세요.", success: false);
            return;
        }

        var equip = _equipList[equipmentDropdown.value - 1];
        string[] types = { "", "REGULAR", "EMERGENCY", "PARTS_REPLACEMENT" };

        if (workTypeDropdown == null || workTypeDropdown.value < 0 || workTypeDropdown.value >= types.Length)
        {
            Debug.LogWarning("[Register] 올바른 작업 타입을 선택해 주세요.");
            ShowMessage("올바른 작업 타입을 선택해 주세요.", success: false);
            return;
        }

        string manualId = null;
        if (manualDropdown != null && manualDropdown.value > 0 && _manualList != null && manualDropdown.value - 1 < _manualList.Count)
        {
            manualId = _manualList[manualDropdown.value - 1].id;
        }

        string technicianId = null;
        if (technicianDropdown != null && technicianDropdown.value > 0 && _techList != null && technicianDropdown.value - 1 < _techList.Count)
        {
            technicianId = _techList[technicianDropdown.value - 1].id;
        }

        try
        {
            // 상태 세팅
            string statusToSet = "scheduled";
            if (_editingRecord != null && statusDropdown != null)
            {
                int val = statusDropdown.value;
                if (val == 1) statusToSet = "scheduled";
                else if (val == 2) statusToSet = "in_progress";
                else if (val == 3) statusToSet = "completed";
                else if (val == 4) statusToSet = "cancelled";
                else statusToSet = _editingRecord.Status;
            }

            // parse scheduled_at
            string schedStr = scheduledAtInput?.text ?? "";
            DateTime schedTime;
            if (!DateTime.TryParse(schedStr, out schedTime))
            {
                schedTime = DateTime.Now.AddDays(1);
            }

            // parse completed_at
            DateTime? completedAtValue = null;
            if (_editingRecord != null && completedAtInput != null && !string.IsNullOrEmpty(completedAtInput.text))
            {
                DateTime compTime;
                if (DateTime.TryParse(completedAtInput.text, out compTime))
                {
                    completedAtValue = compTime;
                }
            }

            bool success = true;
            if (success)
            {
                string resultMsg = _editingRecord == null ? "정비 등록이 완료되었습니다!" : "정비 수정이 완료되었습니다!";
                Debug.Log($"[Register] {resultMsg}");
                ShowMessage(resultMsg, success: true);
                tableController?.OnSearchClicked();
                StartCoroutine(AutoClose(success: true));
            }
            else
            {
                throw new Exception("기록을 찾을 수 없거나 등록 실패했습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Register] 등록 실패: " + e.Message);
            ShowMessage("등록 실패: " + e.Message, success: false);
            StartCoroutine(AutoClose(success: false));
        }
    }

    void ShowMessage(string msg, bool success)
    {
        if (successPanel == null) return;
        var txt = successPanel.GetComponentInChildren<TMP_Text>();
        if (txt) txt.text = msg;
        successPanel.SetActive(true);
    }

    IEnumerator AutoClose(bool success)
    {
        yield return new WaitForSeconds(3f);
        successPanel?.SetActive(false);
        if (success) popupPanel?.SetActive(false);
    }

    void OnCloseButton() => popupPanel?.SetActive(false);
}