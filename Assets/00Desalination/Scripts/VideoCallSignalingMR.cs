using System;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

// ─────────────────────────────────────────────────────────────
// 화상통화 요청/수신 웹소켓 스크립트 (MR)
// AlarmWebSocket.cs 와 동일한 방식으로 서버(ws://192.168.0.66:3000)에 연결하고,
// "call_request" / "call_response" type의 메시지만 별도로 주고받는다.
// ─────────────────────────────────────────────────────────────
public class VideoCallSignalingMR : MonoBehaviour
{
    public static VideoCallSignalingMR Instance;

    [Header("서버 주소")]
    public string serverUrl = "ws://192.168.0.66:3000?platform=MR";

    // 통화 수락/거절/종료 이벤트 (channelName 전달)
    public static event Action<string> OnCallAccepted;
    public static event Action<string> OnCallRejected;
    public static event Action<string> OnCallEnded; // AR이 통화를 끊었을 때

    private WebSocketSharp.WebSocket _ws;
    private CallSignalMessage _pendingMessage = null;
    private readonly object _lockObj = new object();

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
                if (msg != null && (msg.type == "call_response" || msg.type == "call_end"))
                {
                    lock (_lockObj) { _pendingMessage = msg; }
                }
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
        lock (_lockObj)
        {
            if (_pendingMessage != null)
            {
                if (_pendingMessage.type == "call_end")
                    OnCallEnded?.Invoke(_pendingMessage.channelName);
                else if (_pendingMessage.accepted)
                    OnCallAccepted?.Invoke(_pendingMessage.channelName);
                else
                    OnCallRejected?.Invoke(_pendingMessage.channelName);

                _pendingMessage = null;
            }
        }
    }

    // ─────────────────────────────────────────────
    // 통화 요청 전송 (videoCallButton 등에서 호출)
    // ─────────────────────────────────────────────
    public void RequestCall(string channelName)
    {
        var msg = new CallSignalMessage
        {
            type = "call_request",
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
    // 작업 가이드 현재 단계 전송 (작업 가이드 패널에서 이전/다음 클릭 시 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnGuideStepReceived로 수신됨.
    // ─────────────────────────────────────────────
    public void SendGuideStep(string channelName, int stepIndex, int stepCount, string arText)
    {
        var msg = new CallSignalMessage
        {
            type = "guide_step",
            channelName = channelName,
            stepIndex = stepIndex,
            stepCount = stepCount,
            arText = arText
        };

        string json = JsonConvert.SerializeObject(msg);

        if (_ws != null && _ws.ReadyState == WebSocketState.Open)
        {
            _ws.Send(json);
            Debug.Log("[CallWS-MR] 작업 가이드 단계 전송: " + json);
        }
        else
        {
            Debug.LogWarning("[CallWS-MR] 전송 실패 — 연결 안 됨");
        }
    }

    // ─────────────────────────────────────────────
    // 실시간 그리기: 새 선 시작 / 점 추가 / 선 끝 (LiveDrawOverlay에서 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnDrawStart/OnDrawPoint/OnDrawEnd로 수신됨.
    // ─────────────────────────────────────────────
    public void SendDrawStart(string strokeId, string colorHex, float widthNorm)
    {
        Send(new CallSignalMessage { type = "draw_start", strokeId = strokeId, drawColor = colorHex, drawWidth = widthNorm });
    }

    public void SendDrawPoint(string strokeId, float x, float y)
    {
        Send(new CallSignalMessage { type = "draw_point", strokeId = strokeId, drawX = x, drawY = y });
    }

    public void SendDrawEnd(string strokeId)
    {
        Send(new CallSignalMessage { type = "draw_end", strokeId = strokeId });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 회전 스트리밍 (모델 회전 테스트 패널에서 드래그로 회전시키는 동안 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnModelRotateReceived로 수신됨.
    // ─────────────────────────────────────────────
    public void SendModelRotation(Quaternion rotation)
    {
        Send(new CallSignalMessage { type = "model_rotate", rotX = rotation.x, rotY = rotation.y, rotZ = rotation.z, rotW = rotation.w });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 위 포인팅/지시 (모델 회전 테스트 패널에서 컨트롤러 레이로 모델을 가리키는 동안 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnModelPointReceived로 수신됨. localPoint는 modelRoot 기준
    // 로컬좌표 - 양쪽이 같은 정규화 스케일을 쓰므로 그대로 대응된다.
    // ─────────────────────────────────────────────
    public void SendModelPoint(bool pointing, string partName, Vector3 localPoint)
    {
        Send(new CallSignalMessage { type = "model_point", pointing = pointing, partName = partName, pointX = localPoint.x, pointY = localPoint.y, pointZ = localPoint.z });
    }

    // ─────────────────────────────────────────────
    // 3D 모델 분해도 토글 (모델 회전 테스트 패널에서 "분해/조립" 버튼 누를 때 호출)
    // AR쪽 InmoVideoCallSignaling.cs의 OnModelExplodeReceived로 수신됨. 애니메이션 자체는
    // 이 신호를 받은 쪽이 각자 재생한다(중간값을 계속 스트리밍하지 않음).
    // ─────────────────────────────────────────────
    public void SendModelExplode(bool exploded)
    {
        Send(new CallSignalMessage { type = "model_explode", exploded = exploded });
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
            type = "call_end",
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
    public string type;         // "call_request" / "call_response" / "guide_step" 등
    public string channelName;  // 통화방 이름 (equipment_id 등)
    public bool accepted;       // call_response 일 때만 사용
    public int stepIndex;       // guide_step 일 때만 사용 (1부터 시작)
    public int stepCount;       // guide_step 일 때만 사용
    public string arText;       // guide_step 일 때만 사용 - AR에 표시할 한 문장
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
    public bool exploded;       // model_explode 전용 - 분해도 펼침 여부
}
