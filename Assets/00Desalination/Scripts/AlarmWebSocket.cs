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
        // 웹소켓 서버에 연결 (플랫폼은 MR로)
        ws = new WebSocketSharp.WebSocket("ws://192.168.0.66:3000?platform=MR");

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
        lock (lockObj)
        {
            if (pendingAlarm != null)
            {
                OnAlarmReceived?.Invoke(pendingAlarm);
                pendingAlarm = null;
            }
        }
    }

    public void Send(string message)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send(message);
            Debug.Log("[WS] 전송: " + message);
        }
        else
        {
            Debug.LogWarning("[WS] 전송 실패 — 연결 안 됨");
        }
    }

    void OnDestroy()
    {
        ws?.Close();
    }
}