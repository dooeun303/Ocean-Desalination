using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// AR → MR 원격지원 요청 큐 - /api/support-calls, Agora 토큰 - /api/rtc, 작업 매뉴얼 단계 - /api/manuals
public static class SupportCallApi
{
    // MR이 받는 요청 목록(대기 > 진행 > 종료 순, 최신순). status를 주면 그 상태만.
    public static IEnumerator List(string status, Action<List<SupportCallData>> onOk, Action<string> onFail) =>
        ApiClient.Get<ApiResponse<List<SupportCallData>>>(
            "/api/support-calls" + (string.IsNullOrEmpty(status) ? "" : "?status=" + status),
            r => onOk(r.success && r.data != null ? r.data : new List<SupportCallData>()), onFail);

    public static IEnumerator Get(string id, Action<SupportCallData> onOk, Action<string> onFail) =>
        ApiClient.Get<ApiResponse<SupportCallData>>("/api/support-calls/" + id,
            r => { if (r.success && r.data != null) onOk(r.data); else onFail(r.message ?? "조회 실패"); }, onFail);

    // 수락 - 이미 처리된 요청이면 서버가 409로 거절한다.
    public static IEnumerator Accept(string id, string receiverId, Action onOk, Action<string> onFail) =>
        ApiClient.Send("PATCH", $"/api/support-calls/{id}/accept", new { receiver_id = receiverId }, onOk, onFail);

    public static IEnumerator Reject(string id, Action onOk, Action<string> onFail) =>
        ApiClient.Send("PATCH", $"/api/support-calls/{id}/reject", null, onOk, onFail);

    // 통화 종료. by = 끊은 쪽 - 서버가 반대쪽 기기에 call_end를 보낸다.
    public static IEnumerator End(string id, string by, Action onOk, Action<string> onFail) =>
        ApiClient.Send("PATCH", $"/api/support-calls/{id}/end?by={by}", null, onOk, onFail);
}

public static class RtcApi
{
    // Agora 채널 토큰(uid 0)
    public static IEnumerator Token(string channel, Action<string> onOk, Action<string> onFail) =>
        ApiClient.Get<TokenResponse>($"/api/rtc/{channel}/0", r =>
        {
            if (string.IsNullOrEmpty(r.token)) onFail("토큰 응답이 비어있음"); else onOk(r.token);
        }, onFail);

    class TokenResponse { public string token; }
}

public static class ManualApi
{
    public class Step { public int step_no; public string title; public string description; public string model_url; }

    // 매뉴얼 단계를 step_no 순서로(없으면 빈 목록)
    public static IEnumerator Steps(string manualId, Action<List<Step>> onOk, Action<string> onFail) =>
        ApiClient.Get<ApiResponse<List<Step>>>("/api/manuals/" + manualId + "/steps",
            r => onOk(r.success && r.data != null ? r.data.OrderBy(s => s.step_no).ToList() : new List<Step>()), onFail);
}
