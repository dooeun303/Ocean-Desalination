using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateMaintenancePopup
{
    [MenuItem("Tools/유지관리 팝업 자동 생성")]
    public static void CreatePopup()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CreateMaintenancePopup] 씬에 Canvas가 없습니다.");
            return;
        }

        // 1. 루트 오브젝트 생성
        GameObject rootGo = new GameObject("RegisterPopup_Root");
        rootGo.transform.SetParent(canvas.transform, false);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;
        rootGo.layer = LayerMask.NameToLayer("UI");
        
        var popupScript = rootGo.AddComponent<MaintenanceRegisterPopup>();

        // 2. 딤 처리 배경 (Scrim)
        GameObject scrimGo = new GameObject("Scrim");
        scrimGo.transform.SetParent(rootGo.transform, false);
        var scrimRt = scrimGo.AddComponent<RectTransform>();
        scrimRt.anchorMin = Vector2.zero;
        scrimRt.anchorMax = Vector2.one;
        scrimRt.sizeDelta = Vector2.zero;
        scrimRt.anchoredPosition = Vector2.zero;
        var scrimImg = scrimGo.AddComponent<Image>();
        scrimImg.color = new Color32(26, 32, 45, 128); // #1A202D 50%
        scrimGo.layer = LayerMask.NameToLayer("UI");

        // 3. 패널 본체
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(rootGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(500, 550); // 팝업 사이즈
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = Color.white;
        panelGo.layer = LayerMask.NameToLayer("UI");
        
        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 32, 32);
        vlg.spacing = 16f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // UIEffect 그림자 적용 (존재한다면)
        UIStyleApplier.ApplyStyle(panelGo);

        popupScript.popupPanel = panelGo;

        // 4. 타이틀 텍스트
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "유지관리 작업 등록";
        titleTxt.fontSize = 22;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = new Color32(26, 32, 45, 255);
        titleTxt.alignment = TextAlignmentOptions.TopLeft;

        // 간격
        CreateSpacer(panelGo.transform, 10f);

        // 5. 입력 필드들 생성
        TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources();

        popupScript.equipmentDropdown = CreateDropdownRow(panelGo.transform, "설비 선택", resources);
        popupScript.workTypeDropdown = CreateDropdownRow(panelGo.transform, "작업 타입", resources);
        popupScript.manualDropdown = CreateDropdownRow(panelGo.transform, "매뉴얼 선택", resources);
        popupScript.technicianDropdown = CreateDropdownRow(panelGo.transform, "담당자 배정", resources);
        popupScript.scheduledAtInput = CreateInputRow(panelGo.transform, "예정 일시 (yyyy-MM-dd HH:mm)", resources);

        // 간격
        CreateSpacer(panelGo.transform, 20f);

        // 6. 하단 버튼 영역
        GameObject btnGroupGo = new GameObject("ButtonGroup");
        btnGroupGo.transform.SetParent(panelGo.transform, false);
        var btnGroupLe = btnGroupGo.AddComponent<LayoutElement>();
        btnGroupLe.minHeight = 48f;
        
        var hlg = btnGroupGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // 취소 버튼
        GameObject cancelBtnGo = CreateButton(btnGroupGo.transform, "취소", new Color32(240, 240, 240, 255), new Color32(61, 90, 106, 255));
        popupScript.closeButton = cancelBtnGo.GetComponent<Button>();

        // 등록 버튼
        GameObject confirmBtnGo = CreateButton(btnGroupGo.transform, "등록", new Color32(80, 184, 184, 255), Color.white);
        popupScript.registerButton = confirmBtnGo.GetComponent<Button>();


        // ==========================================
        // 7. 성공 패널 (SuccessPanel)
        // ==========================================
        GameObject successPanelGo = new GameObject("SuccessPanel");
        successPanelGo.transform.SetParent(rootGo.transform, false);
        var sPanelRt = successPanelGo.AddComponent<RectTransform>();
        sPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        sPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        sPanelRt.sizeDelta = new Vector2(400, 200);
        sPanelRt.anchoredPosition = Vector2.zero;
        var sPanelImg = successPanelGo.AddComponent<Image>();
        sPanelImg.color = Color.white;
        UIStyleApplier.ApplyStyle(successPanelGo);

        var sVlg = successPanelGo.AddComponent<VerticalLayoutGroup>();
        sVlg.padding = new RectOffset(32, 32, 32, 32);
        sVlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject sMsgGo = new GameObject("SuccessMessage");
        sMsgGo.transform.SetParent(successPanelGo.transform, false);
        var sMsgTxt = sMsgGo.AddComponent<TextMeshProUGUI>();
        sMsgTxt.text = "작업이 성공적으로 완료되었습니다.";
        sMsgTxt.fontSize = 18;
        sMsgTxt.fontStyle = FontStyles.Bold;
        sMsgTxt.color = new Color32(80, 184, 184, 255);
        sMsgTxt.alignment = TextAlignmentOptions.Center;

        popupScript.successPanel = successPanelGo;


        // 생성 직후 비활성화
        successPanelGo.SetActive(false);
        rootGo.SetActive(false);
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[CreateMaintenancePopup] 유지관리 등록 팝업이 성공적으로 생성되었습니다!");
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        var le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
    }

    private static TMP_Dropdown CreateDropdownRow(Transform parent, string labelText, TMP_DefaultControls.Resources resources)
    {
        GameObject row = new GameObject("Row_" + labelText);
        row.transform.SetParent(parent, false);
        var vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 14;
        labelTmp.color = new Color32(61, 90, 106, 255);

        GameObject dropdownGo = TMP_DefaultControls.CreateDropdown(resources);
        dropdownGo.transform.SetParent(row.transform, false);
        var le = dropdownGo.AddComponent<LayoutElement>();
        le.minHeight = 40f;
        
        // 배경색
        var img = dropdownGo.GetComponent<Image>();
        if (img != null) { img.color = new Color32(245, 247, 250, 255); }

        return dropdownGo.GetComponent<TMP_Dropdown>();
    }

    private static TMP_InputField CreateInputRow(Transform parent, string labelText, TMP_DefaultControls.Resources resources)
    {
        GameObject row = new GameObject("Row_" + labelText);
        row.transform.SetParent(parent, false);
        var vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = 14;
        labelTmp.color = new Color32(61, 90, 106, 255);

        GameObject inputGo = TMP_DefaultControls.CreateInputField(resources);
        inputGo.transform.SetParent(row.transform, false);
        var le = inputGo.AddComponent<LayoutElement>();
        le.minHeight = 40f;

        // 배경색
        var img = inputGo.GetComponent<Image>();
        if (img != null) { img.color = new Color32(245, 247, 250, 255); }

        return inputGo.GetComponent<TMP_InputField>();
    }

    private static GameObject CreateButton(Transform parent, string text, Color bgColor, Color textColor)
    {
        GameObject btnGo = new GameObject(text + "Button");
        btnGo.transform.SetParent(parent, false);
        
        var img = btnGo.AddComponent<Image>();
        img.color = bgColor;
        
        var btn = btnGo.AddComponent<Button>();

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = textColor;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        return btnGo;
    }
}
