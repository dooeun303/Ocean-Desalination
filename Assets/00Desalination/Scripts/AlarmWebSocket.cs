using System;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;   // ← 추가


// 알람 웹소켓을 연결하는 스크립트
public class AlarmWebSocket : MonoBehaviour
{
    // 알람받았음을 감지하는 이벤트
    public static event Action<AlarmData> OnAlarmReceived;
    
    private WebSocketSharp.WebSocket ws;
    private AlarmData pendingAlarm = null;
    private readonly object lockObj = new object();

    public static AlarmWebSocket Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Connect();
    }

    // 2026-10-21: 예전엔 시작할 때 한 번만 연결해서, 서버 재시작/와이파이 끊김으로 한 번 끊기면 앱을 다시 켤
    // 때까지 끊긴 채였다(파일공유 "[WS] 전송 실패 — 연결 안 됨"). 끊겨 있으면 Update에서 3초마다 다시 연결한다.
    const float ReconnectInterval = 3f;
    float _nextReconnectAt;

    void Connect()
    {
        // 웹소켓 서버에 연결 (플랫폼은 MR로)
        ws = new WebSocketSharp.WebSocket(ServerConfig.WsBaseUrl + "?platform=MR");

        // 연결 이벤트
        ws.OnOpen += (s, e) => Debug.Log("[WS] 연결 성공");
        ws.OnError += (s, e) => Debug.LogError("[WS] 오류: " + e.Message);
        ws.OnClose += (s, e) => Debug.Log("[WS] 연결 종료");

        // 메시지 수신 이벤트
        ws.OnMessage += (s, e) =>
        {
            try
            {
                Debug.Log("[WS] 수신: " + e.Data);

                // JsonUtility 대신 Newtonsoft 사용
                AlarmData alarm = JsonConvert.DeserializeObject<AlarmData>(e.Data);

                if (alarm != null && alarm.type == "ALARM")
                {
                    lock (lockObj) { pendingAlarm = alarm; }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[WS] 파싱 오류: " + ex.Message);
            }
        };

        ws.ConnectAsync();
    }

    void Update()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Closed && Time.unscaledTime >= _nextReconnectAt)
        {
            _nextReconnectAt = Time.unscaledTime + ReconnectInterval;
            Debug.Log("[WS] 연결 끊김 - 다시 연결 시도");
            Connect();
        }

        lock (lockObj)
        {
            if (pendingAlarm != null)
            {
                OnAlarmReceived?.Invoke(pendingAlarm);
                pendingAlarm = null;
            }
        }
    }

    public bool Send(string message)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send(message);
            Debug.Log("[WS] 전송: " + message);
            return true;
        }

        // 이 연결이 끊겨 있으면 통화용 웹소켓으로 대신 보낸다 - 서버(ws.js)는 메시지 type으로 전달 대상을
        // 정하므로 같은 MR 플랫폼 연결이면 어느 쪽으로 보내도 AR에 간다.
        if (VideoCallSignalingMR.Instance != null && VideoCallSignalingMR.Instance.SendRaw(message))
        {
            Debug.Log("[WS] 알람 연결이 끊겨 통화 연결로 대신 전송: " + message);
            return true;
        }

        Debug.LogWarning("[WS] 전송 실패 — 연결 안 됨");
        return false;
    }

    void OnDestroy()
    {
        ws?.Close();
    }
}