using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 화상통화 도크의 화면(Passive View). UI를 코드로 만들고, 표시용 메서드와 버튼 이벤트만 내놓는다.
// 언제 무엇을 보여줄지는 MrCallDockPanel(Presenter)이 정한다 - 여기에는 통화·서버·배치 로직이 없다.
// 위쪽에 화상통화 영상을 크게, 그 아래 작업가이드/파일공유·지시/3D모델/그리기 탭. 탭 내용은 다른 패널
// (WorkGuidePanel/ModelRotateTestPanel/LiveDrawOverlay)이 GuideContent/ModelContent/DrawContent 자리에 끼워 넣는다.
public class MrCallDockView
{
    public enum Tab { Guide, Share, Model, Draw }

    public const float PanelWidth = 900f;
    const float VideoHeight = 520f;
    const float HeaderHeight = 100f; // 2026-10-21 글씨 1.3배에 맞춰 80→100
    const float TabBarHeight = 76f;  // 64→76
    // 2026-08-24: 작업가이드 탭이 단계 목록(퀘스트 리스트 스타일)으로 바뀌면서 세로 공간이 더
    // 필요해져서 280→420으로 키움 (WorkGuidePanel.cs의 900x560 캔버스가 이 안에 임베드될 때
    // 글자가 너무 작게 축소되는 걸 방지 - SetEmbedParent의 균일 축소 배율 참고).
    const float ContentHeight = 420f;

    public const string DefaultConnectingText = "연결 대기 중";

    public event Action CloseClicked;
    public event Action<Tab> TabClicked;
    public event Action FileShareClicked;
    public event Action InstructionClicked;

    public GameObject Root { get; private set; }
    public Transform Transform => Root.transform;
    public bool IsVisible => Root.activeSelf;

    // 원격(AR) 영상 뷰가 만들어지는 자리 - DockAgoraSession이 여기에 붙이고, 지시하기 캡처도 여기서 한다.
    public RectTransform VideoContainer { get; private set; }
    public RectTransform GuideContent { get; private set; }
    public RectTransform ModelContent { get; private set; }
    public RectTransform DrawContent { get; private set; }

    // 파일공유(DropZone)와 지시하기(InstructionDrawing) 전송 결과가 같이 쓰는 검은 토스트
    public GameObject ShareAlert { get; private set; }
    public TMPro.TMP_Text ShareAlertText { get; private set; }

    GameObject connectingSpinner;
    Text connectingText;
    Text callTimeText, headerTitleText, headerStatusText;

    Image guideUnderline, shareUnderline, modelUnderline, drawUnderline;
    Text guideTabLabel, shareTabLabel, modelTabLabel, drawTabLabel;
    GameObject guideContentGo, shareContentGo, modelContentGo, drawContentGo;

    public MrCallDockView()
    {
        Build(MrUiTheme.CreateFont());
        SelectTab(Tab.Guide);
        Root.SetActive(false);
    }

    public void SetVisible(bool visible) => Root.SetActive(visible);

    public void SetHeader(string title, string status)
    {
        headerTitleText.text = title;
        headerStatusText.text = status;
    }

    public void SetCallTime(int seconds) => callTimeText.text = $"{seconds / 60:00}:{seconds % 60:00}";

    // 영상 영역의 "연결 대기 중" 문구 - 토큰 재시도/실패도 여기에 표시한다.
    public void SetConnectingText(string s) => connectingText.text = s;

    // 원격 영상이 들어오면 false(가림막 치움), 나가면 true
    public void SetWaitingForVideo(bool waiting) => connectingSpinner.SetActive(waiting);

