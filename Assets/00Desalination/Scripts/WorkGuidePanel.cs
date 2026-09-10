using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 작업 가이드 패널 - 화상통화가 수락되면 뜨고, 단계 진행은 AR이 주도한다(AR의 이전/다음 버튼이
// InmoVideoCallSignaling.SendGuideStepControl()로 보내는 신호를 이 패널이 그대로 따라간다).
// 2026-08-26: "AR이 작업을 넘기면 MR에서 자동으로 넘어가야 함, MR 이전/다음/완료 버튼은
// 필요없어" - MR 자체 버튼(이전/다음/완료)을 없앴다. 지나온 단계는 AR이 다음으로 넘길 때마다
// 자동으로 완료 표시된다(더 이상 수동 토글 없음).
//
// 2026-09: 단계 데이터를 DB로 옮겼다. 통화 수락 시 support-call 상세(→ manual_id)를 받아
// GET /api/manuals/{manual_id}/steps 로 스텝 목록을 불러온다. manual_id가 없거나 서버 실패 시
// FallbackSteps(펌프 베어링 시나리오)로 degrade 한다.
//
// 씬의 기존 "화상통화" 패널이 어디에 어떻게 배치돼 있는지(3D 좌표) 몰라서, 대신 카메라에 매달아
// 항상 시야 앞(HUD 방식)에 뜨게 했다. 위치가 마음에 안 들면 Update()의 로컬 오프셋 값만 조절하면 됨.
public class WorkGuidePanel : MonoBehaviour
{
    struct Step
    {
        public string ar; // 짧은 지시문 (목록에 표시)  ← manual_step.title
        public string mr; // 전문가용 상세 설명          ← manual_step.description
        public Step(string ar, string mr) { this.ar = ar; this.mr = mr; }
    }

    // 서버/DB를 못 불러왔을 때만 쓰는 폴백 (구 PumpBearingWearSteps). 실제 데이터는 manual_step 테이블.
    static readonly Step[] FallbackSteps =
    {
        new Step("펌프 외관을 확인하세요",
            "고압펌프 #1 - AI가 베어링 마모 의심 징후를 감지했습니다. 케이싱 및 배관 연결부에 누유, 균열, 이물질 부착 여부를 육안으로 확인하세요."),
        new Step("전원을 차단하고 안전 조치를 하세요",
            "펌프를 정지하고 전원을 차단(LOTO)한 후, 배관 잔압이 해제됐는지 확인하세요."),
        new Step("커플링 분리 후 하우징 커버를 개방하세요",
            "모터-펌프 커플링을 분리하고, 베어링 하우징 고정 볼트를 해체해 커버를 분리하세요."),
        new Step("베어링을 인출해 육안 검사하세요",
            "축에서 베어링을 조심스럽게 분리한 뒤, 볼/궤도면의 피팅, 스코어링, 변색 여부를 확인하세요."),
        new Step("유격을 측정해 판정하세요",
            "베어링 유격(clearance)을 측정하여 마모 허용치 초과 여부를 판단하고 교체 여부를 결정하세요."),
        new Step("재조립 후 결과를 기록하세요",
            "재조립 후 시운전으로 이상 여부를 재확인하고, 점검 결과를 기록하세요."),
    };

    // MrCallDockPanel 헤더가 그대로 가져다 쓴다. 스텝 로드 시 manual.title 로 갱신된다.
    public static string JobTitle = "작업 가이드";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("WorkGuidePanel");
        go.AddComponent<WorkGuidePanel>();
        DontDestroyOnLoad(go);
    }

    public static WorkGuidePanel Instance;
    // ModelRotateTestPanel이 현재 단계에 맞춰 3D모델을 펌프/베어링으로 자동 전환하는 데 쓴다.
    public int StepIndex => stepIndex;
    public int StepCount => steps.Count;

    GameObject canvasGo;
    GameObject panel;
    Image panelBg;
    Text stepCountText;
    Text mrStepText;

    // 단계 목록(퀘스트 스타일): 완료=체크, 현재=강조, 예정=빈 동그라미. 클릭 불가, 순수 상태 표시용.
    GameObject listGo;
    RectTransform listRect;
    RectTransform mrTextRect;
    Image[] rowBgs = new Image[0];
    Text[] rowIcons = new Text[0];
    Text[] rowLabels = new Text[0];

    readonly List<Step> steps = new List<Step>();

    string channelName;
    int stepIndex = 0; // 0-based
    bool[] completed = new bool[0];
    Coroutine loadRoutine;

    // MrCallDockPanel 도크에 임베드되면 카메라 추적을 멈추고 SetEmbedParent 위치에 붙어있는다.
    bool embedded;

    const float RowHeight = 38f;
    const float RowGap = 4f;
    const float ListTop = 54f;

    void Awake()
    {
        Instance = this;
        BuildShell();
        panel.SetActive(false);
    }

    public void SetEmbedParent(RectTransform parent)
    {
        if (parent == null) return;

        canvasGo.transform.SetParent(parent, false);
        var rt = (RectTransform)canvasGo.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;

        float scaleX = parent.rect.width / 900f;
        float scaleY = parent.rect.height / 560f;
        rt.localScale = Vector3.one * Mathf.Min(scaleX, scaleY);

        // 도크가 이미 자기 배경을 갖고 있어서 임베드될 때만 투명하게.
        panelBg.color = Color.clear;

        embedded = true;
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnCallAccepted += HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded += HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected += HandleCallRejected;
        VideoCallSignalingMR.OnGuideStepControlReceived += HandleGuideStepControl;
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallAccepted -= HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected -= HandleCallRejected;
        VideoCallSignalingMR.OnGuideStepControlReceived -= HandleGuideStepControl;
    }

    void HandleCallAccepted(string channel)
    {
        channelName = channel;
        stepIndex = 0;
        if (loadRoutine != null) StopCoroutine(loadRoutine);
        loadRoutine = StartCoroutine(LoadStepsAndShow());
    }

    // support-call 상세 → manual_id → manual_step 목록을 불러와 패널을 띄운다.
    IEnumerator LoadStepsAndShow()
    {
        string manualId = null;
        string manualTitle = null;

        string callId = VideoCallSignalingMR.CurrentSupportCallId;
        if (!string.IsNullOrEmpty(callId))
        {
            string url = ServerConfig.BaseUrl + "/api/support-calls/" + callId;
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<SupportCallResponse>(req.downloadHandler.text);
                if (res != null && res.success && res.data != null)
                {
                    manualId    = res.data.manual_id;
                    manualTitle = res.data.manual_title;
                }
            }
            else
            {
                Debug.LogWarning("[WorkGuide] support-call 상세 로드 실패: " + req.error);
            }
        }

        steps.Clear();

        if (!string.IsNullOrEmpty(manualId))
        {
            string url = ServerConfig.BaseUrl + "/api/manuals/" + manualId + "/steps";
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<StepListResponse>(req.downloadHandler.text);
                if (res != null && res.success && res.data != null)
                {
                    foreach (var s in res.data)
                        steps.Add(new Step(s.title, s.description));
                }
            }
            else
            {
                Debug.LogWarning("[WorkGuide] 스텝 로드 실패: " + req.error);
            }
        }

        if (steps.Count == 0)
        {
            Debug.LogWarning("[WorkGuide] 스텝 데이터 없음 - 폴백 시나리오 사용");
            steps.AddRange(FallbackSteps);
        }

        // manual.title 을 받았을 때만 헤더 제목 갱신. 없으면 SupportCallList가 설정한 값 유지.
        if (!string.IsNullOrEmpty(manualTitle))
            JobTitle = manualTitle;

        stepIndex = 0;
        completed = new bool[steps.Count];
        RebuildStepList();
        panel.SetActive(true);
        RefreshStep();
        loadRoutine = null;
    }

    // AR이 이전/다음을 눌러 보내는 실제 단계(1-based). MR은 자체 버튼이 없으니 이 신호만 따라간다.
    void HandleGuideStepControl(int remoteStepIndex, int remoteStepCount)
    {
        if (steps.Count == 0) return; // 아직 로드 전
        int idx = Mathf.Clamp(remoteStepIndex - 1, 0, steps.Count - 1);
        if (idx == stepIndex) return;
        for (int i = 0; i < idx; i++) completed[i] = true;
        stepIndex = idx;
        RefreshStep();
    }

    void HandleCallEnded(string channel)
    {
        if (channel != channelName) return;
        panel.SetActive(false);
    }

    void HandleCallRejected(string channel)
    {
        panel.SetActive(false);
    }

    void RefreshStep()
    {
        if (steps.Count == 0) return;
        stepIndex = Mathf.Clamp(stepIndex, 0, steps.Count - 1);
        var step = steps[stepIndex];
        stepCountText.text = $"작업 순서 · {stepIndex + 1} / {steps.Count}";
        mrStepText.text = step.mr;

        for (int i = 0; i < steps.Count; i++)
        {
            if (i == stepIndex)
            {
                rowBgs[i].color = new Color(MrUiTheme.Warn.r, MrUiTheme.Warn.g, MrUiTheme.Warn.b, 0.18f);
                rowIcons[i].text = "▶";
                rowIcons[i].color = MrUiTheme.Warn;
                rowLabels[i].color = MrUiTheme.Ink;
                rowLabels[i].fontStyle = FontStyle.Bold;
            }
            else if (completed[i])
            {
                rowBgs[i].color = Color.clear;
                rowIcons[i].text = "✓";
                rowIcons[i].color = MrUiTheme.Good;
                rowLabels[i].color = MrUiTheme.InkDim;
                rowLabels[i].fontStyle = FontStyle.Normal;
            }
            else
            {
                rowBgs[i].color = Color.clear;
                rowIcons[i].text = "○";
                rowIcons[i].color = MrUiTheme.InkDim;
                rowLabels[i].color = MrUiTheme.InkDim;
                rowLabels[i].fontStyle = FontStyle.Normal;
            }
        }
    }

    void Update()
    {
        if (!panel.activeSelf) return;
        if (embedded) return; // MrCallDockPanel 안에 임베드됐으면 도크가 위치를 관리함

        var cam = Camera.main;
        if (cam != null && canvasGo.transform.parent != cam.transform)
        {
            canvasGo.transform.SetParent(cam.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, -0.62f, 1.4f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.0011f;
        }
    }

    // ── 캔버스 + 패널 껍데기 + 헤더 + 상세 텍스트. 스텝 행은 RebuildStepList()에서 동적 생성. ──
    void BuildShell()
    {
        var font = MrUiTheme.CreateFont();

        canvasGo = new GameObject("WorkGuideCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900, 560);
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        panelBg = panel.AddComponent<Image>();
        panelBg.color = MrUiTheme.Panel;
        MrUiTheme.Round(panelBg, 24);
        var bgRect = panel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        // ── 헤더: "작업 순서 · X / Y" ──
        var countGo = new GameObject("StepCount");
        countGo.transform.SetParent(panel.transform, false);
        stepCountText = countGo.AddComponent<Text>();
        stepCountText.font = font;
        stepCountText.fontSize = 26;
        stepCountText.alignment = TextAnchor.MiddleLeft;
        stepCountText.color = MrUiTheme.InkDim;
        var countRect = countGo.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 1f); countRect.anchorMax = new Vector2(1f, 1f);
        countRect.pivot = new Vector2(0.5f, 1f);
        countRect.sizeDelta = new Vector2(-60f, 44f); countRect.anchoredPosition = new Vector2(0f, 0f);

        // ── 스텝 목록 컨테이너(비어있음) ──
        listGo = new GameObject("StepList");
        listGo.transform.SetParent(panel.transform, false);
        listRect = listGo.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f); listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.sizeDelta = new Vector2(-60f, 0f);
        listRect.anchoredPosition = new Vector2(0f, -ListTop);

        // ── 현재 단계 상세 설명(전문가용 긴 문장) ──
        var mrTextGo = new GameObject("MrStepText");
        mrTextGo.transform.SetParent(panel.transform, false);
        mrStepText = mrTextGo.AddComponent<Text>();
        mrStepText.font = font;
        mrStepText.fontSize = 22;
        mrStepText.alignment = TextAnchor.UpperLeft;
        mrStepText.color = MrUiTheme.InkDim;
        mrStepText.horizontalOverflow = HorizontalWrapMode.Wrap;
        mrTextRect = mrTextGo.GetComponent<RectTransform>();
        mrTextRect.anchorMin = new Vector2(0f, 1f); mrTextRect.anchorMax = new Vector2(1f, 1f);
        mrTextRect.pivot = new Vector2(0.5f, 1f);
        mrTextRect.sizeDelta = new Vector2(-60f, 90f);
        mrTextRect.anchoredPosition = new Vector2(0f, -(ListTop + 10f));
    }

    // 현재 steps 수만큼 목록 행을 다시 만든다.
    void RebuildStepList()
    {
        for (int i = listGo.transform.childCount - 1; i >= 0; i--)
            Destroy(listGo.transform.GetChild(i).gameObject);

        int n = steps.Count;
        var font = MrUiTheme.CreateFont();
        float listHeight = n > 0 ? n * RowHeight + (n - 1) * RowGap : 0f;
        listRect.sizeDelta = new Vector2(-60f, listHeight);

        rowBgs = new Image[n];
        rowIcons = new Text[n];
        rowLabels = new Text[n];

        for (int i = 0; i < n; i++)
        {
            var rowGoI = new GameObject("Row" + i);
            rowGoI.transform.SetParent(listGo.transform, false);
            var rowRectI = rowGoI.AddComponent<RectTransform>();
            rowRectI.anchorMin = new Vector2(0f, 1f); rowRectI.anchorMax = new Vector2(1f, 1f);
            rowRectI.pivot = new Vector2(0.5f, 1f);
            rowRectI.sizeDelta = new Vector2(0f, RowHeight);
            rowRectI.anchoredPosition = new Vector2(0f, -i * (RowHeight + RowGap));

            var rowBg = rowGoI.AddComponent<Image>();
            rowBg.color = Color.clear;
            MrUiTheme.Round(rowBg, 8);
            rowBgs[i] = rowBg;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(rowGoI.transform, false);
            var icon = iconGo.AddComponent<Text>();
            icon.font = font; icon.fontSize = 24;
            icon.alignment = TextAnchor.MiddleCenter;
            icon.raycastTarget = false;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0f); iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(50f, 0f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            rowIcons[i] = icon;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGoI.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.font = font; label.fontSize = 22;
            label.text = steps[i].ar;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(66f, 0f); labelRect.offsetMax = new Vector2(-10f, 0f);
            rowLabels[i] = label;
        }

        mrTextRect.anchoredPosition = new Vector2(0f, -(ListTop + listHeight + 10f));
    }

    // ── 서버 응답 DTO ──
    [System.Serializable]
    class SupportCallResponse { public bool success; public SupportCallData data; }
    [System.Serializable]
    class SupportCallData { public string manual_id; public string manual_title; public string work_type; }
    [System.Serializable]
    class StepListResponse { public bool success; public StepDto[] data; }
    [System.Serializable]
    class StepDto { public int step_no; public string title; public string description; public string model_url; }
}
