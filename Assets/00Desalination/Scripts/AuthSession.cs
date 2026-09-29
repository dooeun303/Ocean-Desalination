using UnityEngine;

/// <summary>
/// 로그인 세션 보관소. JWT 토큰 + 로그인한 사용자 정보를 메모리와 PlayerPrefs 에 저장한다.
/// MonoBehaviour 아님 — 어디서든 <c>AuthSession.Token</c> / <c>AuthSession.IsLoggedIn</c> 으로 접근.
///
/// 서버(auth.js) 응답의 data.token 을 <see cref="Set"/> 로 넘기면 되고,
/// 이후 API 호출 시 헤더에 <see cref="AuthHeader"/> ("Bearer ...") 를 넣는다.
/// </summary>
public static class AuthSession
{
    private const string KEY_TOKEN   = "auth.token";
    private const string KEY_USER_ID = "auth.user_id";
    private const string KEY_USER_NM = "auth.user_nm";
    private const string KEY_COMPANY = "auth.company";

    public static string Token    { get; private set; }
    public static string UserId   { get; private set; }
    public static string UserName { get; private set; }
    public static string Company  { get; private set; }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    /// <summary>HTTP Authorization 헤더 값. "Bearer eyJ..." — 토큰 없으면 null.</summary>
    public static string AuthHeader => IsLoggedIn ? "Bearer " + Token : null;

    // 앱 시작(첫 접근) 시 이전 세션 복원
    static AuthSession()
    {
        Token    = NullIfEmpty(PlayerPrefs.GetString(KEY_TOKEN,   ""));
        UserId   = NullIfEmpty(PlayerPrefs.GetString(KEY_USER_ID, ""));
        UserName = NullIfEmpty(PlayerPrefs.GetString(KEY_USER_NM, ""));
        Company  = NullIfEmpty(PlayerPrefs.GetString(KEY_COMPANY, ""));
    }

    /// <summary>로그인 성공 시 호출 — 토큰/사용자 저장 (PlayerPrefs 영속).</summary>
    public static void Set(string token, string userId, string userName, string company)
    {
        Token    = token;
        UserId   = userId;
        UserName = userName;
        Company  = company;

        PlayerPrefs.SetString(KEY_TOKEN,   token    ?? "");
        PlayerPrefs.SetString(KEY_USER_ID, userId   ?? "");
        PlayerPrefs.SetString(KEY_USER_NM, userName ?? "");
        PlayerPrefs.SetString(KEY_COMPANY, company  ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>로그아웃 — 로컬 세션 전부 삭제.</summary>
    public static void Clear()
    {
        Token = UserId = UserName = Company = null;
        PlayerPrefs.DeleteKey(KEY_TOKEN);
        PlayerPrefs.DeleteKey(KEY_USER_ID);
        PlayerPrefs.DeleteKey(KEY_USER_NM);
        PlayerPrefs.DeleteKey(KEY_COMPANY);
        PlayerPrefs.Save();
    }

    private static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
}
