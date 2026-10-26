using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 알람 - /api/alarms
public static class AlarmApi
{
    // 알람 목록(activeOnly면 활성 알람만)
    public static IEnumerator List(bool activeOnly, Action<List<AlarmData>> onOk, Action<string> onFail) =>
        ApiClient.Get<ApiResponse<List<AlarmData>>>("/api/alarms" + (activeOnly ? "?active=true" : ""),
            r => { if (r.success && r.data != null) onOk(r.data); else onFail(r.message ?? "알람 목록 없음"); }, onFail);

    // 시연용 알람 주기 발생 시작/정지. 성공 시 서버 메시지를 돌려준다.
    public static IEnumerator StartTrigger(Action<string> onOk, Action<string> onFail) =>
        ApiClient.Send<ApiResponse<object>>("POST", "/api/alarms", null, r => onOk(r.message), onFail);

    public static IEnumerator StopTrigger(Action<string> onOk, Action<string> onFail) =>
        ApiClient.Send<ApiResponse<object>>("DELETE", "/api/alarms/active", null, r => onOk(r.message), onFail);
}

// 설비 정보·센서 - /api/equipment
public static class EquipmentApi
{
    public static IEnumerator Get(string equipmentId, Action<EquipmentData> onOk, Action<string> onFail) =>
        ApiClient.Get<EquipmentResponse>("/api/equipment/" + equipmentId,
            r => { if (r.success && r.data != null) onOk(r.data); else onFail(r.message ?? "설비 없음"); }, onFail);

    public static IEnumerator Sensors(string equipmentId, Action<SensorItem[]> onOk, Action<string> onFail) =>
        ApiClient.Get<SensorListResponse>("/api/equipment/" + equipmentId + "/sensors",
            r => { if (r.success && r.data != null) onOk(r.data); else onFail("센서 데이터 없음"); }, onFail);
}

// 정보공유 파일 - /api/files
public static class FileApi
{
    // 파일 목록(type이 비어 있으면 전체)
    public static IEnumerator List(string type, Action<FileData[]> onOk, Action<string> onFail) =>
        ApiClient.Get<FileListResponse>("/api/files" + (string.IsNullOrEmpty(type) ? "" : "?type=" + type),
            r => { if (r.success && r.data != null) onOk(r.data); else onFail("파일 데이터 없음"); }, onFail,
            ServerConfig.LongRequestTimeout);

    // MR 지시하기 그림(PNG) 업로드 - 서버가 저장 후 AR에 file_share로 알린다.
    public static IEnumerator UploadInstruction(byte[] png, string fileName, Action onOk, Action<string> onFail)
    {
        var form = new WWWForm();
        form.AddBinaryData("file", png, fileName, "image/png");
        yield return ApiClient.PostForm<object>("/api/files/instruction", form, _ => onOk?.Invoke(), onFail);
    }
}

// 화상통화 음성 → 전사+요약 - /api/ai/voice-summary
public static class AiApi
{
    // 서버가 응답하면 onResponse(성공 여부는 r.success로 판단), 통신 자체가 실패하면 onFail.
    public static IEnumerator VoiceSummary(byte[] wav, string maintenanceId, Action<SummaryResponse> onResponse, Action<string> onFail)
    {
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "audio.wav", "audio/wav");
        if (!string.IsNullOrEmpty(maintenanceId)) form.AddField("maintenance_id", maintenanceId);
        yield return ApiClient.PostForm("/api/ai/voice-summary", form, onResponse, onFail);
    }
}