    // 내가 먼저 나갈 땐 OnUserOffline이 안 와서 이전 영상 뷰가 남아있을 수 있다(재연결 시
    // MakeVideoView가 "이미 있다"고 착각해 예전 세션에 바인딩된 뷰를 재사용해버림 - 화면 안 뜸) -
    // JoinChannelVideoToken.OnDisable과 동일한 이유로 확실히 지운다.
    public void ClearRemoteVideo()
    {
        for (int i = VideoContainer.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(VideoContainer.GetChild(i).gameObject);
    }

    public void SelectTab(Tab tab)
    {
        guideContentGo.SetActive(tab == Tab.Guide);
        shareContentGo.SetActive(tab == Tab.Share);
        modelContentGo.SetActive(tab == Tab.Model);
        drawContentGo.SetActive(tab == Tab.Draw);

        guideUnderline.gameObject.SetActive(tab == Tab.Guide);
        shareUnderline.gameObject.SetActive(tab == Tab.Share);
        modelUnderline.gameObject.SetActive(tab == Tab.Model);
        drawUnderline.gameObject.SetActive(tab == Tab.Draw);

        guideTabLabel.color = tab == Tab.Guide ? MrUiTheme.Accent : MrUiTheme.InkDim;
        shareTabLabel.color = tab == Tab.Share ? MrUiTheme.Accent : MrUiTheme.InkDim;
        modelTabLabel.color = tab == Tab.Model ? MrUiTheme.Accent : MrUiTheme.InkDim;
        drawTabLabel.color = tab == Tab.Draw ? MrUiTheme.Accent : MrUiTheme.InkDim;
    }

    // ───────────────────────────── 화면 구성 ─────────────────────────────

    void Build(Font font)
    {
        float totalHeight = HeaderHeight + VideoHeight + TabBarHeight + ContentHeight;

        Root = new GameObject("MrCallDockCanvas");
        var canvas = Root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = Root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(PanelWidth, totalHeight);
        Root.AddComponent<GraphicRaycaster>();
        Root.AddComponent<TrackedDeviceGraphicRaycaster>();

        // 2026-08-26: "MRDockCanvas는 컨트롤러로 조정할 수 있어야 함, 메뉴패널처럼" - 씬의 그랩 가능한
        // "메뉴" 패널과 동일한 레시피: Kinematic Rigidbody(중력 없음) + 캔버스 크기에 맞춘 BoxCollider(트리거 아님)
        // + XRGrabInteractable(Kinematic, 놓을 때 던지기 없음).
        var dockCollider = Root.AddComponent<BoxCollider>();
        dockCollider.isTrigger = false;
        dockCollider.size = new Vector3(PanelWidth, totalHeight, 40f);

        var dockRb = Root.AddComponent<Rigidbody>();
        dockRb.useGravity = false;
        dockRb.isKinematic = true;

        var dockGrab = Root.AddComponent<XRGrabInteractable>();
        dockGrab.movementType = XRBaseInteractable.MovementType.Kinematic;
        dockGrab.throwOnDetach = false;

        var bg = Root.AddComponent<Image>();
        bg.color = MrUiTheme.Panel;
        MrUiTheme.Round(bg, 24);

        BuildHeader(font);
        var videoGo = BuildVideoArea(font);
        BuildDropZoneAndAlert(videoGo);
        BuildTabs(font);
    }

    void BuildHeader(Font font)
    {
        var headerRect = NewRect("Header", Root.transform);
        headerRect.anchorMin = new Vector2(0f, 1f); headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
        headerRect.anchoredPosition = Vector2.zero;

        headerTitleText = NewText("Title", headerRect, font, 36, MrUiTheme.Ink, TextAnchor.UpperLeft); // 2026-10-21 도크 글씨 1.3배
        headerTitleText.text = "작업 준비 중...";
        // 글씨를 키우면서 한 줄 높이가 칸(40px)보다 커져 제목이 통째로 안 그려졌다 - 넘쳐도 그리게 하고
        // 오른쪽 닫기 버튼(56px)과 겹치지 않게 비켜 둔다.
        headerTitleText.verticalOverflow = VerticalWrapMode.Overflow;
        headerTitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var titleRect = headerTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(30f, 0f); titleRect.offsetMax = new Vector2(-90f, -8f);

        headerStatusText = NewText("Status", headerRect, font, 26, MrUiTheme.Warn, TextAnchor.LowerLeft);
        headerStatusText.text = "";
        headerStatusText.verticalOverflow = VerticalWrapMode.Overflow;
        var statusRect = headerStatusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f); statusRect.anchorMax = new Vector2(1f, 0.5f);
        statusRect.offsetMin = new Vector2(30f, 6f); statusRect.offsetMax = new Vector2(-30f, 0f);

        // 닫기 버튼 - 헤더 맨 오른쪽 위, 누르면 통화 종료(요청: "닫기 누를시 통화 종료됨").
        var closeBg = NewRect("CloseButton", headerRect).gameObject.AddComponent<Image>();
        closeBg.color = MrUiTheme.Danger;
        MrUiTheme.Round(closeBg, 12);
        var closeRect = closeBg.rectTransform;
        closeRect.anchorMin = new Vector2(1f, 1f); closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(56f, 56f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        var closeBtn = closeBg.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(() => CloseClicked?.Invoke());

        var closeLabel = NewText("Label", closeRect, font, 30, Color.white, TextAnchor.MiddleCenter);
        closeLabel.text = "✕";
        closeLabel.raycastTarget = false;
        Stretch(closeLabel.rectTransform);
    }

    GameObject BuildVideoArea(Font font)
    {
        var videoRect = NewRect("VideoArea", Root.transform);
        videoRect.anchorMin = new Vector2(0f, 1f); videoRect.anchorMax = new Vector2(1f, 1f);
        videoRect.pivot = new Vector2(0.5f, 1f);
        videoRect.sizeDelta = new Vector2(0f, VideoHeight);
        videoRect.anchoredPosition = new Vector2(0f, -HeaderHeight);
        var videoBg = videoRect.gameObject.AddComponent<Image>();
        videoBg.color = new Color(0.08f, 0.09f, 0.1f);

        // 통화 경과시간 배지 - Presenter가 매 프레임 SetCallTime으로 채운다.
        var callTimeBg = NewRect("CallTimeBadge", videoRect).gameObject.AddComponent<Image>();
        callTimeBg.color = new Color(0f, 0f, 0f, 0.5f);
        MrUiTheme.Round(callTimeBg, 8);
        var callTimeBgRect = callTimeBg.rectTransform;
        callTimeBgRect.anchorMin = new Vector2(1f, 1f); callTimeBgRect.anchorMax = new Vector2(1f, 1f);
        callTimeBgRect.pivot = new Vector2(1f, 1f);
        callTimeBgRect.sizeDelta = new Vector2(110f, 40f);
        callTimeBgRect.anchoredPosition = new Vector2(-16f, -16f);
        callTimeText = NewText("Text", callTimeBgRect, font, 26, MrUiTheme.Danger, TextAnchor.MiddleCenter);
        callTimeText.text = "00:00";
        Stretch(callTimeText.rectTransform);

        // DockAgoraSession이 JoinChannelVideoToken.MakeVideoView(...)로 이 자식 밑에 실제 원격(AR) 영상 뷰를 만든다.
        VideoContainer = NewRect("VideoContainer", videoRect);
        Stretch(VideoContainer);

        var spinnerRect = NewRect("ConnectingSpinner", videoRect);
        Stretch(spinnerRect);
        spinnerRect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        connectingText = NewText("Text", spinnerRect, font, 31, MrUiTheme.InkDim, TextAnchor.MiddleCenter);
        connectingText.text = DefaultConnectingText;
        Stretch(connectingText.rectTransform);
        connectingSpinner = spinnerRect.gameObject;

        return videoRect.gameObject;
    }

    void BuildDropZoneAndAlert(GameObject videoGo)
    {
        // "DropZone.cs도 이 진행 패널에 붙여야 해" - 정보공유 아이콘을 화상통화 영상 위로
        // 드래그하면 AR로 전송된다(DropZone.cs 자체 코멘트: "화상통화 영상 영역에 붙는 스크립트").
        // videoBg에 바로 붙이지 않는다 - DropZone.Start()가 dropZoneImage.color를 자기
        // normalColor(투명)로 덮어써서, 그대로 붙이면 영상 배경의 어두운 색이 사라져버린다.
        // 별도의 투명 오버레이를 맨 위(마지막 형제)에 얹어야 다른 자식(스피너/원격영상)에
        // 드롭이 가로채이지 않고 이 오버레이가 받는다.
        var dropOverlayRect = NewRect("DropZoneOverlay", videoGo.transform);
        Stretch(dropOverlayRect);
        var dropOverlayImg = dropOverlayRect.gameObject.AddComponent<Image>();
        var dropZone = dropOverlayRect.gameObject.AddComponent<DropZone>();
        dropZone.dropZoneImage = dropOverlayImg;

        var shareAlertBg = NewRect("ShareAlert", videoGo.transform).gameObject.AddComponent<Image>();
        shareAlertBg.color = new Color(0f, 0f, 0f, 0.75f);
        MrUiTheme.Round(shareAlertBg, 10);
        var shareAlertRect = shareAlertBg.rectTransform;
        shareAlertRect.anchorMin = new Vector2(0.5f, 0f); shareAlertRect.anchorMax = new Vector2(0.5f, 0f);
        shareAlertRect.pivot = new Vector2(0.5f, 0f);
        shareAlertRect.sizeDelta = new Vector2(460f, 64f);
        shareAlertRect.anchoredPosition = new Vector2(0f, 16f);
        ShareAlert = shareAlertBg.gameObject;
        ShareAlert.SetActive(false);

        // TMP_FontAsset은 이 파일이 쓰는 다른 UI(legacy Text)와 달리 직접 안 만든다 -
        // TextMeshProUGUI는 비워두면 TMP_Settings.defaultFontAsset을 알아서 쓴다.
        var alertText = NewRect("Text", shareAlertRect).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        alertText.fontSize = 26; alertText.color = Color.white;
        alertText.alignment = TMPro.TextAlignmentOptions.Center;
        Stretch(alertText.rectTransform);
        ShareAlertText = alertText;

        dropZone.alertPanel = ShareAlert;
        dropZone.alertText = ShareAlertText;
    }

    void BuildTabs(Font font)
    {
        var tabBarRect = NewRect("TabBar", Root.transform);
        tabBarRect.anchorMin = new Vector2(0f, 1f); tabBarRect.anchorMax = new Vector2(1f, 1f);
        tabBarRect.pivot = new Vector2(0.5f, 1f);
        tabBarRect.sizeDelta = new Vector2(0f, TabBarHeight);
        tabBarRect.anchoredPosition = new Vector2(0f, -(HeaderHeight + VideoHeight));
        var tabBarLayout = tabBarRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabBarLayout.childForceExpandWidth = true; tabBarLayout.childForceExpandHeight = true;

        CreateTab(tabBarRect, "작업가이드", font, Tab.Guide, out guideTabLabel, out guideUnderline);
        CreateTab(tabBarRect, "파일공유·지시", font, Tab.Share, out shareTabLabel, out shareUnderline);
        CreateTab(tabBarRect, "3D모델", font, Tab.Model, out modelTabLabel, out modelUnderline);
        CreateTab(tabBarRect, "그리기", font, Tab.Draw, out drawTabLabel, out drawUnderline);

        var contentAreaRect = NewRect("ContentArea", Root.transform);
        contentAreaRect.anchorMin = new Vector2(0f, 1f); contentAreaRect.anchorMax = new Vector2(1f, 1f);
        contentAreaRect.pivot = new Vector2(0.5f, 1f);
        contentAreaRect.sizeDelta = new Vector2(0f, ContentHeight);
        contentAreaRect.anchoredPosition = new Vector2(0f, -(HeaderHeight + VideoHeight + TabBarHeight));

        // 작업가이드·3D모델·그리기 탭은 빈 자리만 - 각 패널이 Start에서 자기 UI를 여기로 옮겨 붙인다
        // (WorkGuidePanel.SetEmbedParent / ModelRotateTestPanel.SetControlsEmbedParent / LiveDrawOverlay.SetControlsEmbedParent).
        GuideContent = CreateEmptyTab(contentAreaRect, "GuideContent");
        guideContentGo = GuideContent.gameObject;
        shareContentGo = CreateShareTab(contentAreaRect, font);
        ModelContent = CreateEmptyTab(contentAreaRect, "ModelContent");
        modelContentGo = ModelContent.gameObject;
        DrawContent = CreateEmptyTab(contentAreaRect, "DrawContent");
        drawContentGo = DrawContent.gameObject;
    }

    static RectTransform CreateEmptyTab(Transform parent, string name)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f); rect.offsetMax = new Vector2(-10f, -10f);
        rect.gameObject.SetActive(false);
        return rect;
    }

    // "파일공유·지시" 탭 - 씬에 이미 있는 정보공유_상세(InfoShareManager)와 지시하기(InstructionDrawing)를 여는 버튼 두 개.
    GameObject CreateShareTab(Transform parent, Font font)
    {
        var rect = NewRect("ShareContent", parent);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(30f, 20f); rect.offsetMax = new Vector2(-30f, -20f);

        var hintText = NewText("Hint", rect, font, 26, MrUiTheme.InkDim, TextAnchor.UpperCenter);
        hintText.text = "파일공유: 자료 아이콘을 화상통화 화면에 드래그하면 AR에 전송됩니다.\n지시하기: 현재 화면을 캡처해서 화살표·스티커로 짚어 AR에 전송합니다.";
        hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0.5f); hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.offsetMin = Vector2.zero; hintRect.offsetMax = Vector2.zero;

        CreateActionButton(rect, font, "FileShareBtn", "파일공유 열기", new Vector2(0f, 104f), () => FileShareClicked?.Invoke());
        CreateActionButton(rect, font, "OpenInstructionBtn", "지시하기 열기", new Vector2(0f, 10f), () => InstructionClicked?.Invoke());

        rect.gameObject.SetActive(false);
        return rect.gameObject;
    }

    // 하단 중앙에 쌓이는 알약형 버튼(파일공유/지시하기 공용)
    static void CreateActionButton(Transform parent, Font font, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var btnImg = NewRect(name, parent).gameObject.AddComponent<Image>();
        btnImg.color = MrUiTheme.AccentSoft;
        MrUiTheme.Round(btnImg, 16);
        var btn = btnImg.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);
        var btnRect = btnImg.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0f); btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.sizeDelta = new Vector2(320f, 84f);
        btnRect.anchoredPosition = anchoredPosition;

        var btnLabel = NewText("Label", btnRect, font, 31, MrUiTheme.Ink, TextAnchor.MiddleCenter);
        btnLabel.text = label;
        btnLabel.raycastTarget = false;
        Stretch(btnLabel.rectTransform);
    }

    void CreateTab(Transform parent, string label, Font font, Tab tab, out Text labelText, out Image underline)
    {
        var img = NewRect("Tab_" + label, parent).gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // 클릭 영역용 - 배경은 투명
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => TabClicked?.Invoke(tab));

        labelText = NewText("Label", img.rectTransform, font, 31, MrUiTheme.InkDim, TextAnchor.MiddleCenter);
        labelText.text = label;
        labelText.raycastTarget = false;
        var textRect = labelText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.2f); textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        underline = NewRect("Underline", img.rectTransform).gameObject.AddComponent<Image>();
        underline.color = MrUiTheme.Accent;
        var underlineRect = underline.rectTransform;
        underlineRect.anchorMin = new Vector2(0.15f, 0f); underlineRect.anchorMax = new Vector2(0.85f, 0f);
        underlineRect.sizeDelta = new Vector2(0f, 3f);
        underlineRect.anchoredPosition = new Vector2(0f, 8f);
    }

    // ── 작은 빌더 헬퍼 ──

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Text NewText(string name, Transform parent, Font font, int size, Color color, TextAnchor anchor)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
