using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class UIStyleApplier
{
    private static Color GetColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
        return Color.white;
    }

    public static readonly Color ColorWhite = GetColor("#FFFFFF");
    public static readonly Color ColorPopupBg = GetColor("#FFFFFFD9"); // 85% opacity
    public static readonly Color ColorBorder = GetColor("#E2E8F0");
    
    public static readonly Color ColorImportantText = GetColor("#1A202D");
    public static readonly Color ColorBodyText = GetColor("#3D5A6A");
    public static readonly Color ColorSubText = GetColor("#7A9EAB");
    
    public static readonly Color ColorMintActive = GetColor("#50B8B8");
    public static readonly Color ColorGradientStart = GetColor("#50B8B8");
    public static readonly Color ColorGradientEnd = GetColor("#3978B8");
    
    public static readonly Color ColorDefaultBtnBorder = new Color(0.313f, 0.721f, 0.721f, 0.2f); // #50B8B8 20%
    public static readonly Color ColorPrimaryBtnShadow = new Color(0.223f, 0.470f, 0.721f, 0.3f); // #3978B8 30%
    public static readonly Color ColorPopupShadow = new Color(0f, 0f, 0f, 0.2f); // #000000 20%

    public static void ApplyStyle(GameObject popupRoot)
    {
        if (popupRoot == null) return;
        
        // 1. 스크림(배경 오버레이)과 실제 패널 찾기
        Image scrimImg = popupRoot.GetComponent<Image>();
        Image panelImg = null;
        
        if (popupRoot.transform.childCount > 0)
        {
            panelImg = popupRoot.transform.GetChild(0).GetComponent<Image>();
        }

        // 스크림(전체화면 오버레이)은 어둡게 처리
        if (scrimImg != null && scrimImg.rectTransform.anchorMin == Vector2.zero && scrimImg.rectTransform.anchorMax == Vector2.one)
        {
            scrimImg.color = new Color(0, 0, 0, 0.4f); // 40% Black
        }
        else
        {
            // 루트 자체가 패널인 경우
            panelImg = scrimImg;
        }

        // 실제 팝업 패널은 가이드라인의 흰색(85%) 처리 및 그림자 적용
        if (panelImg != null && panelImg != scrimImg)
        {
            panelImg.color = ColorPopupBg;
            ApplyUIShadow(panelImg.gameObject, new Vector2(0, -8f), 32f, ColorPopupShadow);
            
            // 패널 내부 여백(패딩) 및 요소 간격 줄이기
            var vlg = panelImg.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding = new RectOffset(20, 20, 20, 20);
                vlg.spacing = 10f; // 요소 간격을 줄임
            }
        }

        // 2. 입력 필드 및 드롭다운 스타일링
        TMP_InputField[] inputs = popupRoot.GetComponentsInChildren<TMP_InputField>(true);
        foreach (var input in inputs)
        {
            Image img = input.GetComponent<Image>();
            if (img != null) img.color = ColorWhite;
            
            // 입력칸 높이 및 패딩 강제 축소
            var rt = input.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 38f);
            var le = input.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = 38f;
            
            var outline = input.GetComponent<Outline>();
            if (outline == null) outline = input.gameObject.AddComponent<Outline>();
            outline.effectColor = ColorBorder;
            outline.effectDistance = new Vector2(1, -1);
            
            if (input.textComponent != null) input.textComponent.color = ColorImportantText;
            if (input.placeholder != null) input.placeholder.color = ColorSubText;
        }

        TMP_Dropdown[] dropdowns = popupRoot.GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (var dd in dropdowns)
        {
            Image img = dd.GetComponent<Image>();
            if (img != null) img.color = ColorWhite;
            
            // 드롭다운칸 높이 및 패딩 강제 축소
            var rt = dd.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 38f);
            var le = dd.GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = 38f;
            
            var outline = dd.GetComponent<Outline>();
            if (outline == null) outline = dd.gameObject.AddComponent<Outline>();
            outline.effectColor = ColorBorder;
            outline.effectDistance = new Vector2(1, -1);

            if (dd.captionText != null) dd.captionText.color = ColorImportantText;
        }

        // 3. 일반 텍스트 타이포그래피 (버튼 및 인풋 내부 텍스트 제외)
        TMP_Text[] texts = popupRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.GetComponentInParent<Button>() != null || t.GetComponentInParent<TMP_InputField>() != null || t.GetComponentInParent<TMP_Dropdown>() != null)
                continue;

            string name = t.name.ToLower();
            if (name.Contains("title") || name.Contains("제목") || t.fontSize > 12f)
            {
                t.color = ColorImportantText;
            }
            else if (name.Contains("sub") || name.Contains("보조") || name.Contains("라벨") || name.Contains("label"))
            {
                t.color = ColorSubText;
            }
            else
            {
                t.color = ColorBodyText;
            }
        }

        // 4. 버튼
        Button[] buttons = popupRoot.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            Image btnImg = btn.GetComponent<Image>();
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>(true);
            
            if (btnImg != null) btnImg.color = ColorWhite;
            if (btnText != null) btnText.color = ColorBodyText;

            if (btnText != null && (btnText.text.Contains("등록") || btnText.text.Contains("확인") || btnText.text.Contains("저장")))
            {
                if (btnText != null) btnText.color = ColorWhite;
                ApplyUIGradient(btn.gameObject, ColorGradientStart, ColorGradientEnd);
                ApplyUIShadow(btn.gameObject, new Vector2(0, -4f), 16f, ColorPrimaryBtnShadow);
            }
            else
            {
                var outline = btn.gameObject.GetComponent<Outline>();
                if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
                outline.effectColor = ColorDefaultBtnBorder;
                outline.effectDistance = new Vector2(1, -1);
            }
        }
    }

    private static void ApplyUIShadow(GameObject go, Vector2 distance, float blur, Color color)
    {
        Type shadowType = Type.GetType("Coffee.UIExtensions.UIShadow, Coffee.UIExtensions");
        if (shadowType == null) return;

        Component shadow = go.GetComponent(shadowType);
        if (shadow == null) shadow = go.AddComponent(shadowType);

        var styleProp = shadowType.GetProperty("style");
        if (styleProp != null) styleProp.SetValue(shadow, 0);

        var effectDistProp = shadowType.GetProperty("effectDistance");
        if (effectDistProp != null) effectDistProp.SetValue(shadow, distance);

        var colorProp = shadowType.GetProperty("effectColor");
        if (colorProp != null) colorProp.SetValue(shadow, color);

        var blurProp = shadowType.GetProperty("blur");
        if (blurProp != null) blurProp.SetValue(shadow, blur);
    }

    private static void ApplyUIGradient(GameObject go, Color c1, Color c2)
    {
        Type gradType = Type.GetType("Coffee.UIExtensions.UIGradient, Coffee.UIExtensions");
        if (gradType == null) return;

        Component grad = go.GetComponent(gradType);
        if (grad == null) grad = go.AddComponent(gradType);

        var dirProp = gradType.GetProperty("direction");
        if (dirProp != null) dirProp.SetValue(grad, 3);

        var color1Prop = gradType.GetProperty("color1");
        if (color1Prop != null) color1Prop.SetValue(grad, c1);
        
        var color2Prop = gradType.GetProperty("color2");
        if (color2Prop != null) color2Prop.SetValue(grad, c2);
        
        var color3Prop = gradType.GetProperty("color3");
        if (color3Prop != null) color3Prop.SetValue(grad, c1);
        
        var color4Prop = gradType.GetProperty("color4");
        if (color4Prop != null) color4Prop.SetValue(grad, c2);
        
        var rotProp = gradType.GetProperty("rotation");
        if (rotProp != null) rotProp.SetValue(grad, 135f);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/팝업 디자인 즉시 적용 (가이드라인)")]
    public static void ApplyToScenePopups()
    {
        int count = 0;
        
        // 1. 유지관리 등록 팝업
        var mPopups = UnityEngine.Object.FindObjectsOfType<MaintenanceRegisterPopup>(true);
        foreach (var p in mPopups)
        {
            if (p.popupPanel != null)
            {
                ApplyStyle(p.popupPanel);
                EditorUtility.SetDirty(p.gameObject);
                count++;
            }
        }
        
        // 2. 원격점검 등록 팝업
        var rPopups = UnityEngine.Object.FindObjectsOfType<RemoteInspectionRegisterPopup>(true);
        foreach (var p in rPopups)
        {
            if (p.popupPanel != null)
            {
                ApplyStyle(p.popupPanel);
                EditorUtility.SetDirty(p.gameObject);
                count++;
            }
        }

        if (count > 0)
        {
            // 변경 사항을 저장할 수 있도록 씬에 마킹
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[UIStyleApplier] 성공적으로 {count}개의 팝업에 디자인 가이드라인을 강제 적용했습니다! (이제 플레이 모드가 아니어도 밝게 보입니다)");
        }
        else
        {
            Debug.LogWarning("[UIStyleApplier] 씬에서 팝업을 찾지 못했습니다. 팝업 루트가 존재하는 씬을 열어두고 실행해주세요.");
        }
    }
#endif
}
