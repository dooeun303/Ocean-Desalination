using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 알람발생기의 버튼을 작동시키는 스크립트
/// 노드 서버에 api/alarm/trigger/start or stop 을 post 요청하여 
/// </summary>
public class AlarmTrigger : MonoBehaviour
{
    public TMP_Text logText;


    public void OnPressedStart()
    {
        StartCoroutine(SendRequest(AlarmApi.StartTrigger));
        Debug.Log("[Trigger] 알람 발생 시작 버튼 누름");
        logText.text = "[Trigger] 알람 발생 시작 버튼 누름";
    }

    public void OnPressedStop()
    {
        StartCoroutine(SendRequest(AlarmApi.StopTrigger));

        Debug.Log("[Trigger] 알람 발생 중지 버튼 누름");
        logText.text = "[Trigger] 알람 발생 중지 버튼 누름";

    }

    // request = AlarmApi.StartTrigger / StopTrigger - 결과(서버 메시지 또는 실패 사유)를 로그와 화면에 남긴다.
    private IEnumerator SendRequest(System.Func<System.Action<string>, System.Action<string>, IEnumerator> request)
    {
        yield return request(
            message =>
            {
                Debug.Log("[Trigger] 요청 성공: " + message);
                logText.text = $"[Trigger] 요청 성공: {message}";
            },
            error =>
            {
                Debug.Log("[Trigger] 요청 실패: " + error);
                logText.text = $"[Trigger] 요청 실패: {error}";
            });
    }
}
