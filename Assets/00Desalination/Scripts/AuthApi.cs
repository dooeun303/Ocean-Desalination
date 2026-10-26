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

    // 401 등 서버가 이유를 준 실패는 그 메시지(예: "등록되지 않은 사용자입니다.")가 onError로 간다(ApiClient).
    private IEnumerator CoQrLogin(string qr, Action<UserDto> onSuccess, Action<string> onError)
    {
        yield return ApiClient.Send<QrLoginResponse>("POST", "/api/auth/qr-login", new QrLoginBody { qr = qr }, response =>
        {
            if (response.success && response.data != null)
            {
                var user = response.data.user;
                AuthSession.Set(response.data.token, user?.user_id, user?.user_nm, user?.company);
                onSuccess?.Invoke(user);
            }
            else
            {
                onError?.Invoke(response.message ?? "로그인에 실패했습니다.");
            }
        }, onError, (int)timeoutSeconds);
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

    // 토큰은 ApiClient가 Authorization 헤더로 붙인다. 통신 실패도 "유효하지 않음"으로 보고 세션을 지운다(기존 동작).
    private IEnumerator CoVerify(Action<bool> done)
    {
        bool valid = false;
        yield return ApiClient.Get<MeResponse>("/api/auth/me", res => valid = res.success, _ => valid = false,
            (int)timeoutSeconds);

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
        // 서버 기록(logout_dt)만 남기는 요청이라 실패해도 로컬 세션은 지운다.
        if (AuthSession.IsLoggedIn)
            yield return ApiClient.Send("POST", "/api/auth/logout", null, null, null, (int)timeoutSeconds);

        AuthSession.Clear();
        done?.Invoke();
    }
}
