using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateDeletePopup
{
    [MenuItem("Tools/삭제 확인 팝업 자동 생성")]
    public static void CreatePopup()
    {
        // 최상위 Canvas 찾기
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CreateDeletePopup] 씬에 Canvas가 없습니다.");
            return;
        }

        // 1. 루트 오브젝트 생성
        GameObject rootGo = new GameObject("DeleteConfirmPopup_Root");
        rootGo.transform.SetParent(canvas.transform, false);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;
        
        var popupScript = rootGo.AddComponent<DeleteConfirmPopup>();
        popupScript.panelRoot = rootGo;

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

        // 3. 패널 본체
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(rootGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(400, 220); // 팝업 사이즈
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = Color.white;
        
        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 24, 24);
        vlg.spacing = 24f; // 간격 넓힘
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = panelGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // UIEffect 그림자 적용 (존재한다면)
        UIStyleApplier.ApplyStyle(panelGo);

        // 4. 타이틀 텍스트
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "삭제 확인";
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = new Color32(26, 32, 45, 255);
        titleTxt.alignment = TextAlignmentOptions.TopLeft;
        popupScript.titleText = titleTxt;

        // 5. 메시지 텍스트
        GameObject msgGo = new GameObject("MessageText");
        msgGo.transform.SetParent(panelGo.transform, false);
        var msgTxt = msgGo.AddComponent<TextMeshProUGUI>();
        msgTxt.text = "정말 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.";
        msgTxt.fontSize = 16;
        msgTxt.color = new Color32(61, 90, 106, 255); // 본문 색상
        msgTxt.alignment = TextAlignmentOptions.TopLeft;
        msgTxt.enableWordWrapping = true;
        popupScript.messageText = msgTxt;

        // 7. 버튼 컨테이너
        GameObject btnGroupGo = new GameObject("ButtonGroup");
        btnGroupGo.transform.SetParent(panelGo.transform, false);
        var hlg = btnGroupGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleRight;

        // 8. 취소 버튼
        GameObject cancelBtnGo = CreateButton(btnGroupGo.transform, "취소", new Color32(240, 240, 240, 255), new Color32(61, 90, 106, 255));
        popupScript.cancelButton = cancelBtnGo.GetComponent<Button>();

        // 9. 삭제 버튼 (파괴적 행동 - 빨간색)
        GameObject confirmBtnGo = CreateButton(btnGroupGo.transform, "삭제", new Color32(239, 68, 68, 255), Color.white);
        popupScript.confirmButton = confirmBtnGo.GetComponent<Button>();

        // 생성 직후 비활성화 (Show() 호출 시 활성화 됨)
        rootGo.SetActive(false);
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[CreateDeletePopup] 삭제 팝업이 성공적으로 생성되었습니다!");
    }

    private static GameObject CreateButton(Transform parent, string text, Color bgColor, Color textColor)
    {
        GameObject btnGo = new GameObject(text + "Button");
        btnGo.transform.SetParent(parent, false);
        
        var img = btnGo.AddComponent<Image>();
        img.color = bgColor;
        
        var le = btnGo.AddComponent<LayoutElement>();
        le.minWidth = 100f;
        le.minHeight = 44f;
        
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
