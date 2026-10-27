using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// MR 앱의 모든 서버 HTTP 호출이 지나가는 곳(AR 프로젝트의 ApiClient와 같은 구조). 화면 코드는 UnityWebRequest를
// 직접 쓰지 않고 도메인별 API(SupportCallApi, AlarmApi …)를 부르고, 그 API들이 이 클래스를 쓴다.
// 여기서 한 번에 처리하는 것: 서버 주소(ServerConfig.BaseUrl), JSON 직렬화(Newtonsoft), 타임아웃(ServerConfig),
// 로그인 토큰(AuthSession.AuthHeader), 실패 메시지(서버가 준 message/error가 있으면 그것).
// 모두 코루틴이라 호출부에서 StartCoroutine / yield return 으로 돌린다.
public static class ApiClient
{
    // "/api/..." 같은 경로는 우리 서버 주소를 붙이고, "http..."로 시작하면 그대로 쓴다.
    public static string Url(string pathOrUrl) =>
        pathOrUrl.StartsWith("http") ? pathOrUrl : ServerConfig.BaseUrl + pathOrUrl;

    public static IEnumerator Get<T>(string path, Action<T> onOk, Action<string> onFail, int timeout = ServerConfig.RequestTimeout)
    {
        using var req = UnityWebRequest.Get(Url(path));
        yield return Run(req, timeout, onOk, onFail);
    }

    // POST/PATCH/PUT/DELETE. body가 null이 아니면 JSON으로 보낸다.
    public static IEnumerator Send<T>(string method, string path, object body, Action<T> onOk, Action<string> onFail,
        int timeout = ServerConfig.RequestTimeout)
    {
        using var req = new UnityWebRequest(Url(path), method) { downloadHandler = new DownloadHandlerBuffer() };
        if (body != null)
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
            req.SetRequestHeader("Content-Type", "application/json");
        }
        yield return Run(req, timeout, onOk, onFail);
    }

    // 응답 본문이 필요 없을 때(성공/실패만)
    public static IEnumerator Send(string method, string path, object body, Action onOk, Action<string> onFail,
        int timeout = ServerConfig.RequestTimeout)
    {
        yield return Send<object>(method, path, body, _ => onOk?.Invoke(), onFail, timeout);
    }

    // multipart/form-data 업로드(음성·이미지 파일 등)
    public static IEnumerator PostForm<T>(string path, WWWForm form, Action<T> onOk, Action<string> onFail,
        int timeout = ServerConfig.LongRequestTimeout)
    {
        using var req = UnityWebRequest.Post(Url(path), form);
        yield return Run(req, timeout, onOk, onFail);
    }

    // 큰 파일을 메모리에 올리지 않고 그대로 스트리밍 업로드(녹화 mp4 등). 업로드는 오래 걸릴 수 있어 타임아웃 없음.
    public static IEnumerator PostFile(string path, string filePath, string contentType, Action onOk, Action<string> onFail)
    {
        using var req = new UnityWebRequest(Url(path), "POST")
        {
            uploadHandler = new UploadHandlerFile(filePath),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        req.SetRequestHeader("Content-Type", contentType);
        yield return Run<object>(req, 0, _ => onOk?.Invoke(), onFail);
    }

    public static IEnumerator GetTexture(string url, Action<Texture2D> onOk, Action<string> onFail,
        int timeout = ServerConfig.LongRequestTimeout)
    {
        using var req = UnityWebRequestTexture.GetTexture(Url(url));
        AddAuth(req);
        req.timeout = timeout;
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { onFail?.Invoke(req.error); yield break; }
        onOk?.Invoke(DownloadHandlerTexture.GetContent(req));
    }

    static IEnumerator Run<T>(UnityWebRequest req, int timeout, Action<T> onOk, Action<string> onFail)
    {
        AddAuth(req);
        req.timeout = timeout;
        yield return req.SendWebRequest();

        string text = req.downloadHandler?.text;
        if (req.result != UnityWebRequest.Result.Success)
        {
            onFail?.Invoke(ErrorMessage(req.error, text));
            yield break;
        }
        if (typeof(T) == typeof(object)) { onOk?.Invoke(default); yield break; }

        T value;
        try { value = JsonConvert.DeserializeObject<T>(text); }
        catch (Exception e) { onFail?.Invoke("응답 해석 실패: " + e.Message); yield break; }
        if (value == null) { onFail?.Invoke("빈 응답"); yield break; }
        onOk?.Invoke(value);
    }

    static void AddAuth(UnityWebRequest req)
    {
        if (AuthSession.IsLoggedIn) req.SetRequestHeader("Authorization", AuthSession.AuthHeader);
    }

    // 서버가 { success:false, message } 또는 { error } 로 이유를 주면 그 이유를, 아니면 통신 오류 문구를 돌려준다.
    static string ErrorMessage(string transportError, string body)
    {
        if (string.IsNullOrEmpty(body)) return transportError;
        try
        {
            var e = JsonConvert.DeserializeObject<ErrorBody>(body);
            string reason = !string.IsNullOrEmpty(e?.message) ? e.message : e?.error;
            if (!string.IsNullOrEmpty(reason)) return reason;
        }
        catch { /* JSON이 아닌 응답 */ }
        return transportError;
    }

    class ErrorBody { public string message; public string error; }
}

// 서버의 공통 응답 모양 { success, message, data }
public class ApiResponse<T>
{
    public bool success;
    public string message;
    public T data;
}
