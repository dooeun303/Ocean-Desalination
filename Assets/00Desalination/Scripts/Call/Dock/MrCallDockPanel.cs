using System.Collections;
using UnityEngine;

// 유지보수 협업 도크(Presenter). 통화 신호(VideoCallSignalingMR)를 받아 화면(MrCallDockView)·Agora(DockAgoraSession)·
// 배치(DockArrivalLayout)를 움직인다. 통화가 수락돼야 뜨고(HandleCallAccepted) 끝나면 사라진다 -
// WorkGuidePanel/LiveDrawOverlay와 동일한 규칙.
// 2026-10-27 리팩토링 3단계: 화면 구성·Agora·배치를 각각 파일로 나누고, 여기에는 흐름만 남겼다.
//
// 씬에 빈 GameObject 하나 만들어서 이 컴포넌트만 붙이면 된다(MR 2: 시스템/MrCallDockPanel).
public class MrCallDockPanel : MonoBehaviour
{
    // 통화 수락~종료 동안 AR 원격 영상 / MR 시점 화면을 mp4로 녹화 → 종료 시 서버 업로드.
    // 2026-10-20 잠시 껐다가 2026-10-21 다시 켬(영상·통화 음성 저장 필요). false로 바꾸면 녹화 안 함.
    // 꺼져 있으면 recorder가 null - 녹화 전용 채널/토큰/외부 비디오 소스도 안 쓴다.
    const bool EnableCallRecording = true;

    // 2026-10-23: 예전엔 토큰 발급이 한 번 실패하면 조용히 끝나서 도크가 "연결 대기 중"으로 영원히 남았다.
    // 몇 번 재시도하고, 그래도 안 되면 영상 영역에 실패를 표시한다(닫기 버튼으로 통화를 끝내면 됨).
    const int TokenAttempts = 3;
    const float TokenRetryDelay = 2f;

    // 2026-10-23: 지금 통화 중인지 - 통화 중에 다른 지원요청을 수락하면 이전 Agora 채널을 안 나간 채
    // 채널 이름만 덮어써져서, 이전 통화의 종료 신호도 무시되는 꼬인 상태가 됐다. SupportCallList가 이걸
    // 보고 수락을 막는다. 도크가 수락 신호를 받았을 때도(다른 경로) 이전 통화부터 정리한다(HandleCallAccepted).
    public static bool IsInCall { get; private set; }

    MrCallDockView view;
    DockAgoraSession agora;
    readonly DockArrivalLayout layout = new DockArrivalLayout();
    MrCallRecorder recorder;

    InstructionDrawing instructionDrawing;
    InfoShareManager infoShareManager;

    string channelName;
    float callStartTime;

    void Awake()
    {
        if (EnableCallRecording) recorder = gameObject.AddComponent<MrCallRecorder>();
        view = new MrCallDockView();
        agora = new DockAgoraSession(view.VideoContainer, recorder, view.SetWaitingForVideo);

        view.CloseClicked += OnCloseClicked;
        view.TabClicked += view.SelectTab;
        view.FileShareClicked += OnOpenFileShareClicked;
        view.InstructionClicked += OnOpenInstructionDrawingClicked;
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnCallAccepted += HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded += HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected += HandleCallRejected;
        VideoCallSignalingMR.OnMaintenanceComplete += HandleMaintenanceComplete;
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallAccepted -= HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected -= HandleCallRejected;
        VideoCallSignalingMR.OnMaintenanceComplete -= HandleMaintenanceComplete;
    }

    // 다른 패널들의 UI를 도크 탭 자리에 끼워 넣고, 지시하기/파일공유 패널을 찾아 둔다.
    void Start()
    {
        RewireInstructionDrawing();
        RewireInfoShareManager();
        WorkGuidePanel.Instance?.SetEmbedParent(view.GuideContent);
        ModelRotateTestPanel.Instance?.SetControlsEmbedParent(view.ModelContent);
        LiveDrawOverlay.Instance?.SetControlsEmbedParent(view.DrawContent);
        LiveDrawOverlay.Instance?.SetVideoAreaEmbedParent(view.VideoContainer);
    }

    void Update()
    {
        if (!view.IsVisible) return;
        view.SetCallTime(Mathf.Max(0, Mathf.FloorToInt(Time.time - callStartTime)));
        // 헤더 제목/진행 단계 - WorkGuidePanel의 실제 진행 상황을 그대로 반영한다("2/3 단계는 뭐야?" 피드백).
        var guide = WorkGuidePanel.Instance;
        view.SetHeader(WorkGuidePanel.JobTitle, guide != null ? $"진행중 · {guide.StepIndex + 1} / {guide.StepCount}단계" : "");
    }

    void OnDestroy()
    {
        IsInCall = false; // 정적 값이라 씬이 바뀌어도 남는다
        agora?.Dispose();
    }

