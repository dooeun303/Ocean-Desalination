/// <summary>
/// 서버 주소 중앙 설정.
/// 로컬 DB 테스트 &lt;-&gt; 회사 서버 전환은 아래 <see cref="Current"/> 한 줄만 바꾸면
/// 프로젝트 전체(REST/웹소켓)에 반영됩니다.
/// </summary>
public static class ServerConfig
{
    public enum Target
    {
        /// <summary>내 PC에서 백엔드 서버 + 로컬 DB 실행 (에디터 Play 테스트용)</summary>
        Local,
        /// <summary>회사 서버 192.168.0.66</summary>
        Company,
    }

    // ▼▼▼ 여기 한 줄만 바꾸면 됩니다 ▼▼▼
    public static readonly Target Current = Target.Local;
    // ▲▲▲ 여기 한 줄만 바꾸면 됩니다 ▲▲▲

    public const int Port = 3000;

    /// <summary>현재 대상의 호스트(IP/도메인)</summary>
    public static string Host
    {
        get
        {
            switch (Current)
            {
                case Target.Company: return "192.168.0.66";
                case Target.Local:
                default:             return "localhost";
            }
        }
    }

    /// <summary>http://{host}:{port} — REST API 베이스 URL (뒤에 슬래시 없음)</summary>
    public static string BaseUrl => $"http://{Host}:{Port}";

    /// <summary>ws://{host}:{port} — 웹소켓 베이스 URL</summary>
    public static string WsBaseUrl => $"ws://{Host}:{Port}";
}
