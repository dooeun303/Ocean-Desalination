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
}