    // ───────────────────────────── 통화 흐름 ─────────────────────────────

    void HandleCallAccepted(string channel)
    {
        if (IsInCall && channel != channelName)
        {
            Debug.LogWarning($"[MrCallDockPanel] 통화 중({channelName})에 새 통화({channel}) 수락 - 이전 채널부터 정리");
            LeaveAgoraChannel();
            layout.RestoreRightSlot();
        }
        IsInCall = true;
        channelName = channel;
        callStartTime = Time.time;
        view.SetConnectingText(MrCallDockView.DefaultConnectingText); // 이전 통화의 "연결 실패" 문구가 남지 않게
        view.SetVisible(true);
        MrUiOnTop.Apply(view.Root); // 설비에 가려지지 않게(도크에 끼워 넣은 탭 내용까지 매번 다시)

        // 2026-10-21: 원격지원으로 설비 앞에 도착했으면 세 패널을 시선 기준으로 한 번에 배치한다.
        // 유지보수 도착 연출이 도크 위치를 지정했으면 거기에(시선 밖이면 눈앞), 그 외는 눈앞.
        if (MrArrivalState.Marker != null && Camera.main != null)
            layout.LayoutArrival(view.Transform, MrArrivalState.Marker, MrCallDockView.PanelWidth * MrUiTheme.HudScale);
        else if (MrArrivalState.HasDockPose && DockArrivalLayout.IsInFrontOfViewer(MrArrivalState.DockPos))
            DockArrivalLayout.PlaceAt(view.Transform, MrArrivalState.DockPos, MrArrivalState.DockRot);
        else
            DockArrivalLayout.PlaceInFront(view.Transform);

        view.SelectTab(MrCallDockView.Tab.Guide);
        StartCoroutine(FetchAgoraTokenAndJoin(channel));
    }

    void HandleCallEnded(string channel)
    {
        if (channel != channelName) return;
        IsInCall = false;
        view.SetVisible(false);
        LeaveAgoraChannel();
        EquipmentMarker.CurrentlyHighlighted?.StopHighlight();
        layout.RestoreRightSlot();
        MrArrivalState.RestoreArrival(); // 수락 시 숨겼던 메뉴 등 원복
    }

    void HandleCallRejected(string channel)
    {
        IsInCall = false;
        view.SetVisible(false);
        // 아직 join 전이라(수락 전에는 Agora 채널에 들어가지 않음) 나갈 것도 없음.
        layout.RestoreRightSlot();
        MrArrivalState.RestoreArrival();
    }

    // "AR에서 저장 완료하면 MR도 진행 패널을 종료해야 하고, 설비 깜빡임도 사라져야 해" - 통화가
    // 이미 끝나있을 수도 있어서(작업 종료 후 결과저장까지 가는 동안 통화를 먼저 끊었을 수 있음)
    // call_end와 별개로 처리한다. 도크가 이미 닫혀있어도 안전하게 아무 일도 안 하고,
    // 깜빡이던 설비 하이라이트는 통화 상태와 무관하게 항상 정리한다.
    void HandleMaintenanceComplete()
    {
        IsInCall = false;
        if (view.IsVisible)
        {
            view.SetVisible(false);
            LeaveAgoraChannel();
        }
        EquipmentMarker.CurrentlyHighlighted?.StopHighlight();
        layout.RestoreRightSlot();
        MrArrivalState.RestoreArrival();
    }

    IEnumerator FetchAgoraTokenAndJoin(string channel)
    {
        string token = null;
        for (int attempt = 1; attempt <= TokenAttempts && token == null; attempt++)
        {
            if (attempt > 1)
            {
                view.SetConnectingText($"연결 재시도 중 ({attempt}/{TokenAttempts})");
                yield return new WaitForSeconds(TokenRetryDelay);
            }
            if (!IsInCall || channel != channelName) yield break; // 기다리는 사이 통화가 이미 끝났거나 바뀜

            int tries = attempt;
            yield return RtcApi.Token(channel, t => token = t,
                e => Debug.LogWarning($"[MrCallDockPanel][Agora] 토큰 발급 실패 ({tries}/{TokenAttempts}): {e}"));
        }

        // 통화가 끝나도 channelName은 그대로 남아서, 이름 비교만으로는 "끝난 통화"를 못 걸렀다 - IsInCall도 본다.
        if (!IsInCall || channel != channelName) yield break;
        if (token == null)
        {
            view.SetConnectingText("연결 실패 - 서버 상태를 확인하세요");
            yield break;
        }
        view.SetConnectingText(MrCallDockView.DefaultConnectingText);

        agora.Join(token, channel);
        if (recorder == null) yield break; // 녹화 꺼짐

        // 녹화 세션 시작 - 실제 MR 화면 녹화는 전용 채널 토큰을 받은 뒤 StartViewCapture에서.
        recorder.Begin(agora.Engine, channel, VideoCallSignalingMR.CurrentSupportCallId);

        // MR 시점 화면 녹화용 전용 채널({채널}_mrrec) 토큰 - 받지 못하면 녹화만 건너뛰고 통화는 그대로.
        string recToken = null;
        yield return RtcApi.Token(channel + "_mrrec", t => recToken = t,
            e => Debug.LogWarning("[MrCallDockPanel][Agora] 녹화 채널 토큰 발급 실패: " + e));
        if (channel != channelName || !agora.InChannel || recToken == null) yield break;
        recorder.StartViewCapture(recToken);
    }

