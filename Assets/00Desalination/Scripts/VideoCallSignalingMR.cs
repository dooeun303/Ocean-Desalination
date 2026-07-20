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

    // 통화 수락/거절 이벤트 (channelName 전달)
    public static event Action<string> OnCallAccepted;
    public static event Action<string> OnCallRejected;

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
                if (msg != null && msg.type == "call_response")
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
                if (_pendingMessage.accepted)
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
    public string type;         // "call_request" 또는 "call_response"
    public string channelName;  // 통화방 이름 (equipment_id 등)
    public bool accepted;       // call_response 일 때만 사용
}
