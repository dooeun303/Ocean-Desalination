using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.JoinChannelVideoToken;

// 유지보수 협업 도크: 위쪽에 화상통화 영상을 크게 띄우고, 그 아래 작업가이드/파일공유·지시/
// 3D모델 탭에 각각 WorkGuidePanel/InstructionDrawing/ModelRotateTestPanel을 연결한다. 지금까지
// 이 넷이 각자 따로 떠서 화면이 어지럽다는 피드백으로 하나로 모은 것. 통화가 수락돼야 뜨고
// (HandleCallAccepted) 끝나면 사라진다 - WorkGuidePanel/LiveDrawOverlay와 동일한 규칙.
//
// 2026-08-26: 원래는 씬의 "화상통화_일반" 패널(JoinChannelVideoToken)을 그대로 두고 그
// remoteVideoContainer/connectingSpinner 필드만 이 도크 쪽으로 재배정하는 방식이었는데,
// "화상통화_일반은 아예 안 쓰고 그 연결 로직만 참고하라"는 요청으로 전면 교체했다 - 이제 이
// 도크가 자기 자신의 IRtcEngineEx를 직접 만들어서 토큰 발급→join→원격 영상 렌더링까지 전부
// 스스로 처리한다(JoinChannelVideoToken.cs의 코드를 그대로 참고했지만 그 인스턴스/GameObject는
// 전혀 안 건드림 - MakeVideoView/DestroyVideoView만 static 유틸로 재사용). "화상통화_일반"
// GameObject가 씬에서 비활성 상태라 그 위의 컴포넌트가 StartCoroutine을 못 돌리는 문제가 있었고,
// 그걸 억지로 활성화하는 대신 아예 의존을 끊는 게 더 안전한 방향이라 이렇게 바꿨다.
//
// 씬에 빈 GameObject 하나 만들어서 이 컴포넌트만 붙이면 된다(WorkGuidePanel.cs와 달리 이건
// 전역 부트스트랩이 아니라 이 도크를 쓰는 씬에만 수동으로 붙이는 방식 - 다른 씬에는 영향 없음).
public class MrCallDockPanel : MonoBehaviour
{
    enum Tab { Guide, Share, Model, Draw }

    const float PanelWidth = 900f;
    const float VideoHeight = 520f;
    const float HeaderHeight = 80f;
    const float TabBarHeight = 64f;
    // 2026-08-24: 작업가이드 탭이 단계 목록(퀘스트 리스트 스타일)으로 바뀌면서 세로 공간이 더
    // 필요해져서 280→420으로 키움 (WorkGuidePanel.cs의 900x560 캔버스가 이 안에 임베드될 때
    // 글자가 너무 작게 축소되는 걸 방지 - SetEmbedParent의 균일 축소 배율 참고).
    const float ContentHeight = 420f;
    // 도크는 작은 HUD 버튼들과 같은 MrUiTheme.HudDepth(1.4)를 그대로 썼더니, 전체 크기(900x1084
    // 유닛)가 커서 얼굴 바로 앞을 가릴 만큼 크게 보였다("너무 가까워" 실기 확인). 버튼들은 크기가
    // 작아 그 거리에서도 괜찮았지만, 도크는 더 멀리 둬야 같은 체감 크기가 된다.
    const float DockDepth = 2.2f;
    const string SupportCallUrl = "http://192.168.0.66:3000/api/support-calls/";
    // InmoCallManager.cs(AR)/JoinChannelVideoToken.cs(MR 원본 로직)와 동일한 App ID/토큰 서버.
    const string AgoraAppID = "5b6baf59793d494f95554d1246847731";
    const string AgoraTokenServerUrl = "http://192.168.0.66:3000";

    GameObject canvasGo;
    RectTransform videoContainer;
    GameObject connectingSpinner;

    Button guideTabBtn, shareTabBtn, modelTabBtn, drawTabBtn;
    Image guideUnderline, shareUnderline, modelUnderline, drawUnderline;
    Text guideTabLabel, shareTabLabel, modelTabLabel, drawTabLabel;
    GameObject guideContent, shareContent, modelContent, drawContent;
    RectTransform guideContentRect;
    RectTransform modelContentRect;
    RectTransform drawContentRect;

    InstructionDrawing instructionDrawing;
    InfoShareManager infoShareManager;
    string channelName;

    Text callTimeText;
    Text headerTitleText;
    Text headerStatusText;
    float callStartTime;

    // 이 도크 전용 Agora 엔진 - "화상통화_일반"의 JoinChannelVideoToken 인스턴스와는 완전히
    // 독립적이다(그 오브젝트/코루틴에 의존하지 않음).
    IRtcEngineEx _rtcEngine;
    bool _agoraInCall;

    // 2026-08-26: "MRDockCanvas는 컨트롤러로 조정할 수 있어야 함, 메뉴패널처럼" - 씬의 그랩 가능한
    // "메뉴" 패널(XRGrabInteractable+Rigidbody+BoxCollider)과 동일한 레시피를 그대로 옮겨왔다.
    // 한 번이라도 손으로 잡아서 옮기면 그 뒤로는 Update()의 카메라-추적(HUD) 로직이 그 위치를
    // 다시 덮어쓰지 않도록 막는다 - 안 그러면 놓자마자 다음 프레임에 원래 HUD 위치로 튕겨 돌아간다.
    bool grabbedOnce;

    // 2026-09-03: 유지보수 도착 연출에서 EquipmentMarker가 "도크를 정보 패널 옆 여기에" 라고
    // 포즈를 남기면(MrArrivalState.HasDockPose), 카메라 앞 HUD 대신 그 자리에 월드 고정으로 띄운다.
    bool worldAnchored;

    void Awake()
    {
        BuildUI();
        SelectTab(Tab.Guide);
        // 2026-08-24: "Play 누르자마자 도크가 뜨는데 통화 요청은 언제 나가는거냐" 피드백 - 이
        // 도크만 WorkGuidePanel/LiveDrawOverlay와 다르게 통화 상태를 구독 안 해서 항상 떠
        // 있었다. 나머지 패널들과 동일하게 통화가 수락돼야 뜨도록 맞춘다.
        canvasGo.SetActive(false);
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

    void HandleCallAccepted(string channel)
    {
        channelName = channel;
        callStartTime = Time.time;
        grabbedOnce = false; // 새 통화마다 기본 HUD 위치에서 다시 시작
        canvasGo.SetActive(true);

        // 유지보수 도착 연출이 도크 위치를 지정했으면(정보 패널 옆) 카메라 HUD 대신 거기에 월드 고정.
        worldAnchored = MrArrivalState.HasDockPose;
        if (worldAnchored)
        {
            canvasGo.transform.SetParent(null, true);
            canvasGo.transform.SetPositionAndRotation(MrArrivalState.DockPos, MrArrivalState.DockRot);
            canvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;
        }

        SelectTab(Tab.Guide);
        StartCoroutine(FetchAgoraTokenAndJoin(channel));
    }

    void HandleCallEnded(string channel)
    {
        if (channel != channelName) return;
        canvasGo.SetActive(false);
        LeaveAgoraChannel();
        EquipmentMarker.CurrentlyHighlighted?.StopHighlight();
        MrArrivalState.RestoreArrival(); // 수락 시 숨겼던 메뉴 등 원복
    }

    void HandleCallRejected(string channel)
    {
        canvasGo.SetActive(false);
        // 아직 join 전이라(수락 전에는 Agora 채널에 들어가지 않음) 나갈 것도 없음.
        MrArrivalState.RestoreArrival();
    }

    // "AR에서 저장 완료하면 MR도 진행 패널을 종료해야 하고, 설비 깜빡임도 사라져야 해" - 통화가
    // 이미 끝나있을 수도 있어서(작업 종료 후 결과저장까지 가는 동안 통화를 먼저 끊었을 수 있음)
    // call_end와 별개로 처리한다. 도크가 이미 닫혀있어도(canvasGo 비활성) 안전하게 아무 일도
    // 안 하고, 깜빡이던 설비 하이라이트는 통화 상태와 무관하게 항상 정리한다.
    void HandleMaintenanceComplete()
    {
        if (canvasGo.activeSelf)
        {
            canvasGo.SetActive(false);
            LeaveAgoraChannel();
        }
        EquipmentMarker.CurrentlyHighlighted?.StopHighlight();
        MrArrivalState.RestoreArrival();
    }

    // ───────────── Agora (JoinChannelVideoToken.cs의 로직을 참고, 이 도크 전용 인스턴스) ─────────────

    IEnumerator FetchAgoraTokenAndJoin(string channel)
    {
        string url = $"{AgoraTokenServerUrl}/rtc/{channel}/0";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[MrCallDockPanel][Agora] 토큰 발급 실패: " + req.error);
            yield break;
        }

        var tokenResponse = JsonUtility.FromJson<AgoraTokenResponse>(req.downloadHandler.text);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.token))
        {
            Debug.LogWarning("[MrCallDockPanel][Agora] 토큰 응답이 비어있음");
            yield break;
        }

        if (channel != channelName) yield break; // 토큰 기다리는 사이 통화가 이미 끝났거나 바뀜

        EnsureAgoraEngine();
        _rtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        _rtcEngine.EnableAudio();
        _rtcEngine.EnableVideo(); // 원격(AR) 영상을 렌더링하려면 비디오 모듈 자체는 켜져 있어야 함

        var options = new ChannelMediaOptions();
        options.publishCameraTrack.SetValue(false);       // MR 카메라는 publish 안 함
        options.publishCustomVideoTrack.SetValue(false);
        options.publishMicrophoneTrack.SetValue(true);    // 마이크(오디오)만 publish
        options.autoSubscribeVideo.SetValue(true);        // AR 영상은 구독
        options.autoSubscribeAudio.SetValue(true);

        _rtcEngine.JoinChannel(tokenResponse.token, channel, 0, options);
        _agoraInCall = true;
        Debug.Log("[MrCallDockPanel][Agora] 채널 join: " + channel);
    }

    void EnsureAgoraEngine()
    {
        if (_rtcEngine != null) return;

        _rtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngineEx();
        var context = new RtcEngineContext
        {
            appId = AgoraAppID,
            channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
            audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
            areaCode = AREA_CODE.AREA_CODE_GLOB
        };
        _rtcEngine.Initialize(context);
        _rtcEngine.InitEventHandler(new DockAgoraEventHandler(this));
    }

    void LeaveAgoraChannel()
    {
        if (!_agoraInCall) return;
        _rtcEngine?.LeaveChannel();

        // 내가 먼저 나갈 땐 OnUserOffline이 안 와서 이전 영상 뷰가 남아있을 수 있다(재연결 시
        // MakeVideoView가 "이미 있다"고 착각해 예전 세션에 바인딩된 뷰를 재사용해버림 - 화면 안 뜸) -
        // JoinChannelVideoToken.OnDisable과 동일한 이유로 확실히 지운다.
        for (int i = videoContainer.childCount - 1; i >= 0; i--)
            Destroy(videoContainer.GetChild(i).gameObject);

        if (connectingSpinner != null) connectingSpinner.SetActive(true);
        _agoraInCall = false;
    }

    void OnDestroy()
    {
        if (_rtcEngine != null)
        {
            _rtcEngine.LeaveChannel();
            _rtcEngine.Dispose();
        }
    }

    [System.Serializable]
    class AgoraTokenResponse { public string token; }

    // JoinChannelVideoToken.UserEventHandler와 동일한 목적 - 원격(AR) 유저가 들어오면 영상 뷰를
    // 만들고, 나가면 지운다. MakeVideoView/DestroyVideoView는 JoinChannelVideoToken.cs의 static
    // 유틸을 그대로 재사용한다(그 클래스의 인스턴스/GameObject와는 무관).
    class DockAgoraEventHandler : IRtcEngineEventHandler
    {
        readonly MrCallDockPanel _owner;
        internal DockAgoraEventHandler(MrCallDockPanel owner) { _owner = owner; }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            JoinChannelVideoToken.MakeVideoView(uid, connection.channelId, _owner.videoContainer);
            if (_owner.connectingSpinner != null) _owner.connectingSpinner.SetActive(false);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            JoinChannelVideoToken.DestroyVideoView(uid);
            if (_owner.connectingSpinner != null) _owner.connectingSpinner.SetActive(true);
        }

        public override void OnError(int err, string msg)
        {
            Debug.LogError($"[MrCallDockPanel][Agora] Error {err}: {msg}");
        }
    }

    // 헤더 닫기 버튼 - MR이 직접 통화를 끊는다. AR/도크쪽 정리는 EndCallAsInitiator가 기존
    // OnCallEnded 구독자(HandleCallEnded 등)를 그대로 재사용해서 처리하고, 여기서는 DB 상태만
    // 별도로 REST(/end)로 정리한다(지원요청 큐의 status='active'가 영원히 안 남게).
    void OnCloseClicked()
    {
        string callId = VideoCallSignalingMR.CurrentSupportCallId;
        if (!string.IsNullOrEmpty(callId))
            StartCoroutine(EndSupportCallOnServer(callId));
        VideoCallSignalingMR.Instance?.EndCallAsInitiator(channelName);
    }

    IEnumerator EndSupportCallOnServer(string callId)
    {
        using var req = new UnityWebRequest(SupportCallUrl + callId + "/end", "PATCH");
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("[MrCallDockPanel] 통화 종료 처리 실패: " + req.error);
    }

    void Start()
    {
        RewireInstructionDrawing();
        RewireInfoShareManager();
        WorkGuidePanel.Instance?.SetEmbedParent(guideContentRect);
        ModelRotateTestPanel.Instance?.SetControlsEmbedParent(modelContentRect);
        LiveDrawOverlay.Instance?.SetControlsEmbedParent(drawContentRect);
        LiveDrawOverlay.Instance?.SetVideoAreaEmbedParent(videoContainer);
    }

    // InstructionDrawing("지시하기")은 원래 "화상통화_일반" 패널 안의 "영상영역"에서 화면을
    // 캡처했다 - 이 도크가 실제 영상이 그려지는 위치(videoContainer)이므로, 캡처 소스도 여기로
    // 옮겨주지 않으면 "화상통화 텍스처 없음"으로 캡처가 조용히 실패한다.
    // InstructionDrawing의 루트 오브젝트가 기본적으로 꺼져있는 상태(drawingPanel.SetActive(false))라
    // FindFirstObjectByType에 Include를 꼭 줘야 찾아진다.
    void RewireInstructionDrawing()
    {
        instructionDrawing = FindFirstObjectByType<InstructionDrawing>(FindObjectsInactive.Include);
        if (instructionDrawing == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InstructionDrawing을 못 찾음 - " +
                "씬에 지시하기 오브젝트가 있는지 확인하세요.");
            return;
        }
        instructionDrawing.videoImageParent = videoContainer;
    }

    // "파일공유·지시" 탭에 지시하기 버튼밖에 없어서 "파일공유" 버튼도 추가해달라는 요청 -
    // 씬에 이미 있는 "정보공유_상세"(InfoShareManager) 팝업을 그대로 연다(InstructionDrawing과
    // 동일한 패턴). InfoShareManager는 OnEnable()에서 바로 파일 목록을 불러오므로, 여기서는
    // 찾아서 캐싱만 해두고 버튼 클릭 시 SetActive(true)만 하면 된다.
    void RewireInfoShareManager()
    {
        infoShareManager = FindFirstObjectByType<InfoShareManager>(FindObjectsInactive.Include);
        if (infoShareManager == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InfoShareManager를 못 찾음 - " +
                "씬에 정보공유_상세 오브젝트가 있는지 확인하세요.");
        }
    }

    void OnGuideTabClicked() => SelectTab(Tab.Guide);
    void OnShareTabClicked() => SelectTab(Tab.Share);
    void OnModelTabClicked() => SelectTab(Tab.Model);
    void OnDrawTabClicked() => SelectTab(Tab.Draw);

    void SelectTab(Tab tab)
    {
        guideContent.SetActive(tab == Tab.Guide);
        shareContent.SetActive(tab == Tab.Share);
        modelContent.SetActive(tab == Tab.Model);
        drawContent.SetActive(tab == Tab.Draw);

        guideUnderline.gameObject.SetActive(tab == Tab.Guide);
        shareUnderline.gameObject.SetActive(tab == Tab.Share);
        modelUnderline.gameObject.SetActive(tab == Tab.Model);
        drawUnderline.gameObject.SetActive(tab == Tab.Draw);

        guideTabLabel.color = tab == Tab.Guide ? MrUiTheme.Accent : MrUiTheme.InkDim;
        shareTabLabel.color = tab == Tab.Share ? MrUiTheme.Accent : MrUiTheme.InkDim;
        modelTabLabel.color = tab == Tab.Model ? MrUiTheme.Accent : MrUiTheme.InkDim;
        drawTabLabel.color = tab == Tab.Draw ? MrUiTheme.Accent : MrUiTheme.InkDim;
    }

    void Update()
    {
        if (!canvasGo.activeSelf) return;

        int elapsed = Mathf.Max(0, Mathf.FloorToInt(Time.time - callStartTime));
        callTimeText.text = $"{elapsed / 60:00}:{elapsed % 60:00}";

        // 헤더 제목/진행 단계 - 예전엔 "펌프 P-102 · 정기 점검"/"진행중 · 2 / 3단계"가 하드코딩
        // 플레이스홀더로 남아있어서 실제 시나리오(펌프 P-101 베어링 마모 점검, 6단계)와 안 맞았다
        // ("2/3 단계는 뭐야?" 피드백). WorkGuidePanel의 실제 진행 상황을 그대로 반영한다.
        var guide = WorkGuidePanel.Instance;
        headerTitleText.text = WorkGuidePanel.JobTitle;
        headerStatusText.text = guide != null
            ? $"진행중 · {guide.StepIndex + 1} / {guide.StepCount}단계"
            : "";

        // WorkGuidePanel.cs와 동일한 카메라 앞 HUD 방식 - 씬의 실제 화상통화 패널 좌표를 몰라서
        // 이 도크도 독립적으로 카메라 앞에 고정 오프셋으로 띄운다.
        // 월드 고정 모드(유지보수 도착)면 카메라 앞 추적을 건너뛴다 - 손으로 잡아 옮기는 건 여전히 가능.
        if (worldAnchored) return;

        var cam = Camera.main;
        if (!grabbedOnce && cam != null && canvasGo.transform.parent != cam.transform)
        {
            canvasGo.transform.SetParent(cam.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, DockDepth);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;
        }
    }

    void BuildUI()
    {
        var font = MrUiTheme.CreateFont();
        float totalHeight = HeaderHeight + VideoHeight + TabBarHeight + ContentHeight;

        canvasGo = new GameObject("MrCallDockCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(PanelWidth, totalHeight);
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        // 씬의 그랩 가능한 "메뉴" 패널과 동일한 레시피: Kinematic Rigidbody(중력 없음) + 캔버스
        // 크기에 맞춘 BoxCollider(트리거 아님) + XRGrabInteractable(Kinematic, 놓을 때 던지기 없음).
        var dockCollider = canvasGo.AddComponent<BoxCollider>();
        dockCollider.isTrigger = false;
        dockCollider.size = new Vector3(PanelWidth, totalHeight, 40f);

        var dockRb = canvasGo.AddComponent<Rigidbody>();
        dockRb.useGravity = false;
        dockRb.isKinematic = true;

        var dockGrab = canvasGo.AddComponent<XRGrabInteractable>();
        dockGrab.movementType = XRBaseInteractable.MovementType.Kinematic;
        dockGrab.throwOnDetach = false;
        dockGrab.selectEntered.AddListener(_ => grabbedOnce = true);

        var bg = canvasGo.AddComponent<Image>();
        bg.color = MrUiTheme.Panel;
        MrUiTheme.Round(bg, 24);

        // ── 헤더 (장비명·진행상태) ──
        // 2026-08-24: 아직 실제 유지보수 데이터와 연결 안 됨 - 자리만 잡아둔 상태. 목록/상세
        // 팝업 쪽 데이터가 정리되면 이 텍스트를 그쪽 값으로 채우면 됨.
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(canvasGo.transform, false);
        var headerRect = headerGo.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f); headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
        headerRect.anchoredPosition = Vector2.zero;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(headerGo.transform, false);
        var title = titleGo.AddComponent<Text>();
        title.font = font; title.fontSize = 28; title.color = MrUiTheme.Ink;
        title.text = "작업 준비 중...";
        title.alignment = TextAnchor.UpperLeft;
        headerTitleText = title;
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(30f, 0f); titleRect.offsetMax = new Vector2(-30f, -10f);

        var statusGo = new GameObject("Status");
        statusGo.transform.SetParent(headerGo.transform, false);
        var status = statusGo.AddComponent<Text>();
        status.font = font; status.fontSize = 20; status.color = MrUiTheme.Warn;
        status.text = "";
        status.alignment = TextAnchor.LowerLeft;
        headerStatusText = status;
        var statusRect = status.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f); statusRect.anchorMax = new Vector2(1f, 0.5f);
        statusRect.offsetMin = new Vector2(30f, 6f); statusRect.offsetMax = new Vector2(-30f, 0f);

        // 닫기 버튼 - 헤더 맨 오른쪽 위, 누르면 통화 종료(요청: "닫기 누를시 통화 종료됨").
        var closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(headerGo.transform, false);
        var closeBg = closeGo.AddComponent<Image>();
        closeBg.color = MrUiTheme.Danger;
        MrUiTheme.Round(closeBg, 12);
        var closeRect = closeBg.rectTransform;
        closeRect.anchorMin = new Vector2(1f, 1f); closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(56f, 56f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(OnCloseClicked);

        var closeLabelGo = new GameObject("Label");
        closeLabelGo.transform.SetParent(closeGo.transform, false);
        var closeLabel = closeLabelGo.AddComponent<Text>();
        closeLabel.font = font; closeLabel.fontSize = 30; closeLabel.color = Color.white;
        closeLabel.text = "✕";
        closeLabel.alignment = TextAnchor.MiddleCenter;
        closeLabel.raycastTarget = false;
        var closeLabelRect = closeLabel.rectTransform;
        closeLabelRect.anchorMin = Vector2.zero; closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero; closeLabelRect.offsetMax = Vector2.zero;

        // ── 화상통화 영상 (크게) ──
        var videoGo = new GameObject("VideoArea");
        videoGo.transform.SetParent(canvasGo.transform, false);
        var videoRect = videoGo.AddComponent<RectTransform>();
        videoRect.anchorMin = new Vector2(0f, 1f); videoRect.anchorMax = new Vector2(1f, 1f);
        videoRect.pivot = new Vector2(0.5f, 1f);
        videoRect.sizeDelta = new Vector2(0f, VideoHeight);
        videoRect.anchoredPosition = new Vector2(0f, -HeaderHeight);
        var videoBg = videoGo.AddComponent<Image>();
        videoBg.color = new Color(0.08f, 0.09f, 0.1f);

        // 통화 경과시간 배지 - Update()에서 HandleCallAccepted가 찍어둔 callStartTime 기준으로
        // 매 프레임 다시 계산해서 채워넣는다.
        var callTimeGo = new GameObject("CallTimeBadge");
        callTimeGo.transform.SetParent(videoGo.transform, false);
        var callTimeBg = callTimeGo.AddComponent<Image>();
        callTimeBg.color = new Color(0f, 0f, 0f, 0.5f);
        MrUiTheme.Round(callTimeBg, 8);
        var callTimeBgRect = callTimeBg.rectTransform;
        callTimeBgRect.anchorMin = new Vector2(1f, 1f); callTimeBgRect.anchorMax = new Vector2(1f, 1f);
        callTimeBgRect.pivot = new Vector2(1f, 1f);
        callTimeBgRect.sizeDelta = new Vector2(110f, 40f);
        callTimeBgRect.anchoredPosition = new Vector2(-16f, -16f);

        var callTimeTextGo = new GameObject("Text");
        callTimeTextGo.transform.SetParent(callTimeGo.transform, false);
        callTimeText = callTimeTextGo.AddComponent<Text>();
        callTimeText.font = font; callTimeText.fontSize = 20; callTimeText.color = MrUiTheme.Danger;
        callTimeText.text = "00:00";
        callTimeText.alignment = TextAnchor.MiddleCenter;
        var callTimeTextRect = callTimeText.rectTransform;
        callTimeTextRect.anchorMin = Vector2.zero; callTimeTextRect.anchorMax = Vector2.one;
        callTimeTextRect.offsetMin = Vector2.zero; callTimeTextRect.offsetMax = Vector2.zero;

        // DockAgoraEventHandler.OnUserJoined가 JoinChannelVideoToken.MakeVideoView(...)로
        // 이 자식 밑에 실제 원격(AR) 영상 뷰를 만든다.
        var containerGo = new GameObject("VideoContainer");
        containerGo.transform.SetParent(videoGo.transform, false);
        videoContainer = containerGo.AddComponent<RectTransform>();
        videoContainer.anchorMin = Vector2.zero; videoContainer.anchorMax = Vector2.one;
        videoContainer.offsetMin = Vector2.zero; videoContainer.offsetMax = Vector2.zero;

        var spinnerGo = new GameObject("ConnectingSpinner");
        spinnerGo.transform.SetParent(videoGo.transform, false);
        var spinnerRect = spinnerGo.AddComponent<RectTransform>();
        spinnerRect.anchorMin = Vector2.zero; spinnerRect.anchorMax = Vector2.one;
        spinnerRect.offsetMin = Vector2.zero; spinnerRect.offsetMax = Vector2.zero;
        var spinnerBg = spinnerGo.AddComponent<Image>();
        spinnerBg.color = new Color(0f, 0f, 0f, 0.6f);
        var spinnerTextGo = new GameObject("Text");
        spinnerTextGo.transform.SetParent(spinnerGo.transform, false);
        var spinnerText = spinnerTextGo.AddComponent<Text>();
        spinnerText.font = font; spinnerText.fontSize = 24; spinnerText.color = MrUiTheme.InkDim;
        spinnerText.text = "연결 대기 중";
        spinnerText.alignment = TextAnchor.MiddleCenter;
        var spinnerTextRect = spinnerText.rectTransform;
        spinnerTextRect.anchorMin = Vector2.zero; spinnerTextRect.anchorMax = Vector2.one;
        spinnerTextRect.offsetMin = Vector2.zero; spinnerTextRect.offsetMax = Vector2.zero;
        connectingSpinner = spinnerGo;

        // "DropZone.cs도 이 진행 패널에 붙여야 해" - 정보공유 아이콘을 화상통화 영상 위로
        // 드래그하면 AR로 전송된다(DropZone.cs 자체 코멘트: "화상통화 영상 영역에 붙는 스크립트").
        // videoBg에 바로 붙이지 않는다 - DropZone.Start()가 dropZoneImage.color를 자기
        // normalColor(투명)로 덮어써서, 그대로 붙이면 영상 배경의 어두운 색이 사라져버린다.
        // 별도의 투명 오버레이를 맨 위(마지막 형제)에 얹어야 다른 자식(스피너/원격영상)에
        // 드롭이 가로채이지 않고 이 오버레이가 받는다.
        var dropOverlayGo = new GameObject("DropZoneOverlay");
        dropOverlayGo.transform.SetParent(videoGo.transform, false);
        var dropOverlayImg = dropOverlayGo.AddComponent<Image>();
        var dropOverlayRect = dropOverlayImg.rectTransform;
        dropOverlayRect.anchorMin = Vector2.zero; dropOverlayRect.anchorMax = Vector2.one;
        dropOverlayRect.offsetMin = Vector2.zero; dropOverlayRect.offsetMax = Vector2.zero;
        var dropZone = dropOverlayGo.AddComponent<DropZone>();
        dropZone.dropZoneImage = dropOverlayImg;

        var shareAlertGo = new GameObject("ShareAlert");
        shareAlertGo.transform.SetParent(videoGo.transform, false);
        var shareAlertBg = shareAlertGo.AddComponent<Image>();
        shareAlertBg.color = new Color(0f, 0f, 0f, 0.75f);
        MrUiTheme.Round(shareAlertBg, 10);
        var shareAlertRect = shareAlertBg.rectTransform;
        shareAlertRect.anchorMin = new Vector2(0.5f, 0f); shareAlertRect.anchorMax = new Vector2(0.5f, 0f);
        shareAlertRect.pivot = new Vector2(0.5f, 0f);
        shareAlertRect.sizeDelta = new Vector2(360f, 50f);
        shareAlertRect.anchoredPosition = new Vector2(0f, 16f);
        shareAlertGo.SetActive(false);

        var shareAlertTextGo = new GameObject("Text");
        shareAlertTextGo.transform.SetParent(shareAlertGo.transform, false);
        var shareAlertText = shareAlertTextGo.AddComponent<TMPro.TextMeshProUGUI>();
        // TMP_FontAsset은 이 파일이 쓰는 다른 UI(legacy Text)와 달리 직접 안 만든다 -
        // TextMeshProUGUI는 비워두면 TMP_Settings.defaultFontAsset을 알아서 쓴다.
        shareAlertText.fontSize = 20; shareAlertText.color = Color.white;
        shareAlertText.alignment = TMPro.TextAlignmentOptions.Center;
        var shareAlertTextRect = shareAlertText.rectTransform;
        shareAlertTextRect.anchorMin = Vector2.zero; shareAlertTextRect.anchorMax = Vector2.one;
        shareAlertTextRect.offsetMin = Vector2.zero; shareAlertTextRect.offsetMax = Vector2.zero;

        dropZone.alertPanel = shareAlertGo;
        dropZone.alertText = shareAlertText;

        // ── 탭바 ──
        var tabBarGo = new GameObject("TabBar");
        tabBarGo.transform.SetParent(canvasGo.transform, false);
        var tabBarRect = tabBarGo.AddComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0f, 1f); tabBarRect.anchorMax = new Vector2(1f, 1f);
        tabBarRect.pivot = new Vector2(0.5f, 1f);
        tabBarRect.sizeDelta = new Vector2(0f, TabBarHeight);
        tabBarRect.anchoredPosition = new Vector2(0f, -(HeaderHeight + VideoHeight));
        var tabBarLayout = tabBarGo.AddComponent<HorizontalLayoutGroup>();
        tabBarLayout.childForceExpandWidth = true; tabBarLayout.childForceExpandHeight = true;

        guideTabBtn = CreateTab(tabBarGo.transform, "작업가이드", font, out guideTabLabel, out guideUnderline);
        shareTabBtn = CreateTab(tabBarGo.transform, "파일공유·지시", font, out shareTabLabel, out shareUnderline);
        modelTabBtn = CreateTab(tabBarGo.transform, "3D모델", font, out modelTabLabel, out modelUnderline);
        drawTabBtn = CreateTab(tabBarGo.transform, "그리기", font, out drawTabLabel, out drawUnderline);
        guideTabBtn.onClick.AddListener(OnGuideTabClicked);
        shareTabBtn.onClick.AddListener(OnShareTabClicked);
        modelTabBtn.onClick.AddListener(OnModelTabClicked);
        drawTabBtn.onClick.AddListener(OnDrawTabClicked);

        // ── 탭 콘텐츠 (1단계라 자리만 - 다음 단계에서 실제 패널 연결) ──
        var contentAreaGo = new GameObject("ContentArea");
        contentAreaGo.transform.SetParent(canvasGo.transform, false);
        var contentAreaRect = contentAreaGo.AddComponent<RectTransform>();
        contentAreaRect.anchorMin = new Vector2(0f, 1f); contentAreaRect.anchorMax = new Vector2(1f, 1f);
        contentAreaRect.pivot = new Vector2(0.5f, 1f);
        contentAreaRect.sizeDelta = new Vector2(0f, ContentHeight);
        contentAreaRect.anchoredPosition = new Vector2(0f, -(HeaderHeight + VideoHeight + TabBarHeight));

        guideContent = CreateGuideTab(contentAreaGo.transform, out guideContentRect);
        shareContent = CreateShareTab(contentAreaGo.transform, font);
        modelContent = CreateModelTab(contentAreaGo.transform, font, out modelContentRect);
        drawContent = CreateDrawTab(contentAreaGo.transform, out drawContentRect);
    }

    // 5단계: "그리기" 탭 - 실시간 그리기(LiveDrawOverlay)는 화상통화 영상 위에 겹쳐 보여야 해서
    // 서페이스 자체는 그대로 카메라 앞에 남고, 토글 버튼만 이 자리로 옮겨온다(3D모델 탭과 동일한
    // 패턴 - 모델 본체는 안 옮기고 버튼만 옮기는 것과 같은 이유).
    GameObject CreateDrawTab(Transform parent, out RectTransform rect)
    {
        var go = new GameObject("DrawContent");
        go.transform.SetParent(parent, false);
        rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f); rect.offsetMax = new Vector2(-10f, -10f);
        go.SetActive(false);
        return go;
    }

    // 4단계: "3D모델" 탭 - 모델 본체는 여전히 카메라 앞 공간에 뜨고(잡기 상호작용 유지), 여기엔
    // ModelRotateTestPanel의 토글/분해도/포인터모드 버튼 세 개만 세로로 들어온다. 안내 문구는
    // 일부러 안 넣었다 - 버튼 세 개(0.83/0.5/0.17 앵커)와 겹칠 자리가 마땅치 않아서, 실기로
    // 배치 확인한 뒤 필요하면 여백에 추가.
    GameObject CreateModelTab(Transform parent, Font font, out RectTransform rect)
    {
        var go = new GameObject("ModelContent");
        go.transform.SetParent(parent, false);
        rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f); rect.offsetMax = new Vector2(-10f, -10f);
        go.SetActive(false);
        return go;
    }

    // 3단계: "작업가이드" 탭 - 새로 만들지 않고 빈 자리(RectTransform)만 잡아두면, Start()에서
    // WorkGuidePanel.Instance.SetEmbedParent(guideContentRect)가 기존 WorkGuidePanel 캔버스를
    // 여기로 옮겨붙인다. 실제 단계 텍스트/이전·다음·완료 버튼은 전부 WorkGuidePanel.cs 코드
    // 그대로 - 여기서는 자리만 제공.
    GameObject CreateGuideTab(Transform parent, out RectTransform rect)
    {
        var go = new GameObject("GuideContent");
        go.transform.SetParent(parent, false);
        rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f); rect.offsetMax = new Vector2(-10f, -10f);
        go.SetActive(false);
        return go;
    }

    // 2단계: "파일공유·지시" 탭 - 기존 InstructionDrawing을 코드로 새로 만들지 않고, 이미 씬에
    // 있는 걸 그대로 열기만 한다(그 안 펜/스티커/전송 UI는 손 안 댐). 열리는 팝업 자체의 위치는
    // 원래 씬에 있던 그대로라 이 도크와 겹칠 수 있음 - 실기 확인 후 위치 조정 필요할 수 있다.
    GameObject CreateShareTab(Transform parent, Font font)
    {
        var go = new GameObject("ShareContent");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(30f, 20f); rect.offsetMax = new Vector2(-30f, -20f);

        var hint = new GameObject("Hint");
        hint.transform.SetParent(go.transform, false);
        var hintText = hint.AddComponent<Text>();
        hintText.font = font; hintText.fontSize = 20; hintText.color = MrUiTheme.InkDim;
        hintText.text = "파일공유: 자료 아이콘을 화상통화 화면에 드래그하면 AR에 전송됩니다.\n지시하기: 현재 화면을 캡처해서 화살표·스티커로 짚어 AR에 전송합니다.";
        hintText.alignment = TextAnchor.UpperCenter;
        hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0.5f); hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.offsetMin = Vector2.zero; hintRect.offsetMax = Vector2.zero;

        // "파일공유·지시" 탭에 지시하기 버튼밖에 없다는 요청으로, 씬에 이미 있는 정보공유_상세
        // (InfoShareManager) 팝업을 여는 "파일공유" 버튼을 위쪽에 하나 더 쌓는다.
        CreateActionButton(go.transform, font, "FileShareBtn", "파일공유 열기", new Vector2(0f, 90f), OnOpenFileShareClicked);
        CreateActionButton(go.transform, font, "OpenInstructionBtn", "지시하기 열기", new Vector2(0f, 10f), OnOpenInstructionDrawingClicked);

        go.SetActive(false);
        return go;
    }

    // 하단 중앙에 쌓이는 260x70 크기의 알약형 버튼 - CreateShareTab의 두 버튼(파일공유/지시하기)이
    // 똑같은 모양이라 공용으로 뺐다.
    void CreateActionButton(Transform parent, Font font, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = MrUiTheme.AccentSoft;
        MrUiTheme.Round(btnImg, 16);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);
        var btnRect = btnImg.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0f); btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.sizeDelta = new Vector2(260f, 70f);
        btnRect.anchoredPosition = anchoredPosition;

        var btnLabelGo = new GameObject("Label");
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        var btnLabel = btnLabelGo.AddComponent<Text>();
        btnLabel.font = font; btnLabel.fontSize = 24; btnLabel.color = MrUiTheme.Ink;
        btnLabel.text = label;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.raycastTarget = false;
        var btnLabelRect = btnLabel.rectTransform;
        btnLabelRect.anchorMin = Vector2.zero; btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero; btnLabelRect.offsetMax = Vector2.zero;
    }

    void OnOpenFileShareClicked()
    {
        if (infoShareManager == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InfoShareManager 연결 안 됨 - 파일공유를 열 수 없음.");
            return;
        }
        infoShareManager.gameObject.SetActive(true); // OnEnable()에서 바로 파일 목록을 불러온다
    }

    void OnOpenInstructionDrawingClicked()
    {
        if (instructionDrawing == null)
        {
            Debug.LogWarning("[MrCallDockPanel] InstructionDrawing 연결 안 됨 - 지시하기를 열 수 없음.");
            return;
        }
        instructionDrawing.Open();
    }

    Button CreateTab(Transform parent, string label, Font font, out Text labelText, out Image underline)
    {
        var go = new GameObject("Tab_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // 클릭 영역용 - 배경은 투명
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        labelText = textGo.AddComponent<Text>();
        labelText.font = font; labelText.fontSize = 24;
        labelText.text = label;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;
        var textRect = labelText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.2f); textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        var underlineGo = new GameObject("Underline");
        underlineGo.transform.SetParent(go.transform, false);
        underline = underlineGo.AddComponent<Image>();
        underline.color = MrUiTheme.Accent;
        var underlineRect = underline.rectTransform;
        underlineRect.anchorMin = new Vector2(0.15f, 0f); underlineRect.anchorMax = new Vector2(0.85f, 0f);
        underlineRect.sizeDelta = new Vector2(0f, 3f);
        underlineRect.anchoredPosition = new Vector2(0f, 8f);

        return btn;
    }
}
