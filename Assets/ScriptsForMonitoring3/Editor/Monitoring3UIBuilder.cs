using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class Monitoring3UIBuilder : EditorWindow
{
    [MenuItem("Tools/Setup Sidebar Navigation (Safe)")]
    public static void SetupNavigation()
    {
        GameObject root = null;
        Transform[] allT = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allT)
        {
            if (t.name == "Page_EquipmentHealth" && t.gameObject.scene.isLoaded)
            {
                root = t.parent.gameObject;
                break;
            }
        }

        if (root == null)
        {
            Debug.LogError("페이지(Page_EquipmentHealth)를 찾을 수 없습니다! 씬 구조를 확인해주세요.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Setup Navigation");

        // Attach MenuButtonGroup to root
        MenuButtonGroup group = root.GetComponent<MenuButtonGroup>();
        if (group == null) group = root.AddComponent<MenuButtonGroup>();

        string[] pageNames = { "Page_EquipmentHealth", "Page_AlarmHistory", "Page_Maintenance", "Page_RemoteInspection" };
        string[] btnNames = { "Btn_설비 건전성 현황", "Btn_고장예지(알람 이력)", "Btn_유지보수 작업 관리", "Btn_MR 원격점검 관리" };

        group.sidebarContents = new GameObject[0]; // Prevent null ref exception in MenuButtonGroup!

        // Fuzzy find pages to handle lowercase/uppercase mismatches (e.g. page_maintenance)
        string[] pageKeywords = { "EquipmentHealth", "AlarmHistory", "Maintenance", "RemoteInspection" };
        group.pages = new GameObject[4];
        for (int i = 0; i < pageKeywords.Length; i++)
        {
            Transform foundPage = null;
            foreach (Transform child in root.transform)
            {
                if (child.name.IndexOf(pageKeywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundPage = child;
                    break;
                }
            }
            if (foundPage != null)
            {
                group.pages[i] = foundPage.gameObject;
            }
            else
            {
                Debug.LogError($"[치명적] {pageKeywords[i]} 가 포함된 이름의 페이지를 찾을 수 없습니다! 이름을 확인해주세요.");
                // Create a dummy object so MenuButtonGroup doesn't throw NullReferenceException
                GameObject dummy = new GameObject("DummyPage_" + pageKeywords[i]);
                dummy.transform.SetParent(root.transform);
                dummy.SetActive(false);
                group.pages[i] = dummy;
            }
        }

        Transform sidebar = root.transform.Find("Sidebar");
        if (sidebar == null)
        {
            foreach (Transform child in root.transform)
            {
                if (child.name.IndexOf("Sidebar", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sidebar = child;
                    break;
                }
            }
        }
        if (sidebar != null)
        {
            Transform subMenuArea = sidebar.Find("SubMenuArea");
            if (subMenuArea != null)
            {
                group.buttons = new MenuButton[4];

                Sprite normBg = FindSpriteByName("서브메뉴_기본");
                if (normBg == null) normBg = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                Sprite selBg = FindSpriteByName("서브메뉴_선택됨");
                if (selBg == null) selBg = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

                ColorUtility.TryParseHtmlString("#A0AAB4", out Color normIconCol);
                ColorUtility.TryParseHtmlString("#FFFFFF", out Color selIconCol);

                for (int i = 0; i < btnNames.Length; i++)
                {
                    Transform btnT = subMenuArea.Find(btnNames[i]);
                    if (btnT != null)
                    {
                        Button oldBtn = btnT.GetComponent<Button>();
                        if (oldBtn != null) Undo.DestroyObjectImmediate(oldBtn);

                        MenuButton mb = btnT.GetComponent<MenuButton>();
                        if (mb == null) mb = Undo.AddComponent<MenuButton>(btnT.gameObject);

                        mb.backgroundImage = btnT.GetComponent<Image>();
                        Transform iconT = btnT.Find("Icon");
                        if (iconT != null) mb.iconImage = iconT.GetComponent<Image>();

                        mb.normalBg = normBg;
                        mb.selectedBg = selBg;
                        mb.normalIconColor = normIconCol;
                        mb.selectedIconColor = selIconCol;
                        mb.group = group;

                        group.buttons[i] = mb;
                    }
                    else
                    {
                        Debug.LogWarning($"사이드바 버튼 {btnNames[i]} 를 찾을 수 없습니다.");
                    }
                }
            }
        }

        Debug.Log("사이드바 네비게이션(MenuButtonGroup) 설정이 완료되었습니다! 플레이 모드에서 확인해보세요.");
    }

    [MenuItem("Tools/Populate Anomaly Timeline (Safe)")]
    public static void PopulateAnomalyTimeline()
    {
        GameObject page = GameObject.Find("Page_EquipmentHealth");
        if (page == null)
        {
            Debug.LogError("Page_EquipmentHealth not found in the scene!");
            return;
        }

        Transform targetCard = null;
        Transform[] allTransforms = page.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "Card_이상 감지 타임라인")
            {
                targetCard = t;
                break;
            }
        }

        if (targetCard == null)
        {
            Debug.LogError("Card_이상 감지 타임라인 not found in Page_EquipmentHealth!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(targetCard.gameObject, "Populate Timeline List");

        for (int i = targetCard.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(targetCard.GetChild(i).gameObject);
        }

        VerticalLayoutGroup cardVlg = targetCard.GetComponent<VerticalLayoutGroup>();
        if (cardVlg == null) cardVlg = targetCard.gameObject.AddComponent<VerticalLayoutGroup>();
        cardVlg.padding = new RectOffset(20, 20, 18, 18);
        cardVlg.spacing = 15;
        cardVlg.childAlignment = TextAnchor.UpperLeft;
        cardVlg.childControlHeight = true;
        cardVlg.childControlWidth = true;
        cardVlg.childForceExpandHeight = false;
        cardVlg.childForceExpandWidth = true;

        TMP_FontAsset font = FindFontAsset();
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        ColorUtility.TryParseHtmlString("#1A202D", out Color darkCol);
        ColorUtility.TryParseHtmlString("#7A9EAB", out Color subCol);
        ColorUtility.TryParseHtmlString("#E2E8F0", out Color borderCol);
        ColorUtility.TryParseHtmlString("#A0AAB4", out Color timeCol);

        Color[] statusColors = new Color[3];
        ColorUtility.TryParseHtmlString("#50B8B8", out statusColors[0]); // Green/Mint
        ColorUtility.TryParseHtmlString("#E53E3E", out statusColors[1]); // Red
        ColorUtility.TryParseHtmlString("#DD6B20", out statusColors[2]); // Orange

        // --- 1. Top Header Area ---
        GameObject headerArea = new GameObject("HeaderArea");
        headerArea.transform.SetParent(targetCard, false);
        HorizontalLayoutGroup hlgHeader = headerArea.AddComponent<HorizontalLayoutGroup>();
        hlgHeader.spacing = 10;
        hlgHeader.childAlignment = TextAnchor.MiddleLeft;
        hlgHeader.childControlHeight = true;
        hlgHeader.childControlWidth = true;
        hlgHeader.childForceExpandHeight = false;
        hlgHeader.childForceExpandWidth = false; 

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(headerArea.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        Sprite clockIcon = FindSpriteByName("아이콘-시계");
        if (clockIcon == null) clockIcon = FindSpriteByName("아이콘-히스토리");
        if (clockIcon != null) iconImg.sprite = clockIcon;
        iconImg.color = statusColors[0];
        LayoutElement iconLe = iconObj.AddComponent<LayoutElement>();
        iconLe.preferredWidth = 20f;
        iconLe.preferredHeight = 20f;
        iconLe.minWidth = 20f;
        iconLe.minHeight = 20f;
        iconLe.flexibleWidth = 0f;

        GameObject titleVArea = new GameObject("TitleVArea");
        titleVArea.transform.SetParent(headerArea.transform, false);
        VerticalLayoutGroup vlgTitle = titleVArea.AddComponent<VerticalLayoutGroup>();
        vlgTitle.spacing = 2;
        vlgTitle.childAlignment = TextAnchor.MiddleLeft;
        vlgTitle.childControlHeight = true;
        vlgTitle.childControlWidth = true;
        vlgTitle.childForceExpandHeight = false;
        vlgTitle.childForceExpandWidth = true;
        LayoutElement titleVLe = titleVArea.AddComponent<LayoutElement>();
        titleVLe.flexibleWidth = 1f; 

        GameObject mainTitle = new GameObject("MainTitle");
        mainTitle.transform.SetParent(titleVArea.transform, false);
        TextMeshProUGUI mainTitleTxt = mainTitle.AddComponent<TextMeshProUGUI>();
        mainTitleTxt.text = "이상 감지 타임라인";
        if (font != null) mainTitleTxt.font = font;
        mainTitleTxt.fontSize = 16;
        mainTitleTxt.fontStyle = FontStyles.Bold;
        mainTitleTxt.color = darkCol;

        GameObject subTitle = new GameObject("SubTitle");
        subTitle.transform.SetParent(titleVArea.transform, false);
        TextMeshProUGUI subTitleTxt = subTitle.AddComponent<TextMeshProUGUI>();
        subTitleTxt.text = "최근 24시간 이벤트";
        if (font != null) subTitleTxt.font = font;
        subTitleTxt.fontSize = 12;
        subTitleTxt.color = subCol;

        // --- 2. Timeline List Area (Scroll View) ---
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(targetCard, false);
        LayoutElement scrollLe = scrollView.AddComponent<LayoutElement>();
        scrollLe.flexibleHeight = 1f;
        scrollLe.flexibleWidth = 1f;
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 20f;
        AddScrollbarToScrollRect(sr);

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        sr.viewport = vpRt;

        GameObject listArea = new GameObject("Content");
        listArea.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = listArea.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup listVlg = listArea.AddComponent<VerticalLayoutGroup>();
        listVlg.spacing = 0; 
        listVlg.childAlignment = TextAnchor.UpperLeft;
        listVlg.childControlHeight = true;
        listVlg.childControlWidth = true;
        listVlg.childForceExpandHeight = false;
        listVlg.childForceExpandWidth = true;

        ContentSizeFitter csf = listArea.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRt;

        string[] tTimes = { "10:15:32", "09:42:18", "08:30:00", "07:15:45", "03:22:11", "00:00:00" };
        string[] tTitles = { "P-202 베어링 진동 임계 초과", "RO-301 차압 상승 경고", "일일 진단 완료", "ERD-01 효율 점검 완료", "P-202 진동 주의 단계 진입", "야간 자동 진단 시작" };
        string[] tDescs = { "진동 4.2mm/s (임계 3.5mm/s). RUL 추정: 15일. 정비 계획 수립 필요.", "차압 0.3bar 상승 감지. 멤브레인 세정 D-3 권장.", "10개 설비 정기 진단 수행. 종합 건전성 92.4점.", "에너지 회수율 94.2%. 정상 범위 내.", "진동 3.1mm/s → 3.6mm/s 상승 추세.", "스케줄 기반 진동/온도/압력 종합 진단 실행." };
        int[] tTypes = { 1, 2, 0, 0, 2, 0 }; // 0: Green, 1: Red, 2: Orange

        for (int i = 0; i < tTitles.Length; i++)
        {
            GameObject listItem = new GameObject("TimelineItem_" + i);
            listItem.transform.SetParent(listArea.transform, false);

            HorizontalLayoutGroup itemHlg = listItem.AddComponent<HorizontalLayoutGroup>();
            itemHlg.padding = new RectOffset(0, 0, 0, 0);
            itemHlg.spacing = 0;
            itemHlg.childAlignment = TextAnchor.UpperLeft;
            itemHlg.childControlHeight = true;
            itemHlg.childControlWidth = true;
            itemHlg.childForceExpandHeight = true; // To stretch the left line
            itemHlg.childForceExpandWidth = false;

            // --- Left Graphics (Line + Dot) ---
            GameObject leftGfx = new GameObject("LeftGraphics");
            leftGfx.transform.SetParent(listItem.transform, false);
            LayoutElement leftLe = leftGfx.AddComponent<LayoutElement>();
            leftLe.minWidth = 40f;
            leftLe.preferredWidth = 40f;
            leftLe.flexibleWidth = 0f;

            // Vertical Line
            GameObject vLine = new GameObject("VLine");
            vLine.transform.SetParent(leftGfx.transform, false);
            Image lineImg = vLine.AddComponent<Image>();
            lineImg.color = borderCol;
            RectTransform lineRt = vLine.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 0);
            lineRt.anchorMax = new Vector2(0.5f, 1f);
            lineRt.offsetMin = new Vector2(-1, 0);
            lineRt.offsetMax = new Vector2(1, 0); // 2px width
            
            // If first or last item, we could shorten the line, but full height is often fine in timeline lists.
            if (i == 0) lineRt.offsetMax = new Vector2(1, -25); // Don't go above the first dot
            if (i == tTitles.Length - 1) lineRt.offsetMin = new Vector2(-1, 25); // Don't go below the last dot

            // Dot
            GameObject dotHalo = new GameObject("DotHalo");
            dotHalo.transform.SetParent(leftGfx.transform, false);
            Image haloImg = dotHalo.AddComponent<Image>();
            haloImg.sprite = knobSprite;
            haloImg.color = statusColors[tTypes[i]];
            RectTransform haloRt = dotHalo.GetComponent<RectTransform>();
            haloRt.anchorMin = new Vector2(0.5f, 1f);
            haloRt.anchorMax = new Vector2(0.5f, 1f);
            haloRt.offsetMin = new Vector2(-6, -30); // 12x12 dot, 24px from top
            haloRt.offsetMax = new Vector2(6, -18);

            GameObject dotCore = new GameObject("DotCore");
            dotCore.transform.SetParent(dotHalo.transform, false);
            Image coreImg = dotCore.AddComponent<Image>();
            coreImg.sprite = knobSprite;
            coreImg.color = Color.white;
            RectTransform coreRt = dotCore.GetComponent<RectTransform>();
            coreRt.anchorMin = new Vector2(0.5f, 0.5f);
            coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.offsetMin = new Vector2(-3, -3); // 6x6 inner white dot
            coreRt.offsetMax = new Vector2(3, 3);

            // --- Right Text Area ---
            GameObject textVArea = new GameObject("TextArea");
            textVArea.transform.SetParent(listItem.transform, false);
            VerticalLayoutGroup textVlg = textVArea.AddComponent<VerticalLayoutGroup>();
            textVlg.padding = new RectOffset(0, 20, 20, 20); // Increased vertical padding
            textVlg.spacing = 8; // Increased spacing
            textVlg.childAlignment = TextAnchor.UpperLeft;
            textVlg.childControlHeight = true;
            textVlg.childControlWidth = true;
            textVlg.childForceExpandHeight = false;
            textVlg.childForceExpandWidth = true;
            LayoutElement textLe = textVArea.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;

            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI timeTxt = timeObj.AddComponent<TextMeshProUGUI>();
            timeTxt.text = tTimes[i];
            if (font != null) timeTxt.font = font;
            timeTxt.fontSize = 11;
            timeTxt.color = timeCol;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = tTitles[i];
            if (font != null) titleTxt.font = font;
            titleTxt.fontSize = 14;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = darkCol;

            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = tDescs[i];
            if (font != null) descTxt.font = font;
            descTxt.fontSize = 12;
            descTxt.color = subCol;

            // Horizontal Divider (except last item)
            if (i < tTitles.Length - 1)
            {
                GameObject divider = new GameObject("Divider");
                divider.transform.SetParent(textVArea.transform, false);
                LayoutElement divLe = divider.AddComponent<LayoutElement>();
                divLe.ignoreLayout = true; // Ignore layout so it doesn't take vertical space
                Image divImg = divider.AddComponent<Image>();
                divImg.color = borderCol;
                RectTransform divRt = divider.GetComponent<RectTransform>();
                divRt.anchorMin = new Vector2(0, 0);
                divRt.anchorMax = new Vector2(1, 0);
                divRt.offsetMin = new Vector2(0, 0);
                divRt.offsetMax = new Vector2(0, 1); // 1px height
            }
        }

        Debug.Log("이상 감지 타임라인이 성공적으로 생성되었습니다.");
    }

    [MenuItem("Tools/Populate Equipment Health List (Safe)")]
    public static void PopulateEquipmentHealthList()
    {
        GameObject page = GameObject.Find("Page_EquipmentHealth");
        if (page == null)
        {
            Debug.LogError("Page_EquipmentHealth not found in the scene!");
            return;
        }

        Transform targetCard = null;
        Transform[] allTransforms = page.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == "Card_설비 건전성 현황")
            {
                targetCard = t;
                break;
            }
        }

        if (targetCard == null)
        {
            Debug.LogError("Card_설비 건전성 현황 not found in Page_EquipmentHealth!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(targetCard.gameObject, "Populate Equipment Health List");

        // Clear existing children safely
        for (int i = targetCard.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(targetCard.GetChild(i).gameObject);
        }

        // Configure targetCard layout
        VerticalLayoutGroup cardVlg = targetCard.GetComponent<VerticalLayoutGroup>();
        if (cardVlg == null) cardVlg = targetCard.gameObject.AddComponent<VerticalLayoutGroup>();
        cardVlg.padding = new RectOffset(20, 20, 18, 18);
        cardVlg.spacing = 20;
        cardVlg.childAlignment = TextAnchor.UpperLeft;
        cardVlg.childControlHeight = true;
        cardVlg.childControlWidth = true;
        cardVlg.childForceExpandHeight = false;
        cardVlg.childForceExpandWidth = true;

        TMP_FontAsset font = FindFontAsset();
        Sprite basicSprite = FindSpriteByName("기본패널");
        if (basicSprite == null) basicSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        ColorUtility.TryParseHtmlString("#1A202D", out Color darkCol);
        ColorUtility.TryParseHtmlString("#7A9EAB", out Color subCol);
        ColorUtility.TryParseHtmlString("#E2E8F0", out Color borderCol);
        
        Color[] statusColors = new Color[3];
        ColorUtility.TryParseHtmlString("#50B8B8", out statusColors[0]); // Green/Mint
        ColorUtility.TryParseHtmlString("#E53E3E", out statusColors[1]); // Red
        ColorUtility.TryParseHtmlString("#DD6B20", out statusColors[2]); // Orange

        Color[] haloColors = new Color[3];
        ColorUtility.TryParseHtmlString("#E6F6F6", out haloColors[0]);
        ColorUtility.TryParseHtmlString("#FEE2E2", out haloColors[1]);
        ColorUtility.TryParseHtmlString("#FEEBC8", out haloColors[2]);

        // --- 1. Top Header Area ---
        GameObject headerArea = new GameObject("HeaderArea");
        headerArea.transform.SetParent(targetCard, false);
        HorizontalLayoutGroup hlgHeader = headerArea.AddComponent<HorizontalLayoutGroup>();
        hlgHeader.spacing = 10;
        hlgHeader.childAlignment = TextAnchor.MiddleLeft;
        hlgHeader.childControlHeight = true;
        hlgHeader.childControlWidth = true;
        hlgHeader.childForceExpandHeight = false;
        hlgHeader.childForceExpandWidth = false; // Changed to false to prevent icon stretching!

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(headerArea.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        Sprite warningIcon = FindSpriteByName("아이콘-경고");
        if (warningIcon != null) iconImg.sprite = warningIcon;
        iconImg.color = statusColors[0];
        LayoutElement iconLe = iconObj.AddComponent<LayoutElement>();
        iconLe.preferredWidth = 24f;
        iconLe.preferredHeight = 24f;
        iconLe.minWidth = 24f;
        iconLe.minHeight = 24f;
        iconLe.flexibleWidth = 0f;

        GameObject titleVArea = new GameObject("TitleVArea");
        titleVArea.transform.SetParent(headerArea.transform, false);
        VerticalLayoutGroup vlgTitle = titleVArea.AddComponent<VerticalLayoutGroup>();
        vlgTitle.spacing = 2;
        vlgTitle.childAlignment = TextAnchor.MiddleLeft;
        vlgTitle.childControlHeight = true;
        vlgTitle.childControlWidth = true;
        vlgTitle.childForceExpandHeight = false;
        vlgTitle.childForceExpandWidth = true;
        LayoutElement titleVLe = titleVArea.AddComponent<LayoutElement>();
        titleVLe.flexibleWidth = 1f; // Takes remaining space

        GameObject mainTitle = new GameObject("MainTitle");
        mainTitle.transform.SetParent(titleVArea.transform, false);
        TextMeshProUGUI mainTitleTxt = mainTitle.AddComponent<TextMeshProUGUI>();
        mainTitleTxt.text = "설비 건전성 현황";
        if (font != null) mainTitleTxt.font = font;
        mainTitleTxt.fontSize = 16;
        mainTitleTxt.fontStyle = FontStyles.Bold;
        mainTitleTxt.color = darkCol;

        GameObject subTitle = new GameObject("SubTitle");
        subTitle.transform.SetParent(titleVArea.transform, false);
        TextMeshProUGUI subTitleTxt = subTitle.AddComponent<TextMeshProUGUI>();
        subTitleTxt.text = "10개 주요 설비 실시간 상태";
        if (font != null) subTitleTxt.font = font;
        subTitleTxt.fontSize = 12;
        subTitleTxt.color = subCol;

        // --- 2. List Area (Scroll View) ---
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(targetCard, false);
        LayoutElement scrollLe = scrollView.AddComponent<LayoutElement>();
        scrollLe.flexibleHeight = 1f;
        scrollLe.flexibleWidth = 1f;
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 20f;
        AddScrollbarToScrollRect(sr);

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform vpRt = viewport.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        sr.viewport = vpRt;

        GameObject listArea = new GameObject("Content");
        listArea.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = listArea.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup listVlg = listArea.AddComponent<VerticalLayoutGroup>();
        listVlg.spacing = 15;
        listVlg.childAlignment = TextAnchor.UpperCenter;
        listVlg.childControlHeight = true;
        listVlg.childControlWidth = true;
        listVlg.childForceExpandHeight = false;
        listVlg.childForceExpandWidth = true;

        ContentSizeFitter csf = listArea.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRt;

        string[] eqNames = { "취수펌프 (P-101)", "고압펌프 베어링 (P-202)", "RO 멤브레인 (RO-301)", "전처리 DMF (F-101)", "에너지 회수장치 (ERD-01)", "약품주입 펌프 (CP-01)" };
        string[] eqDescs = { "정상 운전 · 진동 0.8mm/s", "진동 이상 감지 · 4.2mm/s (임계: 3.5)", "차압 상승 · 세정 D-3 권장", "정상 · 차압 0.4bar", "정상 · 효율 94%", "정상 · 유량 안정" };
        int[] eqTypes = { 0, 1, 2, 0, 0, 0 }; // 0: Green, 1: Red, 2: Orange
        int[] eqScores = { 96, 42, 68, 91, 94, 98 };

        for (int i = 0; i < eqNames.Length; i++)
        {
            GameObject listItem = new GameObject("Item_" + eqNames[i]);
            listItem.transform.SetParent(listArea.transform, false);

            Image itemBg = listItem.AddComponent<Image>();
            itemBg.sprite = basicSprite;
            itemBg.type = Image.Type.Sliced;
            itemBg.color = Color.white;
            itemBg.pixelsPerUnitMultiplier = 2f;

            Outline itemOut = listItem.AddComponent<Outline>();
            itemOut.effectColor = borderCol;
            itemOut.effectDistance = new Vector2(0, -1f);

            HorizontalLayoutGroup itemHlg = listItem.AddComponent<HorizontalLayoutGroup>();
            itemHlg.padding = new RectOffset(20, 20, 20, 20); // Increased vertical padding
            itemHlg.spacing = 15;
            itemHlg.childAlignment = TextAnchor.MiddleLeft;
            itemHlg.childControlHeight = true;
            itemHlg.childControlWidth = true;
            itemHlg.childForceExpandHeight = false;
            itemHlg.childForceExpandWidth = false;

            // Status Dot
            GameObject dotHalo = new GameObject("DotHalo");
            dotHalo.transform.SetParent(listItem.transform, false);
            Image haloImg = dotHalo.AddComponent<Image>();
            haloImg.sprite = knobSprite;
            haloImg.color = haloColors[eqTypes[i]];
            LayoutElement haloLe = dotHalo.AddComponent<LayoutElement>();
            haloLe.preferredWidth = 16f;
            haloLe.preferredHeight = 16f;
            haloLe.minWidth = 16f;
            haloLe.minHeight = 16f;
            haloLe.flexibleWidth = 0f;
            haloLe.flexibleHeight = 0f;

            GameObject dotCore = new GameObject("DotCore");
            dotCore.transform.SetParent(dotHalo.transform, false);
            Image coreImg = dotCore.AddComponent<Image>();
            coreImg.sprite = knobSprite;
            coreImg.color = statusColors[eqTypes[i]];
            RectTransform coreRt = dotCore.GetComponent<RectTransform>();
            coreRt.anchorMin = new Vector2(0.3f, 0.3f);
            coreRt.anchorMax = new Vector2(0.7f, 0.7f);
            coreRt.offsetMin = Vector2.zero;
            coreRt.offsetMax = Vector2.zero;

            // Text Area
            GameObject textVArea = new GameObject("TextArea");
            textVArea.transform.SetParent(listItem.transform, false);
            VerticalLayoutGroup textVlg = textVArea.AddComponent<VerticalLayoutGroup>();
            textVlg.spacing = 8; // Increased spacing between texts
            textVlg.childAlignment = TextAnchor.MiddleLeft;
            textVlg.childControlHeight = true;
            textVlg.childControlWidth = true;
            textVlg.childForceExpandHeight = false;
            textVlg.childForceExpandWidth = true;
            LayoutElement textLe = textVArea.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f; // Take remaining space

            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = eqNames[i];
            if (font != null) nameTxt.font = font;
            nameTxt.fontSize = 14;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color = darkCol;

            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = eqDescs[i];
            if (font != null) descTxt.font = font;
            descTxt.fontSize = 12;
            descTxt.color = eqTypes[i] == 0 ? subCol : statusColors[eqTypes[i]];

            // Progress Area
            GameObject progArea = new GameObject("ProgressArea");
            progArea.transform.SetParent(listItem.transform, false);
            HorizontalLayoutGroup progHlg = progArea.AddComponent<HorizontalLayoutGroup>();
            progHlg.spacing = 15;
            progHlg.childAlignment = TextAnchor.MiddleRight;
            progHlg.childControlHeight = true;
            progHlg.childControlWidth = true;
            progHlg.childForceExpandHeight = false;
            progHlg.childForceExpandWidth = false;

            GameObject barBg = new GameObject("BarBg");
            barBg.transform.SetParent(progArea.transform, false);
            Image bgImg = barBg.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#F1F5F9", out Color bgC);
            bgImg.color = bgC;
            LayoutElement barLe = barBg.AddComponent<LayoutElement>();
            barLe.preferredWidth = 100f;
            barLe.preferredHeight = 6f;

            GameObject barFill = new GameObject("Fill");
            barFill.transform.SetParent(barBg.transform, false);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.color = statusColors[eqTypes[i]];
            RectTransform fillRt = barFill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(eqScores[i] / 100f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            GameObject scoreObj = new GameObject("Score");
            scoreObj.transform.SetParent(progArea.transform, false);
            TextMeshProUGUI scoreTxt = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTxt.text = eqScores[i] + "%";
            if (font != null) scoreTxt.font = font;
            scoreTxt.fontSize = 15;
            scoreTxt.fontStyle = FontStyles.Bold;
            scoreTxt.color = statusColors[eqTypes[i]];
            scoreTxt.alignment = TextAlignmentOptions.Right;
            LayoutElement scoreLe = scoreObj.AddComponent<LayoutElement>();
            scoreLe.preferredWidth = 40f;
        }

        Debug.Log("설비 건전성 현황 리스트가 성공적으로 생성되었습니다.");
    }

    [MenuItem("Tools/Populate All Pages KPIs (Safe)")]
    public static void PopulateAllPagesKPIs()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02Monitoring/New UI/KPI_Card_Template.prefab");
        if (prefab == null)
        {
            Debug.LogError("KPI_Card_Template.prefab not found in Assets/02Monitoring/New UI!");
            return;
        }

        string[] pages = { "Page_EquipmentHealth", "Page_AlarmHistory", "Page_Maintenance", "Page_RemoteInspection" };
        
        string[][] pageTitles = {
            new[] { "플랜트 종합 건전성", "정상 설비", "주의 설비", "위험 설비" },
            new[] { "활성화된 고장예지 알람", "해제된 고장예지 알람", "총 고장예지 알람" },
            new[] { "이번 달 유지보수 작업", "유지보수 진행중 작업", "완료된 작업", "설비 가동률" },
            new[] { "이번 달 점검작업", "진행중 점검", "완료된 점검", "이상 발견율" }
        };

        string[][] pageValues = {
            new[] { "98.5", "145", "12", "3" },
            new[] { "3", "12", "15" },
            new[] { "24", "5", "19", "94.2" },
            new[] { "45", "2", "43", "4.5" }
        };

        string[][] pageUnits = {
            new[] { "%", "대", "대", "대" },
            new[] { "건", "건", "건" },
            new[] { "건", "건", "건", "%" },
            new[] { "건", "건", "건", "%" }
        };

        string[][] pageSubtexts = {
            new[] { "전일 대비: 0.2% 증가", "전일과 동일", "관심 필요", "즉시 조치 필요" },
            new[] { "전일 대비: 1건 증가", "전일 대비: 2건 감소", "이번 달 총 발생" },
            new[] { "계획 대비: 2건 추가", "금일 완료 예정: 2건", "목표 달성 순항 중", "전월 대비: 1.5% 하락" },
            new[] { "정기 점검 포함", "원격 연결 중", "보고서 대기: 4건", "전월 대비: 0.5% 상승" }
        };

        string[][] pageIndicators = {
            new[] { "▲ 0.2", "-", "▲ 2", "▼ 1" },
            new[] { "▲ 1", "▼ 2", "-" },
            new[] { "▲ 2", "-", "-", "▼ 1.5" },
            new[] { "-", "▶", "-", "▲ 0.5" }
        };

        for (int p = 0; p < pages.Length; p++)
        {
            GameObject pageObj = null;
            Transform[] allT = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allT)
            {
                if (t.name == pages[p] && t.gameObject.scene.isLoaded)
                {
                    pageObj = t.gameObject;
                    break;
                }
            }
            if (pageObj == null) continue;

            Transform kpiRow = pageObj.transform.Find("Row_KPI");
            if (kpiRow == null) kpiRow = pageObj.transform.Find("Row"); // fallback
            if (kpiRow == null) continue;

            Undo.RegisterFullObjectHierarchyUndo(kpiRow.gameObject, "Populate KPIs for " + pages[p]);

            for (int i = kpiRow.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(kpiRow.GetChild(i).gameObject);
            }

            for (int i = 0; i < pageTitles[p].Length; i++)
            {
                GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                card.name = "KPICard_" + pageTitles[p][i];
                card.transform.SetParent(kpiRow, false);

                LayoutElement le = card.GetComponent<LayoutElement>();
                if (le == null) le = card.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.preferredWidth = 0f;
                le.minWidth = 0f;

                Transform tRow = card.transform.Find("1_TitleRow");
                if (tRow != null)
                {
                    var txt = tRow.GetComponent<TextMeshProUGUI>();
                    if (txt != null) txt.text = pageTitles[p][i];
                }

                Transform vRow = card.transform.Find("2_ValueUnitRow");
                if (vRow != null)
                {
                    Transform vObj = vRow.Find("Value");
                    if (vObj != null)
                    {
                        var txt = vObj.GetComponent<TextMeshProUGUI>();
                        if (txt != null) txt.text = pageValues[p][i];
                    }
                    Transform uObj = vRow.Find("Unit");
                    if (uObj != null)
                    {
                        var txt = uObj.GetComponent<TextMeshProUGUI>();
                        if (txt != null) txt.text = pageUnits[p][i];
                    }
                }

                Transform sRow = card.transform.Find("3_SubTextRow");
                if (sRow != null)
                {
                    Transform sObj = sRow.Find("SubText");
                    if (sObj != null)
                    {
                        var txt = sObj.GetComponent<TextMeshProUGUI>();
                        if (txt != null) txt.text = pageSubtexts[p][i];
                    }
                    
                    Transform iObj = sRow.Find("Indicator");
                    if (iObj != null)
                    {
                        Transform iTxtObj = iObj.Find("Text");
                        if (iTxtObj != null)
                        {
                            var txt = iTxtObj.GetComponent<TextMeshProUGUI>();
                            if (txt != null) txt.text = pageIndicators[p][i];
                        }
                    }
                }
            }
        }

        Debug.Log("모든 탭의 KPI가 생성된 프리팹으로 성공적으로 채워졌습니다!");
    }

    [MenuItem("Tools/Generate KPI Card Template")]
    public static void GenerateKPITemplate()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }

        Sprite panelSprite = FindSpriteByName("복합패널");
        if (panelSprite == null) panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        TMP_FontAsset font = FindFontAsset();

        Color cWhite = Color.white;
        ColorUtility.TryParseHtmlString("#1A202D", out Color cText);
        ColorUtility.TryParseHtmlString("#E2E8F0", out Color cBorder);

        GameObject card = new GameObject("KPI_Card_Template");
        card.transform.SetParent(canvas.transform, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 150);

        Image img = card.AddComponent<Image>();
        img.color = cWhite;
        img.sprite = panelSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 2f;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = cBorder;
        outline.effectDistance = new Vector2(0, -2f);

        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(20, 20, 20, 20);
        cardLayout.spacing = 10;
        cardLayout.childAlignment = TextAnchor.UpperLeft;
        cardLayout.childControlHeight = true;
        cardLayout.childControlWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childForceExpandWidth = true;

        // 1. Title Row
        GameObject titleRow = new GameObject("1_TitleRow");
        titleRow.transform.SetParent(card.transform, false);
        TextMeshProUGUI titleTxt = titleRow.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "현재 전력 소모";
        if (font != null) titleTxt.font = font;
        titleTxt.fontSize = 15;
        ColorUtility.TryParseHtmlString("#7A9EAB", out Color tColor);
        titleTxt.color = tColor;
        titleTxt.alignment = TextAlignmentOptions.Left;

        // 2. Value + Unit Row
        GameObject valRow = new GameObject("2_ValueUnitRow");
        valRow.transform.SetParent(card.transform, false);
        HorizontalLayoutGroup valHlg = valRow.AddComponent<HorizontalLayoutGroup>();
        valHlg.childControlHeight = true;
        valHlg.childControlWidth = true;
        valHlg.childForceExpandHeight = false;
        valHlg.childForceExpandWidth = false;
        valHlg.spacing = 6;
        valHlg.childAlignment = TextAnchor.LowerLeft;

        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(valRow.transform, false);
        TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
        valTxt.text = "4.5";
        if (font != null) valTxt.font = font;
        valTxt.fontSize = 38;
        valTxt.fontStyle = FontStyles.Bold;
        valTxt.color = cText;

        GameObject unitObj = new GameObject("Unit");
        unitObj.transform.SetParent(valRow.transform, false);
        TextMeshProUGUI unitTxt = unitObj.AddComponent<TextMeshProUGUI>();
        unitTxt.text = "kW";
        if (font != null) unitTxt.font = font;
        unitTxt.fontSize = 18;
        unitTxt.fontStyle = FontStyles.Bold;
        unitTxt.color = tColor;

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(card.transform, false);
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.flexibleHeight = 1f;

        // 3. SubText + Indicator Row
        GameObject subRow = new GameObject("3_SubTextRow");
        subRow.transform.SetParent(card.transform, false);
        HorizontalLayoutGroup subHlg = subRow.AddComponent<HorizontalLayoutGroup>();
        subHlg.childControlHeight = true;
        subHlg.childControlWidth = true;
        subHlg.childForceExpandHeight = false;
        subHlg.childForceExpandWidth = false;
        subHlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject subTxtObj = new GameObject("SubText");
        subTxtObj.transform.SetParent(subRow.transform, false);
        TextMeshProUGUI subText = subTxtObj.AddComponent<TextMeshProUGUI>();
        subText.text = "전일 평균: 4.3 kW";
        if (font != null) subText.font = font;
        subText.fontSize = 13;
        ColorUtility.TryParseHtmlString("#A0AAB4", out Color sColor);
        subText.color = sColor;
        LayoutElement subLE = subTxtObj.AddComponent<LayoutElement>();
        subLE.flexibleWidth = 1f;

        GameObject indObj = new GameObject("Indicator");
        indObj.transform.SetParent(subRow.transform, false);
        Image indImg = indObj.AddComponent<Image>();
        ColorUtility.TryParseHtmlString("#FEE2E2", out Color indBg);
        indImg.color = indBg;
        HorizontalLayoutGroup indHlg = indObj.AddComponent<HorizontalLayoutGroup>();
        indHlg.padding = new RectOffset(6, 6, 2, 2);
        indHlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject indTxtObj = new GameObject("Text");
        indTxtObj.transform.SetParent(indObj.transform, false);
        TextMeshProUGUI indTxt = indTxtObj.AddComponent<TextMeshProUGUI>();
        indTxt.text = "▲ 4.7%";
        if (font != null) indTxt.font = font;
        indTxt.fontSize = 12;
        indTxt.fontStyle = FontStyles.Bold;
        ColorUtility.TryParseHtmlString("#DC2626", out Color indTCol);
        indTxt.color = indTCol;

        // 4. Progress Bar Row
        GameObject progRow = new GameObject("4_ProgressBar");
        progRow.transform.SetParent(card.transform, false);
        LayoutElement progLE = progRow.AddComponent<LayoutElement>();
        progLE.preferredHeight = 4f;
        Image progBg = progRow.AddComponent<Image>();
        ColorUtility.TryParseHtmlString("#F1F5F9", out Color pBgCol);
        progBg.color = pBgCol;

        GameObject progFill = new GameObject("Fill");
        progFill.transform.SetParent(progRow.transform, false);
        Image fillImg = progFill.AddComponent<Image>();
        ColorUtility.TryParseHtmlString("#F59E0B", out Color fillCol);
        fillImg.color = fillCol;
        RectTransform fillRt = progFill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(0.7f, 1f); // 70% filled
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        
        Selection.activeGameObject = card;
        Debug.Log("KPI Card 템플릿이 Canvas에 생성되었습니다. 프리팹으로 저장하세요!");
    }

    [MenuItem("Tools/Build Monitoring3 UI Skeleton (Updated)")]
    public static void BuildSkeleton()
    {
        GameObject oldRoot = GameObject.Find("Monitoring3_New_Pages");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

        Transform contentTransform = null;
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name.Equals("content", System.StringComparison.OrdinalIgnoreCase) && t.gameObject.scene.isLoaded)
            {
                if (t.parent != null && t.parent.name.IndexOf("viewport", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (t.parent.parent != null && t.parent.parent.name.Contains("컨텐츠 영역"))
                    {
                        contentTransform = t;
                        break;
                    }
                }
            }
        }

        if (contentTransform == null)
        {
            Debug.LogError("씬에서 'Viewport/Content' 구조를 찾지 못했습니다. 씬 구조를 확인해주세요.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(contentTransform.gameObject, "Build UI Skeleton");

        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(contentTransform.GetChild(i).gameObject);
        }

        Sprite basicSprite = FindSpriteByName("기본패널");
        if (basicSprite == null) basicSprite = FindSpriteByName("패널");
        if (basicSprite == null) basicSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Sprite compSprite = FindSpriteByName("복합패널");
        if (compSprite == null) compSprite = basicSprite;

        TMP_FontAsset fontAsset = FindFontAsset();

        Color colorWhite = Color.white;
        Color colorImportantText; ColorUtility.TryParseHtmlString("#1A202D", out colorImportantText);
        Color colorBorder; ColorUtility.TryParseHtmlString("#E2E8F0", out colorBorder);

        CreateEquipmentHealthPage(contentTransform, basicSprite, compSprite, fontAsset, colorWhite, colorImportantText, colorBorder);
        CreateAlarmHistoryPage(contentTransform, basicSprite, compSprite, fontAsset, colorWhite, colorImportantText, colorBorder);
        CreateMaintenancePage(contentTransform, basicSprite, compSprite, fontAsset, colorWhite, colorImportantText, colorBorder);
        CreateRemoteInspectionPage(contentTransform, basicSprite, compSprite, fontAsset, colorWhite, colorImportantText, colorBorder);

        for(int i = 0; i < contentTransform.childCount; i++)
        {
            contentTransform.GetChild(i).gameObject.SetActive(i == 0);
        }

        Debug.Log("Monitoring3 UI Skeleton이 Content 영역 아래에 성공적으로 생성되었습니다!");
    }

    private static Sprite FindSpriteByName(string match)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/02Monitoring/New UI" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("호버") || path.Contains("Hover") || path.Contains("활성") || path.Contains("클릭") || path.Contains("눌림"))
                continue;
            
            if (path.Contains(match))
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    private static TMP_FontAsset FindFontAsset()
    {
        string[] fontGuids = AssetDatabase.FindAssets("NotoSansKR-Bold SDF t:TMP_FontAsset");
        if (fontGuids.Length == 0) fontGuids = AssetDatabase.FindAssets("NotoSansKR-Medium SDF t:TMP_FontAsset");
        if (fontGuids.Length > 0) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        return null;
    }

    private static void CreateEquipmentHealthPage(Transform parent, Sprite basicSprite, Sprite compSprite, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject page = CreatePage(parent, "Page_EquipmentHealth");
        CreatePageHeader(page, "설비 건전성 현황", "설비별 건전성 지표 확인 및 이상 감지 현황 | 마지막 갱신: 실시간", null, font);
        CreateKPIRow(page, new[] { "플랜트 종합 건전성", "정상 설비", "주의 설비", "위험 설비" }, new[] { 1f, 1f, 1f, 1f }, 1f, basicSprite, font, cWhite, cText, cBorder);
        CreateRow(page, new[] { "설비 건전성 현황", "이상 감지 타임라인" }, new[] { 2f, 1f }, 3.5f, compSprite, font, cWhite, cText, cBorder);
        CreateRow(page, new[] { "설비별 실시간 센서 그래프" }, new[] { 1f }, 2.5f, compSprite, font, cWhite, cText, cBorder);
    }

    private static void CreateAlarmHistoryPage(Transform parent, Sprite basicSprite, Sprite compSprite, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject page = CreatePage(parent, "Page_AlarmHistory");
        CreatePageHeader(page, "알람 이력 및 통계", "과거 알람 발생 내역 조회 및 통계 데이터 | 마지막 갱신: 실시간", null, font);
        CreateKPIRow(page, new[] { "활성화된 고장예지 알람", "해제된 고장예지 알람", "총 고장예지 알람" }, new[] { 1f, 1f, 1f }, 1f, basicSprite, font, cWhite, cText, cBorder);
        CreateSearchBar(page, basicSprite, font);
        CreateRow(page, new[] { "고장예지 알람 이력 테이블" }, new[] { 1f }, 3f, compSprite, font, cWhite, cText, cBorder);
        CreateRow(page, new[] { "설비별 알람 발생 빈도", "심각도별 알람 추이" }, new[] { 1f, 1f }, 2.5f, compSprite, font, cWhite, cText, cBorder);
    }

    private static void CreateMaintenancePage(Transform parent, Sprite basicSprite, Sprite compSprite, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject page = CreatePage(parent, "Page_Maintenance");
        CreatePageHeader(page, "유지보수 작업 관리", "유지보수 작업 내역 조회 및 상태 추적 | 마지막 갱신: 실시간", "MR 원격접속 버튼", font);
        CreateKPIRow(page, new[] { "이번 달 유지보수 작업", "유지보수 진행중 작업", "완료된 작업", "설비 가동률" }, new[] { 1f, 1f, 1f, 1f }, 1f, basicSprite, font, cWhite, cText, cBorder);
        CreateSearchBar(page, basicSprite, font);
        CreateRow(page, new[] { "유지보수 작업 이력 테이블" }, new[] { 1f }, 3f, compSprite, font, cWhite, cText, cBorder);
        CreateRow(page, new[] { "월별 유지보수 수행 추이", "설비별 유지보수 발생 현황", "유지보수 작업 상태" }, new[] { 1f, 1f, 1f }, 2.5f, compSprite, font, cWhite, cText, cBorder);
    }

    private static void CreateRemoteInspectionPage(Transform parent, Sprite basicSprite, Sprite compSprite, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject page = CreatePage(parent, "Page_RemoteInspection");
        CreatePageHeader(page, "MR 원격점검 관리", "원격점검 수행 이력 및 결과 리포트 | 마지막 갱신: 실시간", "MR 원격접속 버튼", font);
        CreateKPIRow(page, new[] { "이번 달 점검작업", "진행중 점검", "완료된 점검", "이상 발견율" }, new[] { 1f, 1f, 1f, 1f }, 1f, basicSprite, font, cWhite, cText, cBorder);
        CreateSearchBar(page, basicSprite, font);
        CreateRow(page, new[] { "원격점검 이력 테이블", "기기 관리 패널" }, new[] { 2f, 1f }, 3f, compSprite, font, cWhite, cText, cBorder);
        CreateRow(page, new[] { "월별 점검 수행 추이", "설비별 점검 발생 현황", "점검 결과 비율" }, new[] { 1f, 1f, 1f }, 2.5f, compSprite, font, cWhite, cText, cBorder);
    }

    private static void CreatePageHeader(GameObject page, string title, string subText, string rightBtnText, TMP_FontAsset font)
    {
        GameObject headerObj = new GameObject("HeaderArea");
        headerObj.transform.SetParent(page.transform, false);
        
        HorizontalLayoutGroup hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(0, 0, 0, 10);
        hlg.childAlignment = TextAnchor.LowerLeft;

        LayoutElement le = headerObj.AddComponent<LayoutElement>();
        le.flexibleHeight = 0; 
        le.preferredHeight = 36f;
        le.minHeight = 0f;

        GameObject titleArea = new GameObject("TitleArea");
        titleArea.transform.SetParent(headerObj.transform, false);
        VerticalLayoutGroup vlg = titleArea.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 0;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement titleLE = titleArea.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 7f;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(titleArea.transform, false);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = title;
        if (font != null) titleTxt.font = font;
        titleTxt.fontSize = 20;
        ColorUtility.TryParseHtmlString("#50B8B8", out Color titleColor);
        titleTxt.color = titleColor;
        titleTxt.alignment = TextAlignmentOptions.Left;

        GameObject subObj = new GameObject("SubText");
        subObj.transform.SetParent(titleArea.transform, false);
        TextMeshProUGUI subTxt = subObj.AddComponent<TextMeshProUGUI>();
        subTxt.text = subText;
        if (font != null) subTxt.font = font;
        subTxt.fontSize = 13;
        ColorUtility.TryParseHtmlString("#7A9EAB", out Color subColor);
        subTxt.color = subColor;
        subTxt.alignment = TextAlignmentOptions.Left;

        if (!string.IsNullOrEmpty(rightBtnText))
        {
            GameObject btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02Monitoring/New UI/버튼프리팹.prefab");
            GameObject btnObj = null;
            if (btnPrefab != null)
            {
                btnObj = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab);
                btnObj.name = "RightButton";
                btnObj.transform.SetParent(headerObj.transform, false);
            }
            else
            {
                btnObj = new GameObject("RightButton");
                btnObj.transform.SetParent(headerObj.transform, false);
                HorizontalLayoutGroup btnLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
                btnLayout.padding = new RectOffset(5, 5, 2, 2);
                btnLayout.childAlignment = TextAnchor.MiddleCenter;
            }
            
            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            Sprite mainBtnSprite = FindSpriteByName("주요버튼");
            img.sprite = mainBtnSprite != null ? mainBtnSprite : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.color = Color.white;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;

            LayoutElement btnLe = btnObj.GetComponent<LayoutElement>();
            if (btnLe == null) btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.flexibleWidth = 1f;
            btnLe.preferredHeight = 36f;
            btnLe.minHeight = 0f;
            btnLe.minWidth = 0f;

            TextMeshProUGUI btnTxt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt == null)
            {
                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
                btnTxt.alignment = TextAlignmentOptions.Center;
            }
            
            btnTxt.text = rightBtnText;
            if (font != null) btnTxt.font = font;
            btnTxt.fontSize = 12;
            btnTxt.color = Color.white;
        }
    }

    private static void CreateSearchBar(GameObject page, Sprite basicSprite, TMP_FontAsset font)
    {
        GameObject row = new GameObject("SearchBarArea");
        row.transform.SetParent(page.transform, false);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 36f;
        rowLE.flexibleHeight = 0f;
        rowLE.minHeight = 0f;

        ColorUtility.TryParseHtmlString("#FFFFFF", out Color white);
        ColorUtility.TryParseHtmlString("#E2E8F0", out Color borderCol);

        GameObject searchField = new GameObject("SearchField");
        searchField.transform.SetParent(row.transform, false);
        LayoutElement searchLE = searchField.AddComponent<LayoutElement>();
        searchLE.preferredWidth = 1403f;
        searchLE.flexibleWidth = 1f;
        searchLE.preferredHeight = 36f;
        searchLE.minHeight = 0f;
        searchLE.minWidth = 0f;
        
        Image searchImg = searchField.AddComponent<Image>();
        searchImg.sprite = basicSprite;
        searchImg.type = Image.Type.Sliced;
        searchImg.color = white;

        Outline outline = searchField.AddComponent<Outline>();
        outline.effectColor = borderCol;
        outline.effectDistance = new Vector2(0, -2f);

        // REAL Functional TMP_InputField Setup (Replaces the broken static text & HorizontalLayoutGroup)
        TMP_InputField inputField = searchField.AddComponent<TMP_InputField>();

        // Text Area Child
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(searchField.transform, false);
        RectTransform taRt = textArea.AddComponent<RectTransform>();
        taRt.anchorMin = new Vector2(0f, 0f);
        taRt.anchorMax = new Vector2(1f, 1f);
        taRt.offsetMin = new Vector2(10f, 2f);
        taRt.offsetMax = new Vector2(-10f, -2f);
        textArea.AddComponent<RectMask2D>();

        // Placeholder Child (Inside Text Area)
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        RectTransform pRt = placeholderObj.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0f, 0f);
        pRt.anchorMax = new Vector2(1f, 1f);
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;
        TextMeshProUGUI placeholderTxt = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTxt.text = "검색어를 입력하세요...";
        if (font != null) placeholderTxt.font = font;
        placeholderTxt.fontSize = 12;
        ColorUtility.TryParseHtmlString("#7A9EAB", out Color subTextCol);
        placeholderTxt.color = subTextCol;
        placeholderTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // Text Child (Inside Text Area)
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform tRt = textObj.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        TextMeshProUGUI textTxt = textObj.AddComponent<TextMeshProUGUI>();
        if (font != null) textTxt.font = font;
        textTxt.fontSize = 13;
        ColorUtility.TryParseHtmlString("#1A202D", out Color darkTextCol);
        textTxt.color = darkTextCol;
        textTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // Wire references of TMP_InputField
        inputField.textViewport = taRt;
        inputField.textComponent = textTxt;
        inputField.placeholder = placeholderTxt;

        GameObject btnArea = new GameObject("ButtonArea");
        btnArea.transform.SetParent(row.transform, false);
        LayoutElement btnAreaLE = btnArea.AddComponent<LayoutElement>();
        btnAreaLE.preferredWidth = 212f;
        btnAreaLE.flexibleWidth = 0f;
        btnAreaLE.minHeight = 0f;
        btnAreaLE.minWidth = 0f;
        
        HorizontalLayoutGroup btnAreaHlg = btnArea.AddComponent<HorizontalLayoutGroup>();
        btnAreaHlg.spacing = 10;
        btnAreaHlg.childControlHeight = true;
        btnAreaHlg.childControlWidth = true;
        btnAreaHlg.childForceExpandHeight = true;
        btnAreaHlg.childForceExpandWidth = true;

        string[] btnNames = { "등록", "수정", "삭제" };
        string[] btnColors = { "#50B8B8", "#FFFFFF", "#FFFFFF" };
        string[] txtColors = { "#FFFFFF", "#3D5A6A", "#E53E3E" };

        Sprite mainBtnSprite = FindSpriteByName("주요버튼");
        GameObject btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02Monitoring/New UI/버튼프리팹.prefab");

        for (int i = 0; i < btnNames.Length; i++)
        {
            GameObject btnObj = null;
            if (btnPrefab != null)
            {
                btnObj = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab);
                btnObj.name = "Btn_" + btnNames[i];
                btnObj.transform.SetParent(btnArea.transform, false);
            }
            else
            {
                btnObj = new GameObject("Btn_" + btnNames[i]);
                btnObj.transform.SetParent(btnArea.transform, false);
                HorizontalLayoutGroup btnLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
                btnLayout.childAlignment = TextAnchor.MiddleCenter;
            }
            
            LayoutElement btnLe = btnObj.GetComponent<LayoutElement>();
            if (btnLe == null) btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.minHeight = 0f;
            btnLe.minWidth = 0f;

            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            
            if (btnNames[i] == "등록")
            {
                img.sprite = mainBtnSprite != null ? mainBtnSprite : basicSprite;
                img.color = Color.white;
                img.pixelsPerUnitMultiplier = 2f;
            }
            else
            {
                img.sprite = basicSprite;
                ColorUtility.TryParseHtmlString(btnColors[i], out Color bgCol);
                img.color = bgCol;
            }

            if (btnColors[i] == "#FFFFFF")
            {
                Outline btnOutline = btnObj.GetComponent<Outline>();
                if (btnOutline == null) btnOutline = btnObj.AddComponent<Outline>();
                btnOutline.effectColor = borderCol;
                btnOutline.effectDistance = new Vector2(0, -2f);
            }

            TextMeshProUGUI bTxt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (bTxt == null)
            {
                GameObject btnTxtGo = new GameObject("Text");
                btnTxtGo.transform.SetParent(btnObj.transform, false);
                bTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
                bTxt.alignment = TextAlignmentOptions.Center;
            }
            
            bTxt.text = btnNames[i];
            if (font != null) bTxt.font = font;
            bTxt.fontSize = 12;
            ColorUtility.TryParseHtmlString(txtColors[i], out Color tCol);
            bTxt.color = tCol;
        }
    }

    private static GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = new GameObject(name);
        page.transform.SetParent(parent, false);
        RectTransform rt = page.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RectTransform viewport = parent.parent.GetComponent<RectTransform>();
        if (viewport != null)
        {
            LayoutElement le = page.AddComponent<LayoutElement>();
            le.preferredHeight = viewport.rect.height;
            le.flexibleHeight = 1f;
        }

        VerticalLayoutGroup vlg = page.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.spacing = 15;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        
        page.SetActive(false);

        return page;
    }

    private static void CreateKPIRow(GameObject page, string[] titles, float[] flexWidths, float flexHeight, Sprite panelSprite, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject row = new GameObject("Row_KPI");
        row.transform.SetParent(page.transform, false);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.flexibleHeight = flexHeight;
        rowLE.minHeight = 0f;

        for (int i = 0; i < titles.Length; i++)
        {
            GameObject card = new GameObject("KPICard_" + titles[i]);
            card.transform.SetParent(row.transform, false);

            Image img = card.AddComponent<Image>();
            img.color = cWhite;
            if (panelSprite != null)
            {
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2f;
            }

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = cBorder;
            outline.effectDistance = new Vector2(0, -2f);

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 10;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = true;

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWidths[i];
            le.minHeight = 0f;
            le.minWidth = 0f;

            // 1. Title Row
            GameObject titleRow = new GameObject("1_TitleRow");
            titleRow.transform.SetParent(card.transform, false);
            TextMeshProUGUI titleTxt = titleRow.AddComponent<TextMeshProUGUI>();
            titleTxt.text = titles[i];
            if (font != null) titleTxt.font = font;
            titleTxt.fontSize = 15;
            ColorUtility.TryParseHtmlString("#7A9EAB", out Color tColor);
            titleTxt.color = tColor;
            titleTxt.alignment = TextAlignmentOptions.Left;

            // 2. Value + Unit Row
            GameObject valRow = new GameObject("2_ValueUnitRow");
            valRow.transform.SetParent(card.transform, false);
            HorizontalLayoutGroup valHlg = valRow.AddComponent<HorizontalLayoutGroup>();
            valHlg.childControlHeight = true;
            valHlg.childControlWidth = true;
            valHlg.childForceExpandHeight = false;
            valHlg.childForceExpandWidth = false;
            valHlg.spacing = 6;
            valHlg.childAlignment = TextAnchor.LowerLeft;

            GameObject valObj = new GameObject("Value");
            valObj.transform.SetParent(valRow.transform, false);
            TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
            valTxt.text = "4.5";
            if (font != null) valTxt.font = font;
            valTxt.fontSize = 38;
            valTxt.fontStyle = FontStyles.Bold;
            valTxt.color = cText;

            GameObject unitObj = new GameObject("Unit");
            unitObj.transform.SetParent(valRow.transform, false);
            TextMeshProUGUI unitTxt = unitObj.AddComponent<TextMeshProUGUI>();
            unitTxt.text = "kW";
            if (font != null) unitTxt.font = font;
            unitTxt.fontSize = 18;
            unitTxt.fontStyle = FontStyles.Bold;
            unitTxt.color = tColor;

            // Empty space to push progress down if needed, but spacing=10 is fine
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(card.transform, false);
            LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1f;

            // 3. SubText + Indicator Row
            GameObject subRow = new GameObject("3_SubTextRow");
            subRow.transform.SetParent(card.transform, false);
            HorizontalLayoutGroup subHlg = subRow.AddComponent<HorizontalLayoutGroup>();
            subHlg.childControlHeight = true;
            subHlg.childControlWidth = true;
            subHlg.childForceExpandHeight = false;
            subHlg.childForceExpandWidth = false;
            subHlg.childAlignment = TextAnchor.MiddleLeft;

            GameObject subTxtObj = new GameObject("SubText");
            subTxtObj.transform.SetParent(subRow.transform, false);
            TextMeshProUGUI subText = subTxtObj.AddComponent<TextMeshProUGUI>();
            subText.text = "전일 평균: 4.3 kW";
            if (font != null) subText.font = font;
            subText.fontSize = 13;
            ColorUtility.TryParseHtmlString("#A0AAB4", out Color sColor);
            subText.color = sColor;
            LayoutElement subLE = subTxtObj.AddComponent<LayoutElement>();
            subLE.flexibleWidth = 1f;

            GameObject indObj = new GameObject("Indicator");
            indObj.transform.SetParent(subRow.transform, false);
            Image indImg = indObj.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#FEE2E2", out Color indBg);
            indImg.color = indBg;
            HorizontalLayoutGroup indHlg = indObj.AddComponent<HorizontalLayoutGroup>();
            indHlg.padding = new RectOffset(6, 6, 2, 2);
            indHlg.childAlignment = TextAnchor.MiddleCenter;

            GameObject indTxtObj = new GameObject("Text");
            indTxtObj.transform.SetParent(indObj.transform, false);
            TextMeshProUGUI indTxt = indTxtObj.AddComponent<TextMeshProUGUI>();
            indTxt.text = "▲ 4.7%";
            if (font != null) indTxt.font = font;
            indTxt.fontSize = 12;
            indTxt.fontStyle = FontStyles.Bold;
            ColorUtility.TryParseHtmlString("#DC2626", out Color indTCol);
            indTxt.color = indTCol;

            // 4. Progress Bar Row
            GameObject progRow = new GameObject("4_ProgressBar");
            progRow.transform.SetParent(card.transform, false);
            RectTransform progRt = progRow.GetComponent<RectTransform>();
            if (progRt != null) progRt.sizeDelta = new Vector2(progRt.sizeDelta.x, 4f);
            
            LayoutElement progLE = progRow.AddComponent<LayoutElement>();
            progLE.preferredHeight = 4f;
            Image progBg = progRow.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#F1F5F9", out Color pBgCol);
            progBg.color = pBgCol;

            GameObject progFill = new GameObject("Fill");
            progFill.transform.SetParent(progRow.transform, false);
            Image fillImg = progFill.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#F59E0B", out Color fillCol);
            fillImg.color = fillCol;
            RectTransform fillRt = progFill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0.7f, 1f); // 70% 채워진 상태
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }
    }

    private static void CreateRow(GameObject page, string[] cards, float[] flexWidths, float flexHeight, Sprite panelSprite, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(page.transform, false);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.flexibleHeight = flexHeight;

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject card = new GameObject("Card_" + cards[i]);
            card.transform.SetParent(row.transform, false);

            Image img = card.AddComponent<Image>();
            img.color = cWhite;
            if (panelSprite != null)
            {
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
            }

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = cBorder;
            outline.effectDistance = new Vector2(0, -2f);

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 20, 20);
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlHeight = true;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = true;

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWidths[i];
            le.minHeight = 0f;
            le.minWidth = 0f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(card.transform, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();

            TextMeshProUGUI txt = textGO.AddComponent<TextMeshProUGUI>();
            txt.text = cards[i];
            txt.color = cText;
            if (font != null) txt.font = font;
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.fontSize = 18;
            txt.enableAutoSizing = false;
        }
    }

    [MenuItem("Tools/Fix Sub-Tab Navigation (Smart)")]
    public static void FixSubTabNavigation()
    {
        string[] pageNames = { "Page_EquipmentHealth", "Page_AlarmHistory", "Page_Maintenance", "Page_RemoteInspection" };
        string[] tabTexts = { "건전", "알람", "유지보수", "원격" };
        
        GameObject[] foundPages = new GameObject[4];
        MenuButton[] foundButtons = new MenuButton[4];

        // 1. Find Pages and Add Scripts
        Transform[] allT = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allT)
        {
            for (int i = 0; i < pageNames.Length; i++)
            {
                if (t.name.IndexOf(pageNames[i], System.StringComparison.OrdinalIgnoreCase) >= 0 && t.gameObject.scene.isLoaded)
                {
                    foundPages[i] = t.gameObject;
                    
                    if (t.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(t.gameObject);
                    if (t.GetComponent<PageFadeUp>() == null) Undo.AddComponent<PageFadeUp>(t.gameObject);
                }
            }
        }

        // 2. Find Sub-tab Buttons by Text (TMP)
        TextMeshProUGUI[] allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI txt in allTexts)
        {
            if (!txt.gameObject.scene.isLoaded) continue;

            for (int i = 0; i < tabTexts.Length; i++)
            {
                if (txt.text.Replace(" ", "").Contains(tabTexts[i].Replace(" ", "")))
                {
                    Button btn = txt.GetComponentInParent<Button>();
                    if (btn != null)
                    {
                        MenuButton mb = btn.GetComponent<MenuButton>();
                        if (mb == null)
                        {
                            mb = Undo.AddComponent<MenuButton>(btn.gameObject);
                            mb.backgroundImage = btn.GetComponent<Image>();
                            mb.normalIconColor = Color.gray;
                            mb.selectedIconColor = Color.white;
                            mb.hoverOffsetY = 0f;
                        }
                        foundButtons[i] = mb;
                    }
                }
            }
        }

        // 2.5 Find Sub-tab Buttons by Text (Legacy UI Text)
        Text[] legacyTexts = Resources.FindObjectsOfTypeAll<Text>();
        foreach (Text txt in legacyTexts)
        {
            if (!txt.gameObject.scene.isLoaded) continue;

            for (int i = 0; i < tabTexts.Length; i++)
            {
                if (txt.text.Replace(" ", "").Contains(tabTexts[i].Replace(" ", "")))
                {
                    Button btn = txt.GetComponentInParent<Button>();
                    if (btn != null)
                    {
                        MenuButton mb = btn.GetComponent<MenuButton>();
                        if (mb == null)
                        {
                            mb = Undo.AddComponent<MenuButton>(btn.gameObject);
                            mb.backgroundImage = btn.GetComponent<Image>();
                            mb.normalIconColor = Color.gray;
                            mb.selectedIconColor = Color.white;
                        }
                        foundButtons[i] = mb;
                    }
                }
            }
        }

        // 3. Create a dedicated MenuButtonGroup
        GameObject navGroupObj = GameObject.Find("SubTabNavigationGroup");
        if (navGroupObj == null)
        {
            navGroupObj = new GameObject("SubTabNavigationGroup");
            Undo.RegisterCreatedObjectUndo(navGroupObj, "Create Nav Group");
        }
        
        MenuButtonGroup group = navGroupObj.GetComponent<MenuButtonGroup>();
        if (group == null) group = Undo.AddComponent<MenuButtonGroup>(navGroupObj);

        group.pages = foundPages;
        group.buttons = foundButtons;
        group.sidebarContents = new GameObject[0];

        // Assign group to buttons
        foreach (var mb in foundButtons)
        {
            if (mb != null) mb.group = group;
        }

        for (int i = 0; i < 4; i++)
        {
            if (foundPages[i] == null) Debug.LogWarning($"[오류] {pageNames[i]} 페이지를 씬에서 찾을 수 없습니다.");
            if (foundButtons[i] == null) Debug.LogWarning($"[오류] '{tabTexts[i]}' 글자가 포함된 서브탭 버튼을 찾을 수 없습니다.");
        }

        // 4. Clean up any broken MenuButtonGroups in the scene (fixes the user's main sidebar crashing)
        MenuButtonGroup[] allGroups = Resources.FindObjectsOfTypeAll<MenuButtonGroup>();
        foreach (var g in allGroups)
        {
            if (g != null && g.gameObject.scene.isLoaded)
            {
                if (g.pages != null)
                {
                    System.Collections.Generic.List<GameObject> validPages = new System.Collections.Generic.List<GameObject>();
                    foreach (var p in g.pages)
                    {
                        if (p != null) validPages.Add(p);
                    }
                    if (validPages.Count != g.pages.Length)
                    {
                        g.pages = validPages.ToArray();
                        EditorUtility.SetDirty(g);
                    }
                }
            }
        }

        Debug.Log("서브 탭 네비게이션 복구 완료! 빈 페이지로 인한 에러도 모두 제거되었습니다.");
    }

    [MenuItem("Tools/Prepare Manual Sub-Tab Navigation")]
    public static void PrepareManualNavigation()
    {
        string[] pageNames = { "Page_EquipmentHealth", "Page_AlarmHistory", "Page_Maintenance", "Page_RemoteInspection" };
        GameObject[] foundPages = new GameObject[4];

        Transform[] allT = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allT)
        {
            for (int i = 0; i < pageNames.Length; i++)
            {
                if (t.name.IndexOf(pageNames[i], System.StringComparison.OrdinalIgnoreCase) >= 0 && t.gameObject.scene.isLoaded)
                {
                    foundPages[i] = t.gameObject;
                    if (t.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(t.gameObject);
                    if (t.GetComponent<PageFadeUp>() == null) Undo.AddComponent<PageFadeUp>(t.gameObject);
                }
            }
        }

        GameObject navGroupObj = GameObject.Find("SubTabNavigationGroup");
        if (navGroupObj == null)
        {
            navGroupObj = new GameObject("SubTabNavigationGroup");
            Undo.RegisterCreatedObjectUndo(navGroupObj, "Create Nav Group");
        }
        
        MenuButtonGroup group = navGroupObj.GetComponent<MenuButtonGroup>();
        if (group == null) group = Undo.AddComponent<MenuButtonGroup>(navGroupObj);

        group.pages = foundPages;
        group.buttons = new MenuButton[4]; // Leave empty for the user to assign
        group.sidebarContents = new GameObject[0];

        Selection.activeGameObject = navGroupObj;
        Debug.Log("SubTabNavigationGroup이 생성되고 페이지가 연결되었습니다. 인스펙터에서 버튼 4개를 직접 할당해주세요!");
    }

    [MenuItem("GameObject/UI/Convert to MenuButton (For Sub-Tabs)", false, 10)]
    public static void ConvertToMenuButton(MenuCommand menuCommand)
    {
        GameObject go = menuCommand.context as GameObject;
        if (go == null) return;

        Button oldBtn = go.GetComponent<Button>();
        if (oldBtn != null) Undo.DestroyObjectImmediate(oldBtn);

        MenuButton mb = go.GetComponent<MenuButton>();
        if (mb == null) mb = Undo.AddComponent<MenuButton>(go);

        mb.backgroundImage = go.GetComponent<Image>();
        mb.normalIconColor = Color.gray;
        mb.selectedIconColor = Color.white;
        mb.hoverOffsetY = 0f;

        GameObject navGroupObj = GameObject.Find("SubTabNavigationGroup");
        if (navGroupObj != null)
        {
            mb.group = navGroupObj.GetComponent<MenuButtonGroup>();
        }

        EditorUtility.SetDirty(go);
        Debug.Log($"{go.name} 오브젝트가 MenuButton으로 변환되었습니다. SubTabNavigationGroup의 Buttons 배열에 드래그 앤 드롭 하세요!");
    }

    [MenuItem("Tools/Disable All MenuButton Hovers (Safe)")]
    public static void DisableAllMenuButtonHovers()
    {
        MenuButton[] allButtons = Resources.FindObjectsOfTypeAll<MenuButton>();
        int count = 0;
        foreach (var btn in allButtons)
        {
            if (btn != null && btn.gameObject.scene.isLoaded)
            {
                Undo.RecordObject(btn, "Disable Hover Anim");
                btn.hoverOffsetY = 0f;
                EditorUtility.SetDirty(btn);
                count++;
            }
        }
        Debug.Log($"총 {count}개의 버튼에서 호버 애니메이션(위아래 움직임)을 완전히 제거했습니다!");
    }

    [MenuItem("Tools/Recreate Equipment Health Page (Safe)")]
    public static void RecreateEquipmentHealthPage()
    {
        Transform contentTransform = null;
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name.Equals("content", System.StringComparison.OrdinalIgnoreCase) && t.gameObject.scene.isLoaded)
            {
                if (t.parent != null && t.parent.name.IndexOf("viewport", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    contentTransform = t;
                    break;
                }
            }
        }

        if (contentTransform == null)
        {
            Debug.LogError("씬에서 'Viewport/Content'를 찾지 못했습니다.");
            return;
        }

        // 기존 페이지가 있다면 삭제
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            if (contentTransform.GetChild(i).name == "Page_EquipmentHealth")
            {
                Undo.DestroyObjectImmediate(contentTransform.GetChild(i).gameObject);
            }
        }

        Sprite basicSprite = FindSpriteByName("기본패널");
        if (basicSprite == null) basicSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        
        TMP_FontAsset fontAsset = FindFontAsset();
        Color colorWhite = Color.white;
        ColorUtility.TryParseHtmlString("#1A202D", out Color colorText);
        ColorUtility.TryParseHtmlString("#E2E8F0", out Color colorBorder);

        GameObject page = CreatePage(contentTransform, "Page_EquipmentHealth");
        CreatePageHeader(page, "설비 건전성 현황", "설비별 건전성 지표 확인 및 이상 감지 현황 | 마지막 갱신: 실시간", null, fontAsset);
        CreateKPIRow(page, new[] { "플랜트 종합 건전성", "정상 설비", "주의 설비", "위험 설비" }, new[] { 1f, 1f, 1f, 1f }, 1f, basicSprite, fontAsset, colorWhite, colorText, colorBorder);

        // 복합패널 프리팹 적용 로직
        GameObject compPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02Monitoring/New UI/복합패널.prefab");
        
        CreateRowWithPrefab(page, new[] { "설비 건전성 현황", "이상 감지 타임라인" }, new[] { 2f, 1f }, 3.5f, compPrefab, fontAsset, colorWhite, colorText, colorBorder);
        CreateRowWithPrefab(page, new[] { "설비별 실시간 센서 그래프" }, new[] { 1f }, 2.5f, compPrefab, fontAsset, colorWhite, colorText, colorBorder);

        page.SetActive(true);
        Undo.RegisterCreatedObjectUndo(page, "Recreate Equipment Health Page");
        Selection.activeGameObject = page;
        
        // 자동으로 내부 컨텐츠 채우기 (테이블, 타임라인 등)
        PopulateEquipmentHealthList();
        PopulateAnomalyTimeline();

        Debug.Log("설비 건전성 현황 페이지가 '복합패널' 프리팹을 사용하여 복구되고 테이블이 채워졌습니다!");
    }

    private static void CreateRowWithPrefab(GameObject page, string[] cards, float[] flexWidths, float flexHeight, GameObject prefab, TMP_FontAsset font, Color cWhite, Color cText, Color cBorder)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(page.transform, false);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.flexibleHeight = flexHeight;

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject card = null;
            if (prefab != null)
            {
                card = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                card.name = "Card_" + cards[i];
                card.transform.SetParent(row.transform, false);
            }
            else
            {
                card = new GameObject("Card_" + cards[i]);
                card.transform.SetParent(row.transform, false);
                Image img = card.AddComponent<Image>();
                img.color = cWhite;
                Outline outline = card.AddComponent<Outline>();
                outline.effectColor = cBorder;
                outline.effectDistance = new Vector2(0, -2f);
            }

            LayoutElement le = card.GetComponent<LayoutElement>();
            if (le == null) le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWidths[i];
            le.minHeight = 0f;
            le.minWidth = 0f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(card.transform, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();

            TextMeshProUGUI txt = textGO.AddComponent<TextMeshProUGUI>();
            txt.text = cards[i];
            txt.color = cText;
            if (font != null) txt.font = font;
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.fontSize = 18;
            txt.enableAutoSizing = false;
        }
    }

    [MenuItem("Tools/Insert Equipment Health Table Only")]
    public static void InsertEquipmentHealthTableOnly()
    {
        // 1. Find Content
        Transform cardTransform = null;
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == "Card_설비 건전성 현황" && t.gameObject.scene.isLoaded)
            {
                cardTransform = t;
                break;
            }
        }

        if (cardTransform == null)
        {
            Debug.LogError("'Card_설비 건전성 현황' 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Transform contentTransform = cardTransform.Find("ScrollView/Viewport/Content");
        if (contentTransform == null)
        {
            contentTransform = cardTransform.Find("Content"); // Fallback
            if (contentTransform == null)
            {
                Debug.LogError("'Card_설비 건전성 현황' 아래에 'ScrollView/Viewport/Content' 또는 'Content' 오브젝트가 없습니다.");
                return;
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(contentTransform.gameObject, "Insert Table Items");

        // 2. Clear existing items
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(contentTransform.GetChild(i).gameObject);
        }

        // Configure LayoutGroup if missing
        VerticalLayoutGroup vlg = contentTransform.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = Undo.AddComponent<VerticalLayoutGroup>(contentTransform.gameObject);
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentTransform.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = Undo.AddComponent<ContentSizeFitter>(contentTransform.gameObject);
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 3. Setup Colors and Sprites
        TMP_FontAsset font = FindFontAsset();
        Sprite basicSprite = FindSpriteByName("기본패널");
        if (basicSprite == null) basicSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        ColorUtility.TryParseHtmlString("#1A202D", out Color darkCol);
        ColorUtility.TryParseHtmlString("#7A9EAB", out Color subCol);
        ColorUtility.TryParseHtmlString("#E2E8F0", out Color borderCol);
        ColorUtility.TryParseHtmlString("#F1F5F9", out Color barBgCol);

        Color[] statusColors = new Color[3];
        ColorUtility.TryParseHtmlString("#50B8B8", out statusColors[0]); // Green/Mint
        ColorUtility.TryParseHtmlString("#E53E3E", out statusColors[1]); // Red
        ColorUtility.TryParseHtmlString("#DD6B20", out statusColors[2]); // Orange

        Color[] haloColors = new Color[3];
        ColorUtility.TryParseHtmlString("#E6F6F6", out haloColors[0]);
        ColorUtility.TryParseHtmlString("#FEE2E2", out haloColors[1]);
        ColorUtility.TryParseHtmlString("#FEEBC8", out haloColors[2]);

        string[] eqNames = { "취수펌프 (P-101)", "고압펌프 베어링 (P-202)", "RO 멤브레인 (RO-301)", "전처리 DMF (F-101)", "에너지 회수장치 (ERD-01)", "약품주입 펌프 (CP-01)" };
        string[] eqDescs = { "정상 운전 · 진동 0.8mm/s", "진동 이상 감지 · 4.2mm/s (임계: 3.5)", "차압 상승 · 세정 D-3 권장", "정상 · 차압 0.4bar", "정상 · 효율 94%", "정상 · 유량 안정" };
        int[] eqTypes = { 0, 1, 2, 0, 0, 0 }; // 0: Green, 1: Red, 2: Orange
        int[] eqScores = { 96, 42, 68, 91, 94, 98 };

        // 4. Generate Items
        for (int i = 0; i < eqNames.Length; i++)
        {
            GameObject listItem = new GameObject("Item_" + eqNames[i]);
            listItem.transform.SetParent(contentTransform, false);

            Image itemBg = listItem.AddComponent<Image>();
            itemBg.sprite = basicSprite;
            itemBg.type = Image.Type.Sliced;
            itemBg.color = Color.white;
            itemBg.pixelsPerUnitMultiplier = 2f;

            Outline itemOut = listItem.AddComponent<Outline>();
            itemOut.effectColor = borderCol;
            itemOut.effectDistance = new Vector2(0, -1f);

            HorizontalLayoutGroup itemHlg = listItem.AddComponent<HorizontalLayoutGroup>();
            itemHlg.padding = new RectOffset(20, 20, 20, 20);
            itemHlg.spacing = 15;
            itemHlg.childAlignment = TextAnchor.MiddleLeft;
            itemHlg.childControlHeight = true;
            itemHlg.childControlWidth = true;
            itemHlg.childForceExpandHeight = false;
            itemHlg.childForceExpandWidth = false;

            // Status Dot
            GameObject dotHalo = new GameObject("DotHalo");
            dotHalo.transform.SetParent(listItem.transform, false);
            Image haloImg = dotHalo.AddComponent<Image>();
            haloImg.sprite = knobSprite;
            haloImg.color = haloColors[eqTypes[i]];
            LayoutElement haloLe = dotHalo.AddComponent<LayoutElement>();
            haloLe.preferredWidth = 20f;
            haloLe.preferredHeight = 20f;
            haloLe.minWidth = 20f;
            haloLe.minHeight = 20f;
            haloLe.flexibleWidth = 0f;

            GameObject dotCore = new GameObject("DotCore");
            dotCore.transform.SetParent(dotHalo.transform, false);
            Image coreImg = dotCore.AddComponent<Image>();
            coreImg.sprite = knobSprite;
            coreImg.color = statusColors[eqTypes[i]];
            RectTransform coreRt = dotCore.GetComponent<RectTransform>();
            coreRt.anchorMin = new Vector2(0.5f, 0.5f);
            coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.pivot = new Vector2(0.5f, 0.5f);
            coreRt.sizeDelta = new Vector2(10f, 10f);

            // Text Area
            GameObject textVArea = new GameObject("TextVArea");
            textVArea.transform.SetParent(listItem.transform, false);
            VerticalLayoutGroup textVlg = textVArea.AddComponent<VerticalLayoutGroup>();
            textVlg.spacing = 2;
            textVlg.childAlignment = TextAnchor.MiddleLeft;
            textVlg.childControlHeight = true;
            textVlg.childControlWidth = true;
            textVlg.childForceExpandHeight = false;
            textVlg.childForceExpandWidth = true;
            LayoutElement textLe = textVArea.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f; // Takes up remaining middle space

            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = eqNames[i];
            if (font != null) nameTxt.font = font;
            nameTxt.fontSize = 15;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color = darkCol;

            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(textVArea.transform, false);
            TextMeshProUGUI descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = eqDescs[i];
            if (font != null) descTxt.font = font;
            descTxt.fontSize = 13;
            descTxt.color = (eqTypes[i] == 1) ? statusColors[1] : ((eqTypes[i] == 2) ? statusColors[2] : subCol); // Colored text if warning/danger

            // Progress Bar Area
            GameObject barArea = new GameObject("BarArea");
            barArea.transform.SetParent(listItem.transform, false);
            LayoutElement barLe = barArea.AddComponent<LayoutElement>();
            barLe.preferredWidth = 120f;
            barLe.minWidth = 120f;
            barLe.flexibleWidth = 0f;
            barLe.preferredHeight = 6f;

            Image barBg = barArea.AddComponent<Image>();
            barBg.sprite = basicSprite;
            barBg.type = Image.Type.Sliced;
            barBg.color = barBgCol;
            barBg.pixelsPerUnitMultiplier = 3f;

            GameObject barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barArea.transform, false);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.sprite = basicSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = statusColors[eqTypes[i]];
            fillImg.pixelsPerUnitMultiplier = 3f;

            RectTransform fillRt = barFill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(eqScores[i] / 100f, 1f); // Fill amount
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Percentage Text
            GameObject pctObj = new GameObject("Pct");
            pctObj.transform.SetParent(listItem.transform, false);
            TextMeshProUGUI pctTxt = pctObj.AddComponent<TextMeshProUGUI>();
            pctTxt.text = eqScores[i] + "%";
            if (font != null) pctTxt.font = font;
            pctTxt.fontSize = 15;
            pctTxt.fontStyle = FontStyles.Bold;
            pctTxt.color = statusColors[eqTypes[i]]; // Colored percentage
            pctTxt.alignment = TextAlignmentOptions.Right;
            LayoutElement pctLe = pctObj.AddComponent<LayoutElement>();
            pctLe.preferredWidth = 40f;
            pctLe.minWidth = 40f;
            pctLe.flexibleWidth = 0f;
        }

        Selection.activeGameObject = contentTransform.gameObject;
        Debug.Log("사용자의 Content 영역 내부에 테이블 아이템들만 깔끔하게 추가되었습니다!");
    }

    private static void AddScrollbarToScrollRect(ScrollRect sr)
    {
        // 1. Viewport Padding
        Transform viewport = sr.transform.Find("Viewport");
        if (viewport != null)
        {
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            if (vpRt != null)
            {
                vpRt.offsetMax = new Vector2(-10f, 0f);
            }
        }

        // 2. Load sprites
        string barPath = "Assets/02Monitoring/New UI/스크롤바(3배확대)/스크롤바(왼쪽메뉴,콘텐츠영역 동일)/세로/스크롤바(왼쪽메뉴)-세로.png";
        string hoverPath = "Assets/02Monitoring/New UI/스크롤바(3배확대)/스크롤바(왼쪽메뉴,콘텐츠영역 동일)/세로/스크롤바(왼쪽메뉴-호버)-세로.png";
        Sprite barSprite = AssetDatabase.LoadAssetAtPath<Sprite>(barPath);
        Sprite hoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(hoverPath);

        // 3. Create Scrollbar
        GameObject scrollbarGo = new GameObject("Scrollbar Vertical");
        scrollbarGo.transform.SetParent(sr.transform, false);
        
        RectTransform sbRt = scrollbarGo.AddComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(1f, 0f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot = new Vector2(1f, 1f);
        sbRt.offsetMin = new Vector2(-4f, 0f);
        sbRt.offsetMax = new Vector2(0f, 0f);
        sbRt.sizeDelta = new Vector2(4f, 0f);
        sbRt.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image bgImg = scrollbarGo.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);
        bgImg.raycastTarget = false;

        Scrollbar scrollbarComp = scrollbarGo.AddComponent<Scrollbar>();
        scrollbarComp.direction = Scrollbar.Direction.BottomToTop;
        scrollbarComp.transition = Selectable.Transition.SpriteSwap;
        
        UnityEngine.UI.Navigation nav = new UnityEngine.UI.Navigation();
        nav.mode = UnityEngine.UI.Navigation.Mode.None;
        scrollbarComp.navigation = nav;

        GameObject slidingAreaGo = new GameObject("Sliding Area");
        slidingAreaGo.transform.SetParent(scrollbarGo.transform, false);
        RectTransform saRt = slidingAreaGo.AddComponent<RectTransform>();
        saRt.anchorMin = Vector2.zero;
        saRt.anchorMax = Vector2.one;
        saRt.offsetMin = Vector2.zero;
        saRt.offsetMax = Vector2.zero;
        saRt.sizeDelta = Vector2.zero;

        GameObject handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(slidingAreaGo.transform, false);
        RectTransform handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        handleRt.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image handleImg = handleGo.AddComponent<UnityEngine.UI.Image>();
        handleImg.sprite = barSprite;
        handleImg.color = Color.white;
        handleImg.type = UnityEngine.UI.Image.Type.Simple;
        handleImg.raycastTarget = true;

        scrollbarComp.handleRect = handleRt;
        scrollbarComp.targetGraphic = handleImg;
        
        SpriteState state = new SpriteState();
        state.highlightedSprite = hoverSprite;
        state.pressedSprite = hoverSprite;
        state.selectedSprite = hoverSprite;
        scrollbarComp.spriteState = state;

        sr.verticalScrollbar = scrollbarComp;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        sr.verticalScrollbarSpacing = 0f;
    }

    [MenuItem("Tools/Populate Device Management Panel (Safe)")]
    public static void PopulateDeviceManagementPanel()
    {
        GameObject contentGo = GameObject.Find("Canvas/Image/컨텐츠 영역/Viewport/Content/Page_RemoteInspection/Row/복합패널 (1)/ScrollView/Viewport/Content");
        if (contentGo == null)
        {
            Debug.LogError("Could not find the ScrollView Content of the Device Management Panel.");
            return;
        }

        // 1. Register full hierarchy undo
        Undo.RegisterFullObjectHierarchyUndo(contentGo, "Populate Device Management Panel");

        // 2. Clear existing items
        for (int i = contentGo.transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(contentGo.transform.GetChild(i).gameObject);
        }

        // 3. Load assets
        Sprite basicSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        
        TMP_FontAsset font = FindFontAsset();

        string eyeIconPath = "Assets/02Monitoring/New UI/아이콘(3배확대)/아이콘-기본/아이콘-뷰.png";
        Sprite eyeIcon = AssetDatabase.LoadAssetAtPath<Sprite>(eyeIconPath);
        if (eyeIcon == null)
        {
            eyeIcon = knobSprite; // fallback
        }

        // Set content padding and spacing
        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.spacing = 15;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        Color darkCol = HexToColor("#1A202D");
        Color subCol = HexToColor("#718096");

        // --- AR Glass Card ---
        Color arStatusBg = HexToColor("#E6F6F6");
        Color arStatusText = HexToColor("#2C7A7B");
        Color arIconBg = HexToColor("#E6F6F6");
        Color arIconTint = HexToColor("#319795");

        CreateDeviceCard(
            contentGo.transform, 
            "AR 글래스", 
            "Meta Lens 2", 
            "연결됨", 
            arStatusBg, 
            arStatusText, 
            arIconBg, 
            arIconTint, 
            eyeIcon, 
            basicSprite, 
            knobSprite, 
            font,
            (specsContainer, f, dColor, sColor) => {
                CreateSpecRow(specsContainer, "착용자", "김현장", f, sColor, dColor, null);
                
                CreateSpecRow(specsContainer, "배터리", "78%", f, sColor, dColor, (container, fComp) => {
                    BuildBatteryValue(container, fComp, 78f, HexToColor("#319795"), "78%");
                });

                CreateSpecRow(specsContainer, "펌웨어", "v2.1.4", f, sColor, dColor, null);
                
                CreateSpecRow(specsContainer, "현재 세션", "점검 중", f, sColor, dColor, (container, fComp) => {
                    GameObject valGo = new GameObject("Value");
                    valGo.transform.SetParent(container.transform, false);
                    TextMeshProUGUI valTxt = valGo.AddComponent<TextMeshProUGUI>();
                    valTxt.text = "점검 중";
                    if (fComp != null) valTxt.font = fComp;
                    valTxt.fontSize = 13;
                    valTxt.color = HexToColor("#B7791F"); // golden brown
                    valTxt.fontStyle = FontStyles.Bold;
                    valTxt.alignment = TextAlignmentOptions.Right;
                });

                CreateSpecRow(specsContainer, "마지막 접속", "2026.06.23 11:35", f, sColor, dColor, null);
            }
        );

        // --- MR Glass Card ---
        Color mrStatusBg = HexToColor("#EDF2F7");
        Color mrStatusText = HexToColor("#4A5568");
        Color mrIconBg = HexToColor("#EBF4FF");
        Color mrIconTint = HexToColor("#3182CE");

        CreateDeviceCard(
            contentGo.transform, 
            "MR 글래스", 
            "Meta Quest 3", 
            "대기 중", 
            mrStatusBg, 
            mrStatusText, 
            mrIconBg, 
            mrIconTint, 
            eyeIcon, 
            basicSprite, 
            knobSprite, 
            font,
            (specsContainer, f, dColor, sColor) => {
                CreateSpecRow(specsContainer, "착용자", "최원격", f, sColor, dColor, null);
                
                CreateSpecRow(specsContainer, "배터리", "45%", f, sColor, dColor, (container, fComp) => {
                    BuildBatteryValue(container, fComp, 45f, HexToColor("#DD6B20"), "45%");
                });

                CreateSpecRow(specsContainer, "펌웨어", "v2.0.9", f, sColor, dColor, (container, fComp) => {
                    BuildFirmwareValue(container, fComp, "v2.0.9", "업데이트 필요", HexToColor("#FEEBC8"), HexToColor("#C05621"), basicSprite);
                });
                
                CreateSpecRow(specsContainer, "현재 세션", "—", f, sColor, dColor, null);

                CreateSpecRow(specsContainer, "마지막 접속", "2026.06.22 17:39", f, sColor, dColor, null);
            }
        );

        // Update SubTitle of the Panel to something clean like "등록 기기: 2대"
        var panelHeaderTitle = GameObject.Find("Canvas/Image/컨텐츠 영역/Viewport/Content/Page_RemoteInspection/Row/복합패널 (1)/HeaderArea/TitleVArea/SubTitle")?.GetComponent<TextMeshProUGUI>();
        if (panelHeaderTitle != null)
        {
            panelHeaderTitle.text = "등록 기기: 2대";
        }

        Debug.Log("Successfully populated Device Management Panel with AR Glasses and MR Glasses cards!");
    }

    private static Color HexToColor(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    private static GameObject CreateDeviceCard(
        Transform parent, 
        string deviceName, 
        string modelName, 
        string statusText, 
        Color statusBgColor, 
        Color statusTextColor,
        Color iconBgColor,
        Color iconTintColor,
        Sprite iconSprite,
        Sprite basicSprite,
        Sprite knobSprite,
        TMP_FontAsset font,
        System.Action<Transform, TMP_FontAsset, Color, Color> addSpecsCallback)
    {
        Color darkCol = HexToColor("#1A202D");
        Color subCol = HexToColor("#718096");
        Color borderCol = HexToColor("#E2E8F0");

        // Card Container
        GameObject cardGo = new GameObject("DeviceCard_" + deviceName);
        cardGo.transform.SetParent(parent, false);

        UnityEngine.UI.Image cardImg = cardGo.AddComponent<UnityEngine.UI.Image>();
        cardImg.sprite = basicSprite;
        cardImg.type = UnityEngine.UI.Image.Type.Sliced;
        cardImg.color = Color.white;
        cardImg.pixelsPerUnitMultiplier = 2f;

        Outline cardOut = cardGo.AddComponent<Outline>();
        cardOut.effectColor = borderCol;
        cardOut.effectDistance = new Vector2(0, -1f);

        VerticalLayoutGroup cardVlg = cardGo.AddComponent<VerticalLayoutGroup>();
        cardVlg.padding = new RectOffset(20, 20, 18, 18);
        cardVlg.spacing = 15;
        cardVlg.childControlWidth = true;
        cardVlg.childControlHeight = true;
        cardVlg.childForceExpandWidth = true;
        cardVlg.childForceExpandHeight = false;

        LayoutElement cardLe = cardGo.AddComponent<LayoutElement>();
        cardLe.flexibleHeight = 0f;

        // --- Header Row ---
        GameObject headerRow = new GameObject("HeaderRow");
        headerRow.transform.SetParent(cardGo.transform, false);
        HorizontalLayoutGroup hlgHeader = headerRow.AddComponent<HorizontalLayoutGroup>();
        hlgHeader.spacing = 12;
        hlgHeader.childAlignment = TextAnchor.MiddleLeft;
        hlgHeader.childControlWidth = true;
        hlgHeader.childControlHeight = true;
        hlgHeader.childForceExpandWidth = false;
        hlgHeader.childForceExpandHeight = false;

        // Icon Area (Knob Circle)
        GameObject iconBgGo = new GameObject("IconBg");
        iconBgGo.transform.SetParent(headerRow.transform, false);
        UnityEngine.UI.Image bgImg = iconBgGo.AddComponent<UnityEngine.UI.Image>();
        bgImg.sprite = knobSprite;
        bgImg.color = iconBgColor;
        LayoutElement bgLe = iconBgGo.AddComponent<LayoutElement>();
        bgLe.preferredWidth = 40f;
        bgLe.preferredHeight = 40f;
        bgLe.minWidth = 40f;
        bgLe.minHeight = 40f;
        bgLe.flexibleWidth = 0f;
        bgLe.flexibleHeight = 0f;

        // Inner Icon
        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(iconBgGo.transform, false);
        UnityEngine.UI.Image iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
        iconImg.sprite = iconSprite;
        iconImg.color = iconTintColor;
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.25f, 0.25f);
        iconRt.anchorMax = new Vector2(0.75f, 0.75f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        // Title Area
        GameObject titleArea = new GameObject("TitleArea");
        titleArea.transform.SetParent(headerRow.transform, false);
        VerticalLayoutGroup vlgTitle = titleArea.AddComponent<VerticalLayoutGroup>();
        vlgTitle.spacing = 2;
        vlgTitle.childAlignment = TextAnchor.MiddleLeft;
        vlgTitle.childControlWidth = true;
        vlgTitle.childControlHeight = true;
        vlgTitle.childForceExpandWidth = true;
        vlgTitle.childForceExpandHeight = false;
        LayoutElement titleLe = titleArea.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;

        GameObject titleTextGo = new GameObject("DeviceName");
        titleTextGo.transform.SetParent(titleArea.transform, false);
        TextMeshProUGUI titleTxt = titleTextGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = deviceName;
        if (font != null) titleTxt.font = font;
        titleTxt.fontSize = 16;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = darkCol;

        GameObject modelTextGo = new GameObject("ModelName");
        modelTextGo.transform.SetParent(titleArea.transform, false);
        TextMeshProUGUI modelTxt = modelTextGo.AddComponent<TextMeshProUGUI>();
        modelTxt.text = modelName;
        if (font != null) modelTxt.font = font;
        modelTxt.fontSize = 12;
        modelTxt.color = subCol;

        // Status Tag
        GameObject tagGo = new GameObject("StatusTag");
        tagGo.transform.SetParent(headerRow.transform, false);
        UnityEngine.UI.Image tagImg = tagGo.AddComponent<UnityEngine.UI.Image>();
        tagImg.sprite = basicSprite;
        tagImg.type = UnityEngine.UI.Image.Type.Sliced;
        tagImg.color = statusBgColor;
        tagImg.pixelsPerUnitMultiplier = 2f;

        HorizontalLayoutGroup tagHlg = tagGo.AddComponent<HorizontalLayoutGroup>();
        tagHlg.padding = new RectOffset(10, 10, 5, 5);
        tagHlg.childAlignment = TextAnchor.MiddleCenter;
        tagHlg.childControlWidth = true;
        tagHlg.childControlHeight = true;
        tagHlg.childForceExpandWidth = false;
        tagHlg.childForceExpandHeight = false;

        GameObject tagTextGo = new GameObject("Text");
        tagTextGo.transform.SetParent(tagGo.transform, false);
        TextMeshProUGUI tagTxt = tagTextGo.AddComponent<TextMeshProUGUI>();
        tagTxt.text = statusText;
        if (font != null) tagTxt.font = font;
        tagTxt.fontSize = 12;
        tagTxt.fontStyle = FontStyles.Bold;
        tagTxt.color = statusTextColor;
        tagTxt.alignment = TextAlignmentOptions.Center;

        // --- Divider ---
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(cardGo.transform, false);
        UnityEngine.UI.Image divImg = divider.AddComponent<UnityEngine.UI.Image>();
        Color divCol = HexToColor("#EDF2F7");
        divImg.color = divCol;
        LayoutElement divLe = divider.AddComponent<LayoutElement>();
        divLe.preferredHeight = 1f;
        divLe.flexibleHeight = 0f;

        // --- Specs Container ---
        GameObject specsContainer = new GameObject("SpecsContainer");
        specsContainer.transform.SetParent(cardGo.transform, false);
        VerticalLayoutGroup specsVlg = specsContainer.AddComponent<VerticalLayoutGroup>();
        specsVlg.spacing = 10;
        specsVlg.childControlWidth = true;
        specsVlg.childControlHeight = true;
        specsVlg.childForceExpandWidth = true;
        specsVlg.childForceExpandHeight = false;

        addSpecsCallback(specsContainer.transform, font, darkCol, subCol);

        return cardGo;
    }

    private static GameObject CreateSpecRow(Transform parent, string label, string val, TMP_FontAsset font, Color labelColor, Color valColor, System.Action<GameObject, TMP_FontAsset> customValBuilder = null)
    {
        GameObject rowGo = new GameObject("SpecRow_" + label);
        rowGo.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        TextMeshProUGUI labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        if (font != null) labelTxt.font = font;
        labelTxt.fontSize = 13;
        labelTxt.color = labelColor;
        LayoutElement labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;

        GameObject valContainer = new GameObject("ValContainer");
        valContainer.transform.SetParent(rowGo.transform, false);
        HorizontalLayoutGroup valHlg = valContainer.AddComponent<HorizontalLayoutGroup>();
        valHlg.spacing = 8;
        valHlg.childAlignment = TextAnchor.MiddleRight;
        valHlg.childControlWidth = true;
        valHlg.childControlHeight = false;
        valHlg.childForceExpandWidth = false;
        valHlg.childForceExpandHeight = false;
        LayoutElement valLe = valContainer.AddComponent<LayoutElement>();
        valLe.flexibleWidth = 1f;

        if (customValBuilder != null)
        {
            customValBuilder(valContainer, font);
        }
        else
        {
            GameObject valGo = new GameObject("Value");
            valGo.transform.SetParent(valContainer.transform, false);
            TextMeshProUGUI valTxt = valGo.AddComponent<TextMeshProUGUI>();
            valTxt.text = val;
            if (font != null) valTxt.font = font;
            valTxt.fontSize = 13;
            valTxt.color = valColor;
            valTxt.alignment = TextAlignmentOptions.Right;
        }

        return rowGo;
    }

    private static void BuildBatteryValue(GameObject container, TMP_FontAsset font, float percentage, Color barFillColor, string valStr)
    {
        GameObject barBg = new GameObject("BatteryBarBg");
        barBg.transform.SetParent(container.transform, false);
        UnityEngine.UI.Image bgImg = barBg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = HexToColor("#E2E8F0");
        
        Sprite basicSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgImg.sprite = basicSprite;
        bgImg.type = UnityEngine.UI.Image.Type.Sliced;
        
        RectTransform bgRt = barBg.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(100f, 10f);
        
        LayoutElement barLe = barBg.AddComponent<LayoutElement>();
        barLe.preferredWidth = 100f;
        barLe.preferredHeight = 10f;
        barLe.minWidth = 100f;
        barLe.minHeight = 10f;
        barLe.flexibleWidth = 0f;
        barLe.flexibleHeight = 0f;

        GameObject barFill = new GameObject("Fill");
        barFill.transform.SetParent(barBg.transform, false);
        UnityEngine.UI.Image fillImg = barFill.AddComponent<UnityEngine.UI.Image>();
        fillImg.sprite = basicSprite;
        fillImg.type = UnityEngine.UI.Image.Type.Sliced;
        fillImg.color = barFillColor;
        RectTransform fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(percentage / 100f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        GameObject valGo = new GameObject("Value");
        valGo.transform.SetParent(container.transform, false);
        TextMeshProUGUI valTxt = valGo.AddComponent<TextMeshProUGUI>();
        valTxt.text = valStr;
        if (font != null) valTxt.font = font;
        valTxt.fontSize = 13;
        valTxt.color = HexToColor("#2D3748");
        valTxt.fontStyle = FontStyles.Bold;
        valTxt.alignment = TextAlignmentOptions.Right;
    }

    private static void BuildFirmwareValue(GameObject container, TMP_FontAsset font, string versionStr, string badgeStr, Color badgeBgC, Color badgeTextC, Sprite basicSprite)
    {
        GameObject valGo = new GameObject("Value");
        valGo.transform.SetParent(container.transform, false);
        TextMeshProUGUI valTxt = valGo.AddComponent<TextMeshProUGUI>();
        valTxt.text = versionStr;
        if (font != null) valTxt.font = font;
        valTxt.fontSize = 13;
        valTxt.color = HexToColor("#2D3748");
        valTxt.alignment = TextAlignmentOptions.Right;

        GameObject badgeGo = new GameObject("UpdateBadge");
        badgeGo.transform.SetParent(container.transform, false);
        UnityEngine.UI.Image badgeImg = badgeGo.AddComponent<UnityEngine.UI.Image>();
        badgeImg.sprite = basicSprite;
        badgeImg.type = UnityEngine.UI.Image.Type.Sliced;
        badgeImg.color = badgeBgC;
        badgeImg.pixelsPerUnitMultiplier = 2f;

        ContentSizeFitter csf = badgeGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        HorizontalLayoutGroup badgeHlg = badgeGo.AddComponent<HorizontalLayoutGroup>();
        badgeHlg.padding = new RectOffset(6, 6, 3, 3);
        badgeHlg.childAlignment = TextAnchor.MiddleCenter;
        badgeHlg.childControlWidth = true;
        badgeHlg.childControlHeight = true;
        badgeHlg.childForceExpandWidth = false;
        badgeHlg.childForceExpandHeight = false;

        GameObject badgeTextGo = new GameObject("Text");
        badgeTextGo.transform.SetParent(badgeGo.transform, false);
        TextMeshProUGUI badgeTxt = badgeTextGo.AddComponent<TextMeshProUGUI>();
        badgeTxt.text = badgeStr;
        if (font != null) badgeTxt.font = font;
        badgeTxt.fontSize = 10;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.color = badgeTextC;
        badgeTxt.alignment = TextAlignmentOptions.Center;
    }
}

