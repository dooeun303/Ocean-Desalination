using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 2026-10-20: "AR에서 MR로 원격지원 요청이 오면 MR에 알림만 띄우고 3초 후 사라지게. 사용자는 그걸 보고
// 메뉴에 들어가서 직접 원격지원을 확인할 거야" - 서버(support_call.js POST)가 MR에 보내는
// { type: "support_call_request", id }를 VideoCallSignalingMR이 넘겨주면, 요청 상세(GET /api/support-calls/{id})로
// 요청자·설비를 채워 눈앞에 짧게 띄우고 알림음을 한 번 울린다. 버튼 없음 - 수락/거절은 원격지원 목록에서.
// 씬에 따로 붙일 필요 없음(다른 MR HUD 패널처럼 스스로 생성).
// 2026-10-26: "모든 알림을 이 방식으로" - 고장예지 알람(AlarmUI)도 같은 모양으로 띄운다. 알람은 버튼(설비로 이동/닫기)이
// 있어서 ShowAction으로 띄우고, 누를 때까지 그 자리에 떠 있다(3초 알림이 그 사이에 오면 끝난 뒤 다시 보여줌).
public class SupportCallRequestAlert : MonoBehaviour
{
    const float PanelWidth = 620f;
    const float PanelHeight = 230f;
    const float ActionPanelHeight = 420f; // 버튼이 있는 알림(고장예지 알람)
    const float ShowSeconds = 3f;
    const float FadeSeconds = 0.25f;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("SupportCallRequestAlert");
        go.AddComponent<SupportCallRequestAlert>();
        DontDestroyOnLoad(go);
    }

    GameObject canvasGo;
    CanvasGroup group;
    Text badgeText;
    Text titleText;
    Text bodyText;
    Image barImage;
    GameObject buttonRow;
    Text primaryLabel;
    Action primaryAction;
    StickyAlert sticky;      // 버튼을 누를 때까지 떠 있는 알림(없으면 null)

    class StickyAlert { public string badge, title, body, primary; public Color accent; public Action onPrimary; }
    AudioSource audioSource;
    AudioClip ringClip;
    Coroutine showRoutine;

    // 다른 화면에서 같은 모양의 알림을 띄울 때 쓴다(예: 통화 중 다른 지원요청 수락 차단 - SupportCallList).
    public static SupportCallRequestAlert Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        BuildUI();
        canvasGo.SetActive(false);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        ringClip = CreateRingClip();
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnSupportCallRequested += HandleRequest;
        VideoCallSignalingMR.OnMaintenanceComplete += HandleMaintenanceComplete;
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnSupportCallRequested -= HandleRequest;
        VideoCallSignalingMR.OnMaintenanceComplete -= HandleMaintenanceComplete;
    }

    // 2026-10-21: AR이 작업 결과를 저장하면(MaintenanceTutorialDemo.SaveAndFinish → maintenance_complete)
    // 같은 모양의 알림으로 알려준다.
    void HandleMaintenanceComplete() => ShowMessage("작업 종료", "현장작업자가 작업을 종료하였습니다.", "");

    void HandleRequest(string callId) => StartCoroutine(LoadAndShow(callId));

    IEnumerator LoadAndShow(string callId)
    {
        SupportCallData data = null;
        yield return SupportCallApi.Get(callId, d => data = d,
            e => Debug.LogWarning("[SupportCallAlert] 요청 상세 조회 실패: " + e));
        // 상세를 못 받아도 알림 자체는 띄운다.
        Show(data ?? new SupportCallData { id = callId });
    }

    void Show(SupportCallData data)
    {
        string who = string.IsNullOrEmpty(data.initiator_name) ? "현장 작업자" : data.initiator_name;
        string equipment = string.IsNullOrEmpty(data.equipment_name) ? "설비 미지정" : data.equipment_name;
        ShowMessage("원격지원 요청", $"{who} 님이 원격지원을 요청했습니다", $"설비: {equipment}\n메뉴 > 원격지원에서 확인하세요");
        Debug.Log($"[SupportCallAlert] 원격지원 요청 알림: {data.id} ({who} / {equipment})");
    }

    // 알림 공통 - 띠 문구/제목/본문을 채우고 3초 띄운 뒤 사라진다. 알림음 한 번.
    public void ShowMessage(string badge, string title, string body)
    {
        ApplyContent(badge, title, body, MrUiTheme.Accent, null);
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowForSeconds());
        audioSource.PlayOneShot(ringClip);
    }

    // 버튼이 있는 알림 - primary 버튼(예: 설비로 이동)이나 닫기를 누를 때까지 떠 있다.
    // 머리를 따라다니면 버튼을 겨누기 어려워서, 나타날 때 한 번만 눈앞에 놓고 그 자리에 고정한다.
    public void ShowAction(string badge, string title, string body, Color accent, string primary, Action onPrimary)
    {
        sticky = new StickyAlert { badge = badge, title = title, body = body, accent = accent, primary = primary, onPrimary = onPrimary };
        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
        ShowSticky();
        audioSource.PlayOneShot(ringClip);
    }

    void ShowSticky()
    {
        ApplyContent(sticky.badge, sticky.title, sticky.body, sticky.accent, sticky.primary);
        primaryAction = sticky.onPrimary;
        PlaceInFrontOfViewer();
        canvasGo.SetActive(true);
        group.alpha = 1f;
        canvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;
    }

    void CloseSticky()
    {
        sticky = null;
        primaryAction = null;
        canvasGo.SetActive(false);
    }

    void OnPrimaryClicked()
    {
        var action = primaryAction;
        CloseSticky();
        action?.Invoke();
    }

    // primary가 null이면 버튼 없는 3초 알림 모양, 있으면 버튼 줄을 보이고 패널을 키운다.
    void ApplyContent(string badge, string title, string body, Color accent, string primary)
    {
        badgeText.text = badge;
        titleText.text = title;
        bodyText.text = body;
        barImage.color = accent;

        bool action = primary != null;
        buttonRow.SetActive(action);
        if (action) primaryLabel.text = primary;
        group.interactable = action;
        group.blocksRaycasts = action;
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, action ? ActionPanelHeight : PanelHeight);
        titleText.rectTransform.sizeDelta = new Vector2(-64f, action ? 80f : 42f);
        bodyText.rectTransform.sizeDelta = new Vector2(-64f, action ? 130f : 80f);
        bodyText.rectTransform.anchoredPosition = new Vector2(0f, action ? -168f : -130f);
    }

    // 눈앞에 나타났다가(페이드+살짝 커짐) ShowSeconds 뒤 페이드아웃.
    IEnumerator ShowForSeconds()
    {
        PlaceInFrontOfViewer();
        canvasGo.SetActive(true);
        var t = canvasGo.transform;

        for (float e = 0f; e < FadeSeconds; e += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - e / FadeSeconds, 3f);
            group.alpha = k;
            t.localScale = Vector3.one * MrUiTheme.HudScale * Mathf.Lerp(0.9f, 1f, k);
            yield return null;
        }
        group.alpha = 1f;
        t.localScale = Vector3.one * MrUiTheme.HudScale;

        yield return new WaitForSeconds(ShowSeconds);

        for (float e = 0f; e < FadeSeconds; e += Time.deltaTime)
        {
            group.alpha = 1f - e / FadeSeconds;
            yield return null;
        }
        canvasGo.SetActive(false);
        showRoutine = null;

        // 3초 알림이 떠 있던 알람을 가렸으면 다시 보여준다.
        if (sticky != null) ShowSticky();
    }

    // 떠 있는 3초 동안은 머리를 따라 움직인다(고개를 돌려도 보이게). 버튼 알림은 고정.
    void LateUpdate()
    {
        if (canvasGo.activeSelf && showRoutine != null) PlaceInFrontOfViewer();
    }

    // 시선의 수평 방향 앞쪽, 눈높이보다 살짝 위. 패널 앞면이 사용자를 향하게.
    void PlaceInFrontOfViewer()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
        fwd.Normalize();
        Vector3 pos = cam.transform.position + fwd * (MrUiTheme.HudDepth * 0.9f) + Vector3.up * 0.15f;
        canvasGo.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.transform.position, Vector3.up));
    }

    // AR 없이 모양/소리 확인 - Play 모드에서 DontDestroyOnLoad의 SupportCallRequestAlert 선택 → 컴포넌트 우클릭.
    [ContextMenu("테스트: 원격지원 요청 알림 띄우기")]
    void TestShow()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[SupportCallAlert] Play 모드에서 실행하세요."); return; }
        Show(new SupportCallData { id = "test", initiator_name = "김현수", equipment_name = "펌프 P-101" });
    }

    [ContextMenu("테스트: 작업 종료 알림 띄우기")]
    void TestMaintenanceComplete()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[SupportCallAlert] Play 모드에서 실행하세요."); return; }
        HandleMaintenanceComplete();
    }

    [ContextMenu("테스트: 버튼 알림(고장예지 알람 모양) 띄우기")]
    void TestAction()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[SupportCallAlert] Play 모드에서 실행하세요."); return; }
        ShowAction("고장예지 알람 · 2026-10-26 15:30:00", "펌프 P-101의 고장예지 확률이 임계값을 초과하였습니다.",
            "진동 및 온도 이상 패턴이 감지되어 고장 가능성이 높습니다.", MrUiTheme.Warn, "설비로 이동",
            () => Debug.Log("[SupportCallAlert] 테스트: 설비로 이동 클릭"));
    }

    // ───────────── UI ─────────────

    void BuildUI()
    {
        var font = MrUiTheme.CreateFont();

        canvasGo = new GameObject("SupportCallAlertCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50; // 다른 HUD 패널보다 위
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, PanelHeight);
        canvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>(); // 버튼 알림을 XR 레이로 누르기 위해
        group = canvasGo.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false; // 3초 알림은 레이를 가로막지 않음(버튼 알림일 때만 켬)

        var bg = canvasGo.AddComponent<Image>();
        bg.color = MrUiTheme.Panel;
        MrUiTheme.Round(bg, 24);

        var bar = NewRect("AccentBar", canvasGo.transform);
        bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, 60f);
        barImage = bar.gameObject.AddComponent<Image>();
        barImage.color = MrUiTheme.Accent;
        MrUiTheme.Round(barImage, 24);

        badgeText = NewText("Badge", bar, font, 25, Color.white, TextAnchor.MiddleLeft);
        badgeText.text = "원격지원 요청";
        badgeText.fontStyle = FontStyle.Bold;
        Stretch(badgeText.rectTransform, new Vector2(32f, 0f), new Vector2(-32f, 0f));

        titleText = NewText("Title", canvasGo.transform, font, 27, MrUiTheme.Ink, TextAnchor.UpperLeft);
        titleText.fontStyle = FontStyle.Bold;
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var tr = titleText.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(-64f, 42f);
        tr.anchoredPosition = new Vector2(0f, -80f);

        bodyText = NewText("Body", canvasGo.transform, font, 22, MrUiTheme.InkDim, TextAnchor.UpperLeft);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.lineSpacing = 1.2f;
        var br = bodyText.rectTransform;
        br.anchorMin = new Vector2(0f, 1f); br.anchorMax = new Vector2(1f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(-64f, 80f);
        br.anchoredPosition = new Vector2(0f, -130f);

        // 버튼 줄(버튼 알림일 때만 보임) - 오른쪽 primary(포인트색), 왼쪽 닫기.
        var row = NewRect("Buttons", canvasGo.transform);
        row.anchorMin = new Vector2(0f, 0f); row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.sizeDelta = new Vector2(-64f, 80f);
        row.anchoredPosition = new Vector2(0f, 28f);
        buttonRow = row.gameObject;
        NewButton(row, font, "Close", "닫기", MrUiTheme.AccentSoft, MrUiTheme.Ink, new Vector2(0f, 0.5f), CloseSticky);
        primaryLabel = NewButton(row, font, "Primary", "", MrUiTheme.Accent, Color.white, new Vector2(1f, 0.5f), OnPrimaryClicked);
        buttonRow.SetActive(false);
    }

    // 버튼 줄의 왼쪽/오른쪽 끝(anchorX 0 또는 1)에 붙는 둥근 버튼. 라벨 Text를 돌려준다.
    static Text NewButton(Transform parent, Font font, string name, string label, Color bg, Color fg, Vector2 anchor,
        UnityEngine.Events.UnityAction onClick)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.sizeDelta = new Vector2(266f, 80f);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        MrUiTheme.Round(img, 16);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var text = NewText("Label", rt, font, 27, fg, TextAnchor.MiddleCenter);
        text.text = label;
        text.fontStyle = FontStyle.Bold;
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return text;
    }

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
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }

    // "띵-동" 두 음 알림음을 코드로 만든다(오디오 파일 없이).
    static AudioClip CreateRingClip()
    {
        const int rate = 44100;
        float[] tones = { 880f, 660f };
        const float toneLen = 0.18f, gap = 0.06f;
        int toneSamples = (int)(rate * toneLen), gapSamples = (int)(rate * gap);
        var data = new float[(toneSamples + gapSamples) * tones.Length];
        int i = 0;
        foreach (var f in tones)
        {
            for (int s = 0; s < toneSamples; s++, i++)
            {
                float env = Mathf.Min(1f, s / (rate * 0.01f)) * (1f - (float)s / toneSamples); // 짧은 어택 + 감쇠
                data[i] = Mathf.Sin(2f * Mathf.PI * f * s / rate) * 0.35f * env;
            }
            i += gapSamples;
        }
        var clip = AudioClip.Create("SupportCallRing", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
