using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class Monitoring3UIStyleApplier : EditorWindow
{
    [MenuItem("Tools/Apply Monitoring3 UI Styles (Light Theme)")]
    public static void ApplyStyles()
    {
        GameObject root = GameObject.Find("Monitoring3_New_Pages");
        if (root == null)
        {
            Debug.LogError("Monitoring3_New_Pages 오브젝트를 찾을 수 없습니다. 먼저 UI Skeleton을 생성해주세요.");
            return;
        }

        // 폰트 에셋 로드 (NotoSansKR-Bold SDF 또는 Medium)
        TMP_FontAsset fontAsset = null;
        string[] fontGuids = AssetDatabase.FindAssets("NotoSansKR-Bold SDF t:TMP_FontAsset");
        if (fontGuids.Length == 0) fontGuids = AssetDatabase.FindAssets("NotoSansKR-Medium SDF t:TMP_FontAsset");
        if (fontGuids.Length > 0)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }

        // 유니티 기본 라운드 스프라이트 로드
        Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 컬러 정의 (가이드라인 참고)
        Color colorWhite = Color.white;
        Color colorImportantText; ColorUtility.TryParseHtmlString("#1A202D", out colorImportantText);
        Color colorBorder; ColorUtility.TryParseHtmlString("#E2E8F0", out colorBorder);

        Undo.RegisterFullObjectHierarchyUndo(root, "Apply UI Styles");

        // 1. 페이지별 여백 및 배경 패딩 설정
        VerticalLayoutGroup[] pages = root.GetComponentsInChildren<VerticalLayoutGroup>(true);
        foreach (var page in pages)
        {
            if (page.gameObject.name.StartsWith("Page_"))
            {
                page.padding = new RectOffset(30, 30, 30, 30);
                page.spacing = 20;
            }
        }

        // 2. 카드 스타일링 (스프라이트, 패딩, 아웃라인)
        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject.name.StartsWith("Card_"))
            {
                // 스프라이트 적용
                if (roundSprite != null)
                {
                    img.sprite = roundSprite;
                    img.type = Image.Type.Sliced;
                }
                img.color = colorWhite;

                // 테두리(아웃라인) 그림자 효과 적용
                Outline outline = img.gameObject.GetComponent<Outline>();
                if (outline == null) outline = img.gameObject.AddComponent<Outline>();
                outline.effectColor = colorBorder;
                outline.effectDistance = new Vector2(0, -2f);

                // 카드 내부 패딩 추가 (텍스트가 테두리에 붙지 않도록)
                VerticalLayoutGroup cardLayout = img.gameObject.GetComponent<VerticalLayoutGroup>();
                if (cardLayout == null) cardLayout = img.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(20, 20, 20, 20);
                cardLayout.childAlignment = TextAnchor.UpperLeft;
                cardLayout.childControlHeight = true;
                cardLayout.childControlWidth = true;
                cardLayout.childForceExpandHeight = false;
                cardLayout.childForceExpandWidth = true;
            }
        }

        // 3. 텍스트 폰트, 크기, 색상 적용
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            if (fontAsset != null) txt.font = fontAsset;
            
            txt.color = colorImportantText;
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.fontSize = 18;
            txt.enableAutoSizing = false; // 오토사이징 해제하고 고정 폰트 크기 사용
            
            // 버튼인지 확인 (MR 원격 접속 버튼 등)
            if (txt.text.Contains("버튼") || txt.text.Contains("▶"))
            {
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white; // 버튼 텍스트는 흰색
                
                // 부모 카드 색상을 버튼 색상으로 변경
                Image parentImg = txt.GetComponentInParent<Image>();
                if (parentImg != null)
                {
                    Color btnColor; ColorUtility.TryParseHtmlString("#50B8B8", out btnColor);
                    parentImg.color = btnColor;
                }
            }
        }

        EditorUtility.SetDirty(root);
        Debug.Log("라이트 테마 스타일이 4가지 탭 모두에 성공적으로 적용되었습니다!");
    }
}
