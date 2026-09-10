using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

/// <summary>
/// �˶��߻����� ��ư�� �۵���Ű�� ��ũ��Ʈ
/// ��� ������ api/alarm/trigger/start or stop �� post ��û�Ͽ� 
/// </summary>
public class AlarmTrigger : MonoBehaviour
{
    public TMP_Text logText;


    public void OnPressedStart()
    {
        StartCoroutine(SendRequest(ServerConfig.BaseUrl + "/api/alarms", "POST"));
        Debug.Log("[Trigger] �˶� �߻� ���� ��ư ����");
        logText.text = "[Trigger] �˶� �߻� ���� ��ư ����";
    }

    public void OnPressedStop()
    {
        StartCoroutine(SendRequest(ServerConfig.BaseUrl + "/api/alarms/active", "DELETE"));

        Debug.Log("[Trigger] �˶� �߻� ���� ��ư ����");
        logText.text = "[Trigger] �˶� �߻� ���� ��ư ����";

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
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Trigger] ��û ����: " + req.downloadHandler.text);
            logText.text = $"[Trigger] ��û ����: {req.downloadHandler.text}";

        }
        else
        {
            Debug.Log("[Trigger] ��û ����: " + req.error);
            logText.text = $"[Trigger] ��û ����: {req.error}";
        }
    }
}
