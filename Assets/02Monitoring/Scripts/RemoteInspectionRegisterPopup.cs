using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================
// 원격점검 등록 팝업 (MockDataStore 버전)
// =============================================
public class RemoteInspectionRegisterPopup : MonoBehaviour
{
    [Serializable] public class EquipData { public string id, name; }
    [Serializable] public class TechData  { public string id, name, role; }

    [Header("팝업 UI")]
    public GameObject popupPanel;
    public GameObject successPanel;

    [Header("입력 필드")]
    public TMP_Dropdown equipmentDropdown;
    public TMP_Dropdown technicianDropdown;
    public TMP_InputField scheduledAtInput;

    [Header("버튼")]
    public Button registerButton;
    public Button closeButton;

    [Header("테이블 새로고침용 컨트롤러")]
    public RemoteInspectionTableController tableController;

    [Header("기본값 설정")]
    [SerializeField] private string defaultPlatform = "MR";

    private readonly List<EquipData> _equipList = new();
    private readonly List<TechData> _techList = new();

    private bool _isOpened = false;

    void Start()
    {
        closeButton?.onClick.AddListener(OnCloseButton);
        registerButton?.onClick.AddListener(OnRegisterButton);
        if (!_isOpened)
        {
            popupPanel?.SetActive(false);
            successPanel?.SetActive(false);
        }

        // 팝업 패널들에 Fade-Up 애니메이션 자동 부착
        UIFadeUp.ApplyTo(popupPanel);
        UIFadeUp.ApplyTo(successPanel);
    }

    private InspectionRecord _editingRecord = null;

    public async void Open(InspectionRecord editData = null)
    {
        _editingRecord = editData;
        Debug.Log($"[RemoteInspectionRegisterPopup] Open() called! Mode: {(_editingRecord == null ? "Register" : "Edit")}");
        _isOpened = true;

        gameObject.SetActive(true);
        gameObject.transform.SetAsLastSibling(); // 맨 앞으로

        if (popupPanel != null) popupPanel.SetActive(true);
        successPanel?.SetActive(false);
        ResetInputs();
        await FetchInitialDataAsync();
        
        if (registerButton != null)
        {
            var btnTxt = registerButton.GetComponentInChildren<TMP_Text>(true);
            if (btnTxt != null) btnTxt.text = _editingRecord == null ? "등록" : "수정";
        }

        if (_editingRecord != null)
        {
            // Set fields based on existing data
            int techIdx = _techList.FindIndex(t => t.name == _editingRecord.InspectorName);
            if (techIdx >= 0) technicianDropdown.value = techIdx + 1;

            if (scheduledAtInput != null)
                scheduledAtInput.text = _editingRecord.InspectionDate.ToString("yyyy-MM-dd HH:mm");
        }
    }

    void ResetInputs()
    {
        equipmentDropdown?.ClearOptions();
        technicianDropdown?.ClearOptions();

        if (scheduledAtInput)
            scheduledAtInput.text = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm");
    }

    private async System.Threading.Tasks.Task FetchInitialDataAsync()
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
                _equipList.Add(new EquipData { id = e.id, name = e.name });
            }

            equipmentDropdown?.ClearOptions();
            var opts = new List<string> { "설비 선택" };
            foreach (var e in _equipList) opts.Add(e.name);
            equipmentDropdown?.AddOptions(opts);
            await System.Threading.Tasks.Task.Yield();
        }
        catch (Exception e) { Debug.LogError("[RemoteRegister] 설비 조회 실패: " + e.Message); }
    }

    private async System.Threading.Tasks.Task FetchTechniciansAsync()
    {
        try
        {
            _techList.Clear();
            foreach (var t in MonitoringMockDataStore.Technicians)
            {
                _techList.Add(new TechData { id = t.id, name = t.name, role = t.role });
            }

            technicianDropdown?.ClearOptions();
            var opts = new List<string> { "담당자 선택" };
            foreach (var t in _techList) opts.Add($"{t.name} ({t.role})");
            technicianDropdown?.AddOptions(opts);
            await System.Threading.Tasks.Task.Yield();
        }
        catch (Exception e) { Debug.LogError("[RemoteRegister] 담당자 조회 실패: " + e.Message); }
    }

    private async void OnRegisterButton()
    {
        if (equipmentDropdown == null || equipmentDropdown.value == 0)
        { Debug.LogWarning("[RemoteRegister] 설비를 선택해 주세요"); return; }
        if (technicianDropdown == null || technicianDropdown.value == 0)
        { Debug.LogWarning("[RemoteRegister] 담당자를 선택해 주세요"); return; }
        if (string.IsNullOrEmpty(scheduledAtInput?.text))
        { Debug.LogWarning("[RemoteRegister] 예정 시간을 입력해 주세요"); return; }

        await PostRemoteInspectionAsync();
    }

    private async System.Threading.Tasks.Task PostRemoteInspectionAsync()
    {
        var equip = _equipList[equipmentDropdown.value - 1];
        var tech = _techList[technicianDropdown.value - 1];

        try
        {
            // Maintain existing result if editing
            string resultToSet = _editingRecord != null ? _editingRecord.Result : "pending";
            if (resultToSet == "대기") resultToSet = "pending";
            else if (resultToSet == "진행중") resultToSet = "in_progress";
            else if (resultToSet == "정상") resultToSet = "normal";
            else if (resultToSet == "이상") resultToSet = "issue_found";
            
            DateTime schedTime;
            if (!DateTime.TryParse(scheduledAtInput.text, out schedTime)) schedTime = DateTime.Now.AddHours(1);

            bool success = true;
            if (success)
            {
                string msg = _editingRecord == null ? "원격 점검 등록이 완료되었습니다!" : "원격 점검 수정이 완료되었습니다!";
                ShowMessage(msg, success: true);
                tableController?.OnSearchClicked();
                StartCoroutine(AutoClose(success: true));
            }
            else
            {
                throw new Exception("기록을 찾을 수 없거나 작업에 실패했습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[RemoteRegister] 등록 실패: " + e.Message);
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
        yield return new WaitForSeconds(2f);
        successPanel?.SetActive(false);
        if (success) popupPanel?.SetActive(false);
    }

    void OnCloseButton() => popupPanel?.SetActive(false);
}