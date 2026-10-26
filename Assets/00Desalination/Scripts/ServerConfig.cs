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
    public static readonly Target Current = Target.Company;
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

    // UnityWebRequest.timeout(초). 기본값 0은 "무제한 대기"라 서버가 응답이 없으면 코루틴이 영원히 멈춘다
    // (예: 통화 토큰 발급이 멈추면 도크가 "연결 대기 중"으로 남음). 요청을 만들 때 둘 중 하나를 넣는다.
    // 녹화 영상 업로드(MrCallRecorder)처럼 수백 MB를 올리는 요청엔 넣지 않는다 - timeout은 전체 소요 시간 제한이라 정상 업로드도 끊긴다.
    /// <summary>일반 REST 요청(조회/상태 변경/토큰 발급) 제한 시간(초)</summary>
    public const int RequestTimeout = 10;
    /// <summary>음성 인식(STT)·이미지 업로드처럼 오래 걸리는 요청 제한 시간(초)</summary>
    public const int LongRequestTimeout = 60;
}
