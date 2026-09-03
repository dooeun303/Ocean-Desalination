using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 작업 가이드 패널 - 화상통화가 수락되면 뜨고, 단계 진행은 AR이 주도한다(AR의 이전/다음 버튼이
// InmoVideoCallSignaling.SendGuideStepControl()로 보내는 신호를 이 패널이 그대로 따라간다).
// 2026-08-26: "AR이 작업을 넘기면 MR에서 자동으로 넘어가야 함, MR 이전/다음/완료 버튼은
// 필요없어" - MR 자체 버튼(이전/다음/완료)을 없앴다. 지나온 단계는 AR이 다음으로 넘길 때마다
// 자동으로 완료 표시된다(더 이상 수동 토글 없음).
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

    // 고장예지 알람 → 펌프 점검 시나리오 데모용 (2026-08-12, 검토 회의 후속). 지금은 안 쓰지만
    // 나중에 다시 필요할 수 있어 남겨둔다 - ActiveSteps 전환 지점만 바꾸면 됨.
    static readonly Step[] PumpInspectionSteps =
    {
        new Step(
            "펌프 외관을 확인하세요",
            "AI 예지 알람이 발생한 펌프입니다. 케이싱 및 배관 연결부에 누유나 부식 흔적이 있는지 육안으로 확인하세요."),
        new Step(
            "진동과 소음을 확인하세요",
            "펌프 가동 중 비정상적인 진동이나 소음이 있는지 확인합니다. 이상이 감지되면 정도를 기록하세요."),
        new Step(
            "베어링 온도를 측정하세요",
            "베어링 하우징 표면 온도를 측정합니다. 정상 범위(70도 이하)를 초과하는지 확인하세요."),
        new Step(
            "점검 결과를 전문가에게 보고하세요",
            "점검한 항목과 이상 유무를 원격 전문가에게 보고하고, 필요 시 추가 조치 지시를 받으세요."),
    };

    // 2026-08-14 데모 시나리오를 펌프 대신 "컨트롤러 건전지 교체"로 바꿨다 - 펌프처럼 큰 설비보다
    // 손에 쥔 컨트롤러가 실물로 시연하기 쉽고, MR쪽에서 실제로 들고 있는 컨트롤러를 그대로 교체
    // 대상으로 쓸 수 있어서 골랐다. 3단계("측면에서 건전지 케이스를 여세요")는 MR이 모델 회전
    // 테스트 기능으로 컨트롤러를 돌려 측면을 보여주고 포인터로 케이스 위치를 짚어주는 것과 맞물리게
    // 설계됨 - 그 자체는 이 스크립트가 아니라 MR 쪽에서 수동으로 같이 조작해줘야 한다.
    static readonly Step[] BatteryReplacementSteps =
    {
        new Step(
            "준비물을 확인하세요: AA 건전지 1개",
            "작업에 필요한 준비물을 확인합니다. AA 건전지 1개가 필요합니다."),
        new Step(
            "교체 대상 컨트롤러의 상태를 확인하세요",
            "교체 대상 컨트롤러의 외관과 상태를 확인합니다."),
        new Step(
            "컨트롤러의 측면에서 건전지 케이스를 여세요",
            "컨트롤러 측면의 건전지 케이스를 엽니다. (모델 회전 테스트로 측면을 보여주고 포인터로 케이스 위치를 짚어주세요)"),
        new Step(
            "기존 건전지를 분리시키세요",
            "기존 건전지를 분리합니다."),
        new Step(
            "새 건전지를 건전지함에 넣어주세요",
            "새 건전지를 건전지함에 넣습니다."),
        new Step(
            "건전지함 케이스를 닫고 원래 위치로 되돌려주세요",
            "건전지함 케이스를 닫고 컨트롤러를 원래 위치로 되돌립니다."),
    };

    // 2026-08-25 조차장님 시연용 시나리오 - "고압펌프 #1 베어링 마모 의심" 데모 스크립트에 맞춤
    // (Monitoring3 대시보드의 고장예지 알람 팝업 문구와 동일한 사건을 다룬다). 3단계에서 막혀서
    // MR을 호출하는 게 핵심 - "정상 범위인데 미세한 이상음이 남는" 애매한 케이스라 SOP만으론
    // 판단이 안 되고 전문가가 3D 모델로 설명해줘야 하는 지점.
    static readonly Step[] PumpBearingWearSteps =
    {
        new Step(
            "펌프 외관을 확인하세요",
            "고압펌프 #1 - AI가 베어링 마모 의심 징후를 감지했습니다. 케이싱 및 배관 연결부에 누유, 균열, 이물질 부착 여부를 육안으로 확인하세요."),
        new Step(
            "전원을 차단하고 안전 조치를 하세요",
            "펌프를 정지하고 전원을 차단(LOTO)한 후, 배관 잔압이 해제됐는지 확인하세요."),
        new Step(
            "커플링 분리 후 하우징 커버를 개방하세요",
            "모터-펌프 커플링을 분리하고, 베어링 하우징 고정 볼트를 해체해 커버를 분리하세요."),
        new Step(
            "베어링을 인출해 육안 검사하세요",
            "축에서 베어링을 조심스럽게 분리한 뒤, 볼/궤도면의 피팅, 스코어링, 변색 여부를 확인하세요."),
        new Step(
            "유격을 측정해 판정하세요",
            "베어링 유격(clearance)을 측정하여 마모 허용치 초과 여부를 판단하고 교체 여부를 결정하세요."),
        new Step(
            "재조립 후 결과를 기록하세요",
            "재조립 후 시운전으로 이상 여부를 재확인하고, 점검 결과를 기록하세요."),
    };

    // 데모 시나리오 전환 지점 - 다른 시나리오를 보여줄 땐 이 두 줄만 바꾸면 됨. JobTitle은
    // MrCallDockPanel 헤더가 그대로 가져다 쓴다(예전엔 헤더에 "펌프 P-102 · 정기 점검"이 하드코딩
    // 플레이스홀더로 남아있어서 실제 시나리오와 안 맞았음).
    static readonly Step[] ActiveSteps = PumpBearingWearSteps;
    public const string JobTitle = "펌프 P-101 · 베어링 마모 의심 점검";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("WorkGuidePanel");
        go.AddComponent<WorkGuidePanel>();
        DontDestroyOnLoad(go);
    }

    public static WorkGuidePanel Instance;
    // ModelRotateTestPanel이 현재 단계에 맞춰 3D모델을 펌프/베어링으로 자동 전환하는 데 쓴다
    // (AR쪽 MaintenanceTutorialDemo.manualIndex와 동일한 개념, MR도 같이 연동해달라는 요청).
    public int StepIndex => stepIndex;
    public int StepCount => ActiveSteps.Length;

    GameObject canvasGo;
    GameObject panel;
    Image panelBg;
    Text stepCountText;
    Text mrStepText;

    // 2026-08-24: "작업가이드 탭 안에 또 작업가이드가 있는 형태"라는 피드백 - 제목/현재 단계
    // 문단만 보여주던 방식을 "전문가가 가상공간에서 훑어보기 좋은" 퀘스트 목록 스타일 리스트로
    // 바꿨다. 전체 단계를 한 번에 보여주고(완료=체크, 현재=강조, 예정=빈 동그라미), 리스트
    // 자체는 클릭 안 되는 순수 상태 표시용 - 이동/완료는 기존대로 아래 버튼으로만 한다.
    Image[] rowBgs;
    Text[] rowIcons;
    Text[] rowLabels;

    string channelName;
    int stepIndex = 0; // 0-based
    bool[] completed;

    // 2026-08-24: MR 통합 도크(MrCallDockPanel) 작업가이드 탭에 이 패널을 그대로 끼워넣기 위해
    // 추가 - AR쪽 ModelRotateTestDemo.Init(markerMode, embedParent)와 같은 목적의 패턴.
    // embedded가 true면 Update()의 카메라 추적(HUD 방식)을 멈추고, SetEmbedParent가 정해준
    // 위치에 그대로 붙어있는다.
    bool embedded;

    void Awake()
    {
        Instance = this;
        BuildUI();
        panel.SetActive(false);
    }

    // parent 밑에 이 패널의 캔버스를 넣고 그 크기에 맞게 배치한다. 늘려서 딱 채우지 않고
    // 균일 배율만 적용하는 이유: 내부 제목/단계표시/버튼들이 전부 900x400 캔버스 기준
    // 고정 픽셀 오프셋으로 짜여있어서, 다른 비율로 늘리면 버튼이 잘리거나 텍스트가 겹친다.
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

        // 2026-08-24: "박스 안에 박스" 피드백 - 도크(MrCallDockPanel)가 이미 자기 배경 패널을
        // 갖고 있는데 이 패널도 자기 배경(panelBg)을 따로 그려서 이중으로 보였다. 임베드될
        // 때만 투명하게 - 단독으로 뜰 때(다른 씬)는 배경이 있어야 하니 색만 지운다.
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
        completed = new bool[ActiveSteps.Length];
        panel.SetActive(true);
        RefreshStep();
    }

    // AR이 이전/다음을 눌러 보내는 실제 단계(1-based, WorkGuidePanel 관례와 동일) - MR은 자체
    // 버튼이 없으니 이 신호만으로 그대로 따라간다. 건너뛴(지나온) 단계는 자동으로 완료 표시.
    void HandleGuideStepControl(int remoteStepIndex, int remoteStepCount)
    {
        if (completed == null) return; // 아직 통화 수락 전
        int idx = Mathf.Clamp(remoteStepIndex - 1, 0, ActiveSteps.Length - 1);
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
        var step = ActiveSteps[stepIndex];
        stepCountText.text = $"작업 순서 · {stepIndex + 1} / {ActiveSteps.Length}";
        mrStepText.text = step.mr;

        for (int i = 0; i < ActiveSteps.Length; i++)
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

        // 씬의 실제 화상통화 패널 좌표를 몰라서, 카메라 앞 고정 오프셋에 매 프레임 붙여 넣는다.
        //
        // 2026-08-24: "패널이 너무 큼" + "세 패널이 겹침" 피드백으로 조정. 스케일을 0.0015→0.0011로
        // 줄여서 900x400 캔버스 내부 좌표는 그대로 두고(제목/단계표시/버튼 앵커 재계산 불필요)
        // 전체를 균일하게 축소했다. Y도 -0.25→-0.62로 내려서 LiveDrawOverlay(중앙, 아래쪽 끝이
        // 대략 Y=-0.37)와 겹치지 않는 아래쪽 밴드를 차지하게 했다. ModelRotateTestPanel의 버튼들도
        // 이 변경에 맞춰 재배치함(ModelRotateTestPanel.cs 참고) - 세 스크립트가 서로의 정확한 크기를
        // 모른 채 HudDepth/HudScale만 공유해서 생긴 문제라, 실제 좌표를 서로 맞물리게 다시 계산함.
        // 빌드해서 실기기로 겹침이 실제로 해소됐는지 확인 필요(에디터에서는 대략적인 비율만 확인함).
        var cam = Camera.main;
        if (cam != null && canvasGo.transform.parent != cam.transform)
        {
            canvasGo.transform.SetParent(cam.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, -0.62f, 1.4f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.0011f;
        }
    }

    void BuildUI()
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

        // ── 헤더: "작업 순서 · X / Y" 한 줄 ── 예전엔 "작업 가이드" 제목 + 단계표시가 따로였는데,
        // 도크의 탭 이름이 이미 "작업가이드"라 그 안에 또 "작업 가이드"라는 제목이 뜨는 게
        // 중복이라는 피드백으로 하나로 합쳤다.
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

        // ── 단계 목록: 완료(체크)/현재(강조)/예정(빈 동그라미) 퀘스트 목록 스타일 ──
        // 이동/완료 자체는 여전히 아래 버튼으로만 하고, 리스트는 순수 상태 표시용(클릭 불가).
        int n = ActiveSteps.Length;
        const float RowHeight = 38f;
        const float RowGap = 4f;
        float listTop = 54f;
        float listHeight = n * RowHeight + (n - 1) * RowGap;

        rowBgs = new Image[n];
        rowIcons = new Text[n];
        rowLabels = new Text[n];

        var listGo = new GameObject("StepList");
        listGo.transform.SetParent(panel.transform, false);
        var listRect = listGo.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f); listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.sizeDelta = new Vector2(-60f, listHeight);
        listRect.anchoredPosition = new Vector2(0f, -listTop);

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
            label.text = ActiveSteps[i].ar;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(66f, 0f); labelRect.offsetMax = new Vector2(-10f, 0f);
            rowLabels[i] = label;
        }

        // ── 현재 단계 상세 설명(전문가용 긴 문장, step.mr) - 목록 바로 아래 ──
        var mrTextGo = new GameObject("MrStepText");
        mrTextGo.transform.SetParent(panel.transform, false);
        mrStepText = mrTextGo.AddComponent<Text>();
        mrStepText.font = font;
        mrStepText.fontSize = 22;
        mrStepText.alignment = TextAnchor.UpperLeft;
        mrStepText.color = MrUiTheme.InkDim;
        mrStepText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var mrTextRect = mrTextGo.GetComponent<RectTransform>();
        mrTextRect.anchorMin = new Vector2(0f, 1f); mrTextRect.anchorMax = new Vector2(1f, 1f);
        mrTextRect.pivot = new Vector2(0.5f, 1f);
        mrTextRect.sizeDelta = new Vector2(-60f, 90f);
        mrTextRect.anchoredPosition = new Vector2(0f, -(listTop + listHeight + 10f));
    }
}
