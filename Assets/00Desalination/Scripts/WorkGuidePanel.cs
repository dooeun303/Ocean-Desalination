using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 작업 가이드 패널 - 화상통화가 수락되면 뜨고, MR이 이전/다음으로 단계를 넘기면 그때마다
// VideoCallSignalingMR.SendGuideStep()으로 AR(InmoCallManager)에 같은 단계가 동기화되어 표시된다.
// AR은 읽기전용이라 조작은 이 패널에서만 한다("완료"는 이동과 별개의 명시적 동작 - 자동 완료 아님).
//
// 작업타입별 단계 데이터가 아직 서버/DB에 없어서, 우선 대화에서 나온 예시(밸브 교체)를 하드코딩해뒀다.
// 나중에 실제 유지보수 등록 데이터(장비/알람/매뉴얼)와 연결되면 이 배열을 서버 응답으로 교체하면 된다.
//
// 씬의 기존 "화상통화" 패널이 어디에 어떻게 배치돼 있는지(3D 좌표) 몰라서, 대신 카메라에 매달아
// 항상 시야 앞(HUD 방식)에 뜨게 했다. 위치가 마음에 안 들면 Update()의 로컬 오프셋 값만 조절하면 됨.
public class WorkGuidePanel : MonoBehaviour
{
    struct Step
    {
        public string ar;
        public string mr;
        public Step(string ar, string mr) { this.ar = ar; this.mr = mr; }
    }

    static readonly Step[] ValveReplacementSteps =
    {
        new Step(
            "밸브를 확인하세요",
            "대상 밸브의 위치와 상태를 육안으로 확인합니다. 몸체 손상이나 누수 흔적이 있는지 점검하세요."),
        new Step(
            "나사를 풀어주세요",
            "밸브 고정 나사를 순서대로 풀어냅니다. 나사는 분실되지 않도록 별도 용기에 보관하세요."),
        new Step(
            "밸브를 새것으로 교체하세요",
            "기존 밸브를 제거하고 새 밸브를 장착합니다. 방향과 규격이 일치하는지 확인 후 단단히 고정하세요."),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("WorkGuidePanel");
        go.AddComponent<WorkGuidePanel>();
        DontDestroyOnLoad(go);
    }

    GameObject canvasGo;
    GameObject panel;
    Text stepCountText;
    Text mrStepText;
    Text completeBtnLabel;
    Image completeBtnImage;

    string channelName;
    int stepIndex = 0; // 0-based
    bool[] completed;

    void Awake()
    {
        BuildUI();
        panel.SetActive(false);
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnCallAccepted += HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded += HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected += HandleCallRejected;
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallAccepted -= HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected -= HandleCallRejected;
    }

    void HandleCallAccepted(string channel)
    {
        channelName = channel;
        stepIndex = 0;
        completed = new bool[ValveReplacementSteps.Length];
        panel.SetActive(true);
        RefreshStep();
        SendCurrentStep();
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
        var step = ValveReplacementSteps[stepIndex];
        stepCountText.text = $"{stepIndex + 1} / {ValveReplacementSteps.Length}단계";
        mrStepText.text = step.mr;
        completeBtnLabel.text = completed[stepIndex] ? "완료됨 ✓" : "이 단계 완료";
        completeBtnImage.color = completed[stepIndex] ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.35f, 0.35f, 0.4f);
    }

    void SendCurrentStep()
    {
        if (string.IsNullOrEmpty(channelName)) return;
        var step = ValveReplacementSteps[stepIndex];
        VideoCallSignalingMR.Instance?.SendGuideStep(channelName, stepIndex + 1, ValveReplacementSteps.Length, step.ar);
    }

    void OnPrev()
    {
        if (stepIndex <= 0) return;
        stepIndex--;
        RefreshStep();
        SendCurrentStep();
    }

    void OnNext()
    {
        if (stepIndex >= ValveReplacementSteps.Length - 1) return;
        stepIndex++;
        RefreshStep();
        SendCurrentStep();
    }

    void OnToggleComplete()
    {
        completed[stepIndex] = !completed[stepIndex];
        RefreshStep();
    }

    void Update()
    {
        if (!panel.activeSelf) return;

        // 씬의 실제 화상통화 패널 좌표를 몰라서, 카메라 앞 고정 오프셋에 매 프레임 붙여 넣는다.
        var cam = Camera.main;
        if (cam != null && canvasGo.transform.parent != cam.transform)
        {
            canvasGo.transform.SetParent(cam.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, -0.25f, 1.4f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.0015f;
        }
    }

    Button CreateButton(Transform parent, string label, Color bgColor, Font font, out Text labelText)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 220f;
        le.preferredHeight = 80f;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        labelText = text;
        return btn;
    }

    void BuildUI()
    {
        var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 36);

        canvasGo = new GameObject("WorkGuideCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900, 400);
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        var bgRect = panel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleText = titleGo.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "작업 가이드";
        titleText.fontSize = 30;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.4f, 0.8f, 1f);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f); titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(800, 50); titleRect.anchoredPosition = new Vector2(0, -35);

        var countGo = new GameObject("StepCount");
        countGo.transform.SetParent(panel.transform, false);
        stepCountText = countGo.AddComponent<Text>();
        stepCountText.font = font;
        stepCountText.fontSize = 24;
        stepCountText.alignment = TextAnchor.MiddleCenter;
        stepCountText.color = new Color(0.7f, 0.7f, 0.7f);
        var countRect = countGo.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 1f); countRect.anchorMax = new Vector2(0.5f, 1f);
        countRect.sizeDelta = new Vector2(800, 40); countRect.anchoredPosition = new Vector2(0, -80);

        var mrTextGo = new GameObject("MrStepText");
        mrTextGo.transform.SetParent(panel.transform, false);
        mrStepText = mrTextGo.AddComponent<Text>();
        mrStepText.font = font;
        mrStepText.fontSize = 28;
        mrStepText.alignment = TextAnchor.MiddleCenter;
        mrStepText.color = Color.white;
        mrStepText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var mrTextRect = mrTextGo.GetComponent<RectTransform>();
        mrTextRect.anchorMin = new Vector2(0.5f, 0.5f); mrTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        mrTextRect.sizeDelta = new Vector2(800, 150); mrTextRect.anchoredPosition = new Vector2(0, 0);

        var rowGo = new GameObject("Buttons");
        rowGo.transform.SetParent(panel.transform, false);
        var rowRect = rowGo.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f); rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.sizeDelta = new Vector2(780, 100); rowRect.anchoredPosition = new Vector2(0, 70);
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 30f;

        var navColor = new Color(0.25f, 0.45f, 0.85f);
        var prevBtn = CreateButton(rowGo.transform, "이전", navColor, font, out _);
        prevBtn.onClick.AddListener(OnPrev);
        var nextBtn = CreateButton(rowGo.transform, "다음", navColor, font, out _);
        nextBtn.onClick.AddListener(OnNext);
        var completeBtn = CreateButton(rowGo.transform, "이 단계 완료", new Color(0.35f, 0.35f, 0.4f), font, out completeBtnLabel);
        completeBtnImage = completeBtn.GetComponent<Image>();
        completeBtn.onClick.AddListener(OnToggleComplete);
    }
}
