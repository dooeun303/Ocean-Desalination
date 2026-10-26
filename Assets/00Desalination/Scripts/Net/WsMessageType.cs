// WebSocket 메시지 타입 목록 - 서버·AR·MR이 주고받는 { type: ... } 값은 전부 여기서 고른다.
// 같은 목록이 서버 TempServer/ws_messages.js(어디로 중계하는지 포함)와
// AR Assets/Scripts/Net/WsMessageType.cs에 있다. 추가·변경할 때는 세 곳을 같이 고친다.
public static class WsMessageType
{
    // 화상통화
    public const string CallRequest = "call_request";               // MR→AR 통화 요청 { channelName }
    public const string CallResponse = "call_response";             // AR→MR 수락/거절 { channelName, accepted }
    public const string CallEnd = "call_end";                       // 양방향(+서버) 통화 종료 { channelName, id? }

    // 원격지원 요청 큐(서버가 보냄)
    public const string SupportCallRequest = "support_call_request";   // 서버→MR 새 요청 { id }
    public const string SupportCallAccepted = "support_call_accepted"; // 서버→AR MR이 수락 { id, channel_name }
    public const string SupportCallRejected = "support_call_rejected"; // 서버→AR MR이 거절 { id }

    // 작업 진행
    public const string GuideStepControl = "guide_step_control";       // AR→MR 작업가이드 단계 { stepIndex(1부터), stepCount }
    public const string MaintenanceComplete = "maintenance_complete";  // AR→MR 작업 결과 저장 완료
    public const string FileShare = "file_share";                      // MR(또는 서버)→AR 파일 공유 { file_id, file_url }

    // 통화 중 실시간 지시(MR→AR)
    public const string DrawStart = "draw_start";       // { strokeId, drawColor, drawWidth }
    public const string DrawPoint = "draw_point";       // { strokeId, drawX, drawY } 0..1 정규화
    public const string DrawEnd = "draw_end";           // { strokeId }
    public const string ModelView = "model_view";       // 3D 모델 창 열림/닫힘 { modelViewOpen, modelUrl }
    public const string ModelRotate = "model_rotate";   // { rotX, rotY, rotZ, rotW }
    public const string ModelPoint = "model_point";     // { pointing, partName, pointX..Z, normX..Z }
    public const string ModelExplode = "model_explode"; // { exploded }

    // AR 작업자 위치(AR→MR) - MR 쪽 표시(ArLocationSync)는 현재 씬에 연결돼 있지 않음
    public const string ArLocation = "ar_location";     // { equipmentId }
    public const string ArPose = "ar_pose";             // { poseX, poseY, poseZ } AR 로컬 좌표

    // 고장예지 알람(서버→MR/AR)
    public const string Alarm = "ALARM";
}
