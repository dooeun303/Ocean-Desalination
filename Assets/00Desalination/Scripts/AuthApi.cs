using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// QR 로그인 서버(auth.js) 통신 담당.
///   POST /api/auth/qr-login  { qr }              → { success, data:{ token, user } }
///   GET  /api/auth/me        Authorization Bearer → { success, data:{ user } }
///   POST /api/auth/logout    Authorization Bearer → { success }
///
/// [사용]
/// AuthApi.Instance.QrLogin(qrText,
///     onSuccess: user => { ... },
///     onError:   msg  => { ... });
/// </summary>
public class AuthApi : MonoBehaviour
{
    public static AuthApi Instance { get; private set; }

    private string baseUrl => ServerConfig.BaseUrl;
    [SerializeField] private float timeoutSeconds = 10f;

    // ---- 서버 응답/요청 DTO (JsonUtility 용) ----
    [Serializable] public class UserDto { public string user_id; public string user_nm; public string company; }
    [Serializable] private class QrLoginBody { public string qr; }
    [Serializable] private class QrLoginData { public string token; public UserDto user; }
    [Serializable] private class QrLoginResponse { public bool success; public QrLoginData data; public string message; }
    [Serializable] private class MeData { public UserDto user; }
    [Serializable] private class MeResponse { public bool success; public MeData data; public string message; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>QR 문자열(작업자 user_id)로 로그인. 성공 시 AuthSession 에 토큰 저장.</summary>
    public void QrLogin(string qr, Action<UserDto> onSuccess, Action<string> onError)
    {
        StartCoroutine(CoQrLogin(qr, onSuccess, onError));
    }

    private IEnumerator CoQrLogin(string qr, Action<UserDto> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/api/auth/qr-login";
        string json = JsonUtility.ToJson(new QrLoginBody { qr = qr });

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = (int)timeoutSeconds;
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest(); // 요청 대기

        // 네트워크 자체 실패 (401 등 서버가 응답한 실패는 아래에서 메시지로 처리)
        if (request.result != UnityWebRequest.Result.Success && request.downloadHandler.text.Length == 0)
        {
            onError?.Invoke($"요청 실패 ({request.responseCode}): {request.error}");
            yield break;
        }

        QrLoginResponse response;
        try
        {
            response = JsonUtility.FromJson<QrLoginResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke($"응답 파싱 실패: {e.Message}");
            yield break;
        }

        if (response != null && response.success && response.data != null)
        {
            var user = response.data.user;
            AuthSession.Set(response.data.token, user?.user_id, user?.user_nm, user?.company);
            onSuccess?.Invoke(user);
        }
        else
        {
            onError?.Invoke(response?.message ?? "로그인에 실패했습니다.");
        }
    }

    /// <summary>저장된 토큰이 아직 유효한지 서버에 확인 (자동 로그인 판단용).</summary>
    public void VerifyToken(Action<bool> done)
    {
        if (!AuthSession.IsLoggedIn)
        {
            done?.Invoke(false);
            return;
        }

        StartCoroutine(CoVerify(done));
    }

    private IEnumerator CoVerify(Action<bool> done)
    {
        string url = $"{baseUrl}/api/auth/me";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = (int)timeoutSeconds;
        request.SetRequestHeader("Authorization", AuthSession.AuthHeader);

        yield return request.SendWebRequest();

        bool valid = request.result == UnityWebRequest.Result.Success;
        if (valid)
        {
            try
            {
                var res = JsonUtility.FromJson<MeResponse>(request.downloadHandler.text);
                valid = res != null && res.success;
            }
            catch
            {
                valid = false;
            }
        }

        if (!valid)
        {
            AuthSession.Clear();
        }

        done?.Invoke(valid);
    }

    /// <summary>로그아웃 — 서버에 알리고 로컬 세션 삭제.</summary>
    public void Logout(Action done = null)
    {
        StartCoroutine(CoLogout(done));
    }

    private IEnumerator CoLogout(Action done)
    {
        if (AuthSession.IsLoggedIn)
        {
            string url = $"{baseUrl}/api/auth/logout";

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = (int)timeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", AuthSession.AuthHeader);

            yield return request.SendWebRequest();
        }

        AuthSession.Clear();
        done?.Invoke();
    }
}
