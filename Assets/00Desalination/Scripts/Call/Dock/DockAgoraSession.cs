using System;
using UnityEngine;
using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.JoinChannelVideoToken;

// 화상통화 도크 전용 Agora 엔진 - 채널 join/leave와 원격(AR) 영상 뷰 생성·삭제만 맡는다.
// 2026-08-26: 씬의 "화상통화_일반"(JoinChannelVideoToken) 인스턴스에는 의존하지 않고 이 엔진을 직접 만든다
// (그 GameObject가 비활성이라 코루틴을 못 돌렸음). MakeVideoView/DestroyVideoView static 유틸만 재사용.
// 통화 녹화(MrCallRecorder)도 이 엔진을 쓴다 - 녹화 전용 채널({채널}_mrrec) 이벤트는 영상 뷰와 무관하게 거른다.
public class DockAgoraSession : IDisposable
{
    // InmoCallManager.cs(AR)/JoinChannelVideoToken.cs(MR 원본 로직)와 동일한 App ID(토큰은 RtcApi).
    const string AgoraAppID = "5b6baf59793d494f95554d1246847731";

    readonly RectTransform videoContainer;
    readonly MrCallRecorder recorder; // null이면 녹화 안 함
    readonly Action<bool> setWaitingForVideo;

    IRtcEngineEx engine;
    public IRtcEngineEx Engine => engine;
    public bool InChannel { get; private set; }
    public string Channel { get; private set; }

    public DockAgoraSession(RectTransform videoContainer, MrCallRecorder recorder, Action<bool> setWaitingForVideo)
    {
        this.videoContainer = videoContainer;
        this.recorder = recorder;
        this.setWaitingForVideo = setWaitingForVideo;
    }

    // MR은 마이크(오디오)만 보내고 AR 영상·음성을 받는다.
    public void Join(string token, string channel)
    {
        EnsureEngine();
        engine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        engine.EnableAudio();
        engine.EnableVideo(); // 원격(AR) 영상을 렌더링하려면 비디오 모듈 자체는 켜져 있어야 함

        var options = new ChannelMediaOptions();
        options.publishCameraTrack.SetValue(false);       // MR 카메라는 publish 안 함
        options.publishCustomVideoTrack.SetValue(false);
        options.publishMicrophoneTrack.SetValue(true);    // 마이크(오디오)만 publish
        options.autoSubscribeVideo.SetValue(true);        // AR 영상은 구독
        options.autoSubscribeAudio.SetValue(true);

        Channel = channel;
        engine.JoinChannel(token, channel, 0, options);
        InChannel = true;
        Debug.Log("[MrCallDockPanel][Agora] 채널 join: " + channel);
    }

    public void Leave()
    {
        if (!InChannel) return;
        recorder?.End(); // 녹화 마무리 + 전용 채널 나가기 → 업로드
        engine?.LeaveChannel();
        InChannel = false;
    }

    public void Dispose()
    {
        if (engine == null) return;
        recorder?.Abort();
        engine.LeaveChannel();
        engine.Dispose();
        engine = null;
        InChannel = false;
    }

    void EnsureEngine()
    {
        if (engine != null) return;
        engine = RtcEngine.CreateAgoraRtcEngineEx();
        engine.Initialize(new RtcEngineContext
        {
            appId = AgoraAppID,
            channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
            audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
            areaCode = AREA_CODE.AREA_CODE_GLOB
        });
        engine.InitEventHandler(new EventHandler(this));
        if (recorder != null)
            MrCallRecorder.ConfigureEngine(engine); // MR 화면 녹화용 외부 비디오 소스 (join 전에 켜야 함)
    }

    // JoinChannelVideoToken.UserEventHandler와 같은 목적 - 원격(AR) 유저가 들어오면 영상 뷰를 만들고, 나가면 지운다.
    class EventHandler : IRtcEngineEventHandler
    {
        readonly DockAgoraSession s;
        internal EventHandler(DockAgoraSession session) { s = session; }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            s.recorder?.OnJoinChannelSuccess(connection);
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            if (connection.channelId != s.Channel) return; // 녹화 전용 채널({채널}_mrrec) 이벤트는 도크 영상과 무관
            JoinChannelVideoToken.MakeVideoView(uid, connection.channelId, s.videoContainer);
            s.setWaitingForVideo(false);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            if (connection.channelId != s.Channel) return;
            JoinChannelVideoToken.DestroyVideoView(uid);
            s.setWaitingForVideo(true);
        }

        public override void OnError(int err, string msg)
        {
            Debug.LogError($"[MrCallDockPanel][Agora] Error {err}: {msg}");
        }
    }
}
