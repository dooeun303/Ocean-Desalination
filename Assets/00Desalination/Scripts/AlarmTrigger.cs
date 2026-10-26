using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

/// <summary>
/// 알람발생기의 버튼을 작동시키는 스크립트
/// 노드 서버에 api/alarm/trigger/start or stop 을 post 요청하여 
/// </summary>
public class AlarmTrigger : MonoBehaviour
{
    public TMP_Text logText;


    public void OnPressedStart()
    {
        StartCoroutine(SendRequest(ServerConfig.BaseUrl + "/api/alarms", "POST"));
        Debug.Log("[Trigger] 알람 발생 시작 버튼 누름");
        logText.text = "[Trigger] 알람 발생 시작 버튼 누름";
    }

    public void OnPressedStop()
    {
        StartCoroutine(SendRequest(ServerConfig.BaseUrl + "/api/alarms/active", "DELETE"));

        Debug.Log("[Trigger] 알람 발생 중지 버튼 누름");
        logText.text = "[Trigger] 알람 발생 중지 버튼 누름";

    }

    private IEnumerator SendRequest(string url, string method = "POST")
    {
        UnityWebRequest req;
        if (method == "DELETE")
        {
            req = UnityWebRequest.Delete(url);
            req.downloadHandler = new DownloadHandlerBuffer();
        }
        else
        {
            req = UnityWebRequest.PostWwwForm(url, "");
        }
        req.timeout = ServerConfig.RequestTimeout;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Trigger] 요청 성공: " + req.downloadHandler.text);
            logText.text = $"[Trigger] 요청 성공: {req.downloadHandler.text}";

        }
        else
        {
            Debug.Log("[Trigger] 요청 실패: " + req.error);
            logText.text = $"[Trigger] 요청 실패: {req.error}";
        }
    }
}
