using System;
using System.Collections.Concurrent;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

// ─────────────────────────────────────────────────────────────
// 화상통화 요청/수신 웹소켓 스크립트 (MR)
// AlarmWebSocket.cs 와 동일한 방식으로 서버(ws://192.168.0.66:3000)에 연결하고,
// 화상통화·작업 진행·실시간 지시 메시지를 주고받는다(종류는 WsMessageType).
// ─────────────────────────────────────────────────────────────
public class VideoCallSignalingMR : MonoBehaviour
{
    public static VideoCallSignalingMR Instance;

    public string serverUrl => ServerConfig.WsBaseUrl + "?platform=MR";

    // 통화 수락/거절/종료 이벤트 (channelName 전달)
    public static event Action<string> OnCallAccepted;
    public static event Action<string> OnCallRejected;
    public static event Action<string> OnCallEnded; // AR이 통화를 끊었을 때

    // AR이 거는 지원 요청(SupportCallListPanel)을 MR에서 수락했을 때도 MrCallDockPanel 등
    // 기존 OnCallAccepted 구독자가 똑같이 반응하도록, 이 통로로 같은 이벤트를 대신 발화해준다.
    // event는 선언한 클래스 밖에서 직접 Invoke할 수 없어서 이 public 메서드로 감싼다.
    // 2026-08-26: "도크 닫기 버튼 누르면 통화 종료" 요청 대응으로 callId도 같이 기억해둔다 -
    // support_call.js REST(/end)를 호출하려면 channelName이 아니라 remote_support 테이블의 id가 필요.
    public static string CurrentSupportCallId { get; private set; }
    public static void RaiseCallAccepted(string channelName, string callId = null)
    {
        CurrentSupportCallId = callId;
        OnCallAccepted?.Invoke(channelName);
    }

    private WebSocketSharp.WebSocket _ws;

    // 2026-10-23: 예전엔 메시지 종류마다 보관 칸이 하나뿐이라(_pendingMessage 등), 같은 종류가 한 프레임에
    // 두 개 오면(call_response 직후 call_end, 지원요청 연달아 등) 앞의 것이 덮어써져 사라졌다. 수신 스레드가
    // 받은 순서대로 큐에 넣고, Update(메인 스레드)에서 전부 꺼내 순서대로 처리한다.
    private readonly ConcurrentQueue<CallSignalMessage> _inbox = new ConcurrentQueue<CallSignalMessage>();

    // 2026-10-23: 예전엔 시작할 때 한 번만 연결해서, 서버 재시작/와이파이 끊김으로 끊기면 앱을 다시 켤 때까지
    // AR의 통화 종료·지원요청·작업종료 신호를 못 받았다(도크가 열린 채 남음). AlarmWebSocket과 같은 방식으로
    // 끊겨 있으면 Update에서 3초마다 다시 연결한다. 끊겨 있던 동안 온 메시지는 서버가 다시 보내주지 않는다.
    const float ReconnectInterval = 3f;
    float _nextReconnectAt;

    // 싱글톤
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Connect();
    }

    // ─────────────────────────────────────────────
    // 웹소켓 연결
    // ─────────────────────────────────────────────
    private void Connect()
    {
        _ws = new WebSocketSharp.WebSocket(serverUrl);

        _ws.OnOpen += (s, e) => Debug.Log("[CallWS-MR] 연결 성공");
        _ws.OnError += (s, e) => Debug.LogError("[CallWS-MR] 오류: " + e.Message);
        _ws.OnClose += (s, e) => Debug.Log("[CallWS-MR] 연결 종료");

        _ws.OnMessage += (s, e) =>
        {
            try
            {
                Debug.Log("[CallWS-MR] 수신: " + e.Data);

                var msg = JsonConvert.DeserializeObject<CallSignalMessage>(e.Data);
                if (msg != null) _inbox.Enqueue(msg); // 종류 판별/발화는 Update(메인 스레드)에서
            }
            catch (Exception ex)
            {
                Debug.LogError("[CallWS-MR] 파싱 오류: " + ex.Message);
            }
        };

        _ws.ConnectAsync();
    }

    void Update()
    {
        if (_ws != null && _ws.ReadyState == WebSocketState.Closed && Time.unscaledTime >= _nextReconnectAt)
        {
            _nextReconnectAt = Time.unscaledTime + ReconnectInterval;
            Debug.Log("[CallWS-MR] 연결 끊김 - 다시 연결 시도");
            Connect();
        }

        while (_inbox.TryDequeue(out var msg))
        {
            // 구독자 하나가 예외를 던져도 나머지 메시지 처리는 계속되게 한다.
            try { Dispatch(msg); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    void Dispatch(CallSignalMessage msg)
    {
        switch (msg.type)
        {
            case WsMessageType.CallResponse:
                if (msg.accepted) OnCallAccepted?.Invoke(msg.channelName);
                else OnCallRejected?.Invoke(msg.channelName);
                break;
            case WsMessageType.CallEnd:
                OnCallEnded?.Invoke(msg.channelName);
                break;
            case WsMessageType.GuideStepControl:
                OnGuideStepControlReceived?.Invoke(msg.stepIndex, msg.stepCount);
                break;
            case WsMessageType.MaintenanceComplete:
                OnMaintenanceComplete?.Invoke();
                break;
            case WsMessageType.SupportCallRequest:
                if (!string.IsNullOrEmpty(msg.id)) OnSupportCallRequested?.Invoke(msg.id);
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 통화 요청 전송 (videoCallButton 등에서 호출)
    // ─────────────────────────────────────────────
    public void RequestCall(string channelName)
    {
        var msg = new CallSignalMessage
        {
            type = WsMessageType.CallRequest,
            channelName = channelName
        };

        string json = JsonConvert.SerializeObject(msg);

        if (_ws != null && _ws.ReadyState == WebSocketState.Open)
        {
            _ws.Send(json);
            Debug.Log("[CallWS-MR] 통화 요청 전송: " + json);
        }
        else
        {
            Debug.LogWarning("[CallWS-MR] 전송 실패 — 연결 안 됨");
        }
    }

    // ─────────────────────────────────────────────
    // 작업 가이드 단계 수신 (2026-08-26: "AR이 넘기면 MR이 자동으로 따라가야 함, MR 이전/다음/
    // 완료 버튼은 필요없어" - 예전엔 MR이 SendGuideStep()으로 AR에 보내던 방향이었는데 반대로
    // 바꿨다. AR쪽 InmoVideoCallSignaling.SendGuideStepControl()이 보낸 걸 WorkGuidePanel이
    // 구독해서 그대로 따라간다.)
    // ─────────────────────────────────────────────
    public static event Action<int, int> OnGuideStepControlReceived; // (stepIndex 1-based, stepCount)

    // 2026-08-26: "AR에서 저장 완료하면 MR도 진행 패널을 종료하고 설비 깜빡임도 사라져야 해" -
    // 통화 상태와 무관하게 항상 오는 신호라 통화 이벤트와 별개로 둔다.
    public static event Action OnMaintenanceComplete;

    // 2026-10-20: AR이 원격지원을 요청하면 서버가 { type: "support_call_request", id } 를 MR에 보낸다 -
    // 예전엔 무시하고 목록을 열어야만 알 수 있었다. SupportCallRequestAlert가 구독해서 알림 팝업을 띄운다.
    public static event Action<string> OnSupportCallRequested; // remote_support.id

    // ─────────────────────────────────────────────
    // 실시간 그리기: 새 선 시작 / 점 추가 / 선 끝 (LiveDrawOverlay에서 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnDrawStart/OnDrawPoint/OnDrawEnd로 수신됨.
    // ─────────────────────────────────────────────
    public void SendDrawStart(string strokeId, string colorHex, float widthNorm)
    {
        Send(new CallSignalMessage { type = WsMessageType.DrawStart, strokeId = strokeId, drawColor = colorHex, drawWidth = widthNorm });
    }

    public void SendDrawPoint(string strokeId, float x, float y)
    {
        Send(new CallSignalMessage { type = WsMessageType.DrawPoint, strokeId = strokeId, drawX = x, drawY = y });
    }

    public void SendDrawEnd(string strokeId)
    {
        Send(new CallSignalMessage { type = WsMessageType.DrawEnd, strokeId = strokeId });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 회전 스트리밍 (모델 회전 테스트 패널에서 드래그로 회전시키는 동안 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnModelRotateReceived로 수신됨.
    // ─────────────────────────────────────────────
    public void SendModelRotation(Quaternion rotation)
    {
        Send(new CallSignalMessage { type = WsMessageType.ModelRotate, rotX = rotation.x, rotY = rotation.y, rotZ = rotation.z, rotW = rotation.w });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 위 포인팅/지시 (모델 회전 테스트 패널에서 컨트롤러 레이로 모델을 가리키는 동안 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnModelPointReceived로 수신됨. localPoint는 modelRoot 기준
    // 로컬좌표 - 양쪽이 같은 정규화 스케일을 쓰므로 그대로 대응된다. localNormal은 히트 지점의
    // 표면 법선(로컬 방향) - AR쪽 포인터 마커를 표면 안쪽에 파묻히지 않고 바깥으로 띄워서
    // 그리는 데 쓴다(실기 확인: 법선 없이 "모델 중심→히트 지점" 방향으로 근사했더니 이 펌프처럼
    // 중심 기준 대칭이 아닌 형태에서는 여전히 파묻혀 보였음).
    // ─────────────────────────────────────────────
    public void SendModelPoint(bool pointing, string partName, Vector3 localPoint, Vector3 localNormal)
    {
        Send(new CallSignalMessage
        {
            type = WsMessageType.ModelPoint,
            pointing = pointing,
            partName = partName,
            pointX = localPoint.x,
            pointY = localPoint.y,
            pointZ = localPoint.z,
            normX = localNormal.x,
            normY = localNormal.y,
            normZ = localNormal.z
        });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 분해도 토글 (모델 회전 테스트 패널에서 "분해/조립" 버튼 누를 때 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnModelExplodeReceived로 수신됨. 애니메이션 자체는
    // 이 신호를 받은 쪽이 각자 재생한다(중간값을 계속 스트리밍하지 않음).
    // ─────────────────────────────────────────────
    public void SendModelExplode(bool exploded)
    {
        Send(new CallSignalMessage { type = WsMessageType.ModelExplode, exploded = exploded });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 창 열림/닫힘 (ModelRotateTestPanel에서 "모델 회전 테스트"를 열거나 닫을 때, 그리고
    // "3D모델" 탭을 벗어나거나 다시 들어올 때도 호출) - AR쪽 InmoVideoCallSignaling.OnModelViewChanged로
    // 수신됨. open=false일 땐 modelUrl을 안 보내도 된다.
    // ─────────────────────────────────────────────
    public void SendModelView(bool open, string modelUrl = null)
    {
        Send(new CallSignalMessage { type = WsMessageType.ModelView, modelViewOpen = open, modelUrl = modelUrl });
    }

    // 이미 JSON으로 만든 메시지를 그대로 보낸다(AlarmWebSocket이 끊겼을 때 파일공유 등 대신 전송용).
    public bool SendRaw(string json)
    {
        if (_ws == null || _ws.ReadyState != WebSocketState.Open) return false;
        _ws.Send(json);
        return true;
    }

    void Send(CallSignalMessage msg)
    {
        string json = JsonConvert.SerializeObject(msg);
        if (_ws != null && _ws.ReadyState == WebSocketState.Open)
        {
            _ws.Send(json);
        }
        else
        {
            Debug.LogWarning("[CallWS-MR] 전송 실패 — 연결 안 됨");
        }
    }

    // ─────────────────────────────────────────────
    // 통화 종료 전송 (내가 통화를 끊을 때 호출)
    // ─────────────────────────────────────────────
    public void EndCall(string channelName)
    {
        var msg = new CallSignalMessage
        {
            type = WsMessageType.CallEnd,
            channelName = channelName
        };

        string json = JsonConvert.SerializeObject(msg);

        if (_ws != null && _ws.ReadyState == WebSocketState.Open)
        {
            _ws.Send(json);
            Debug.Log("[CallWS-MR] 통화 종료 전송: " + json);
        }
        else
        {
            Debug.LogWarning("[CallWS-MR] 전송 실패 — 연결 안 됨");
        }
    }

    // 2026-08-26: MR 도크의 닫기 버튼처럼 "내가(MR이) 먼저 통화를 끊는" 경우 전용 - AR에는 기존
    // EndCall()로 WS만 보내면 되지만, MR 자기 자신의 OnCallEnded 구독자(MrCallDockPanel/
    // ModelRotateTestPanel/WorkGuidePanel/LiveDrawOverlay)는 "AR이 끊었을 때"만 반응하도록
    // 짜여 있어서(네트워크로 받은 call_end만 그 이벤트를 발화함) 로컬에서 직접 끊을 땐 아무도
    // 정리가 안 된다. 그래서 같은 이벤트를 로컬에서도 발화해 기존 정리 로직을 그대로 재사용한다.
    public void EndCallAsInitiator(string channelName)
    {
        EndCall(channelName);
        OnCallEnded?.Invoke(channelName);
        CurrentSupportCallId = null;
    }

    void OnDestroy()
    {
        _ws?.Close();
    }
}

// ─────────────────────────────────────────────
// 통화 요청/응답 메시지 모델
// (AR쪽 VideoCallSignalingAR.cs 와 형식을 맞춰야 함)
// ─────────────────────────────────────────────
[System.Serializable]
public class CallSignalMessage
{
    public string type;         // WsMessageType 중 하나
    public string channelName;  // 통화방 이름 (equipment_id 등)
    public bool accepted;       // call_response 일 때만 사용
    public int stepIndex;       // guide_step_control 일 때만 사용 (1부터 시작)
    public int stepCount;       // guide_step_control 일 때만 사용
    public string strokeId;     // draw_start/draw_point/draw_end 공용
    public string drawColor;    // draw_start 전용 - "#RRGGBB"
    public float drawWidth;     // draw_start 전용 - 캔버스 너비 대비 정규화된 두께
    public float drawX;         // draw_point 전용 - 0..1 정규화
    public float drawY;         // draw_point 전용 - 0..1 정규화
    public float rotX;          // model_rotate 전용 - 쿼터니언 x
    public float rotY;          // model_rotate 전용 - 쿼터니언 y
    public float rotZ;          // model_rotate 전용 - 쿼터니언 z
    public float rotW;          // model_rotate 전용 - 쿼터니언 w
    public bool pointing;       // model_point 전용 - 현재 모델을 가리키고 있는지
    public string partName;     // model_point 전용 - 맞은 부위(GLB 노드) 이름
    public float pointX;        // model_point 전용 - modelRoot 로컬좌표
    public float pointY;        // model_point 전용 - modelRoot 로컬좌표
    public float pointZ;        // model_point 전용 - modelRoot 로컬좌표
    public float normX;         // model_point 전용 - 히트 지점 표면 법선(로컬 방향)
    public float normY;         // model_point 전용 - 히트 지점 표면 법선(로컬 방향)
    public float normZ;         // model_point 전용 - 히트 지점 표면 법선(로컬 방향)
    public bool exploded;       // model_explode 전용 - 분해도 펼침 여부
    public bool modelViewOpen;  // model_view 전용 - 3D 모델 창이 열렸는지
    public string modelUrl;     // model_view 전용 - open=true일 때 AR이 불러올 glb URL
    public string id;           // support_call_request 전용 - remote_support.id (서버 support_call.js POST가 보냄)
}