    void LeaveAgoraChannel()
    {
        if (!agora.InChannel) return;
        agora.Leave();
        view.ClearRemoteVideo();
        view.SetWaitingForVideo(true);
    }

    // ───────────────────────────── 버튼 ─────────────────────────────

    // 헤더 닫기 버튼 - MR이 직접 통화를 끊는다. AR/도크쪽 정리는 EndCallAsInitiator가 기존
    // OnCallEnded 구독자(HandleCallEnded 등)를 그대로 재사용해서 처리하고, 여기서는 DB 상태만
    // 별도로 REST(/end)로 정리한다(지원요청 큐의 status='active'가 영원히 안 남게).
    // (MrTestBridge가 이 이름으로 호출한다)
    void OnCloseClicked()
    {
        string callId = VideoCallSignalingMR.CurrentSupportCallId;
        if (!string.IsNullOrEmpty(callId))
            StartCoroutine(EndSupportCallOnServer(callId));
        VideoCallSignalingMR.Instance?.EndCallAsInitiator(channelName);
    }

    IEnumerator EndSupportCallOnServer(string callId)
    {
        // by=MR - 서버가 AR에 call_end를 보낸다(EndCallAsInitiator의 WS 전송이 끊겨 있어도 AR이 알 수 있게).
        yield return SupportCallApi.End(callId, "MR", null,
            e => Debug.LogWarning("[MrCallDockPanel] 통화 종료 처리 실패: " + e));
    }

    void OnOpenFileShareClicked()
    {
        if (infoShareManager == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InfoShareManager 연결 안 됨 - 파일공유를 열 수 없음.");
            return;
        }
        var go = infoShareManager.gameObject;
        bool wasActive = go.activeSelf;
        go.SetActive(true); // OnEnable()에서 바로 파일 목록을 불러온다
        layout.PlaceInRightSlot(go.transform, wasActive); // 원격지원 도착 화면이면 화상통화 오른쪽에
    }

    void OnOpenInstructionDrawingClicked()
    {
        if (instructionDrawing == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InstructionDrawing 연결 안 됨 - 지시하기를 열 수 없음.");
            return;
        }
        var panel = instructionDrawing.drawingPanel;
        bool wasActive = panel != null && panel.activeSelf;
        instructionDrawing.Open();
        // Open()이 화면 캡처에 실패하면 패널이 안 열린다 - 열렸을 때만 오른쪽 자리로 옮긴다.
        if (panel != null && panel.activeSelf) layout.PlaceInRightSlot(panel.transform, wasActive);
    }

    // InstructionDrawing("지시하기")은 원래 "화상통화_일반" 패널 안의 "영상영역"에서 화면을
    // 캡처했다 - 이 도크가 실제 영상이 그려지는 위치(VideoContainer)이므로, 캡처 소스도 여기로
    // 옮겨주지 않으면 "화상통화 텍스처 없음"으로 캡처가 조용히 실패한다.
    // InstructionDrawing의 루트 오브젝트가 기본적으로 꺼져있는 상태라 FindFirstObjectByType에 Include를 꼭 줘야 찾아진다.
    void RewireInstructionDrawing()
    {
        instructionDrawing = FindFirstObjectByType<InstructionDrawing>(FindObjectsInactive.Include);
        if (instructionDrawing == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InstructionDrawing을 못 찾음 - 씬에 지시하기 오브젝트가 있는지 확인하세요.");
            return;
        }
        instructionDrawing.videoImageParent = view.VideoContainer;
        // 2026-10-21: 전송 결과도 파일공유와 같은 검은 토스트(도크 영상 칸 하단)로 - 씬의 별도 알림 패널 대신.
        instructionDrawing.alertPanel = view.ShareAlert;
        instructionDrawing.alertText = view.ShareAlertText;
    }

    // 씬에 이미 있는 "정보공유_상세"(InfoShareManager) 팝업 - OnEnable()에서 파일 목록을 불러오므로 찾아 두기만 한다.
    void RewireInfoShareManager()
    {
        infoShareManager = FindFirstObjectByType<InfoShareManager>(FindObjectsInactive.Include);
        if (infoShareManager == null)
            Debug.LogWarning("[MrCallDockPanel] InfoShareManager를 못 찾음 - 씬에 정보공유_상세 오브젝트가 있는지 확인하세요.");
    }
}
