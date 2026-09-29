using System;
using System.Collections;
using System.IO;
using Agora.Rtc;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

// 유지보수 원격지원 녹화 (MR). 통화 수락~종료 동안 MR 헤드셋 시점 렌더 화면(mr_view)을 녹화하고,
// 끝나면 서버로 올린다. AR 카메라 영상은 AR 기기가 직접 원본을 녹화한다(InmoCallRecorder, ar_local).
//
// 영상과 음성은 따로 녹화해서 서버가 합친다(support_call.js - ffmpeg):
//   - 영상 {stem}.mp4 : MR은 원래 영상을 publish하지 않아서(MrCallDockPanel.FetchAgoraTokenAndJoin)
//     녹화할 로컬 스트림이 없다. 헤드 위치를 따라가는 캡처 카메라 → RenderTexture → 커스텀 비디오
//     트랙으로 밀어넣고, 그 트랙을 "{채널}_mrrec" 전용 채널(JoinChannelEx)에만 publish해서 로컬
//     녹화한다. 원래 통화 채널에 publish하면 AR 쪽에 MR 화면이 새 영상으로 떠버리기 때문.
//     패스스루(실제 카메라 영상)는 OS가 합성하므로 앱에서 찍히지 않는다 - 홀로그램/UI만 어두운 배경 위에 녹화됨.
//   - 음성 {stem}.aac : 통화 대화 전체(MR 마이크 + AR 음성 믹스). Agora 로컬 영상 녹화는 내 마이크만
//     담을 수 있어서, StartAudioRecording(MIXED)으로 따로 녹음한다.
// 업로드는 음성 → 영상 순서(같은 stem). 영상 업로드 때 두 녹화의 시작 시각 차이(offset_ms)를 같이 보내
// 서버가 음성 앞부분을 잘라 싱크를 맞춘다.
//
// MrCallDockPanel이 AddComponent로 붙이고 Agora 이벤트를 넘겨준다 - 씬에 따로 붙일 필요 없음.
public class MrCallRecorder : MonoBehaviour
{
    const string Source = "mr_view";
    const string RecChannelSuffix = "_mrrec";
    const int MaxDurationMs = 3 * 60 * 60 * 1000; // 기본값(2분)이면 긴 작업 중간에 녹화가 끊김
    const int ViewWidth = 1280;
    const int ViewHeight = 720;
    const float ViewFps = 15f;
    // POST /api/support-calls/{id}/recordings?source=&filename=[&part=audio | &offset_ms=]
    static readonly string SupportCallUrl = ServerConfig.BaseUrl + "/api/support-calls/";

    public static string RecordingDir => Path.Combine(Application.persistentDataPath, "SupportRecordings");

    IRtcEngineEx _engine;
    string _channel;
    string _callId;
    string _stem;
    bool _active;

    // 통화 음성
    string _audioPath;
    float _audioStartTime;

    // MR 시점 화면
    RtcConnection _viewConn;
    uint _viewTrackId;
    bool _viewJoined;
    IMediaRecorder _viewRecorder;
    string _viewPath;
    float _viewStartTime;
    Camera _captureCam;
    RenderTexture _rt;
    byte[] _frameBuf;
    byte[] _rowBuf;
    float _nextCaptureTime;
    bool _readbackPending;

    // 엔진 생성 직후(join 전) 한 번 - 외부 비디오 소스는 join 전에 켜야 한다.
    public static void ConfigureEngine(IRtcEngineEx engine)
    {
        engine.SetExternalVideoSource(true, false, EXTERNAL_VIDEO_SOURCE_TYPE.VIDEO_FRAME, new SenderOptions());
    }

    // 통화 채널 JoinChannel 직후. 음성은 통화 채널 join 완료 시, 영상은 전용 채널 토큰을 받은 뒤
    // StartViewCapture → 전용 채널 join 완료 시 시작된다(둘 다 OnJoinChannelSuccess 경유).
    public void Begin(IRtcEngineEx engine, string channel, string callId)
    {
        if (_active) End();
        _engine = engine;
        _channel = channel;
        _callId = callId;
        _stem = $"{DateTime.Now:yyyyMMdd_HHmmss}_{channel}_{Source}";
        _audioPath = _viewPath = null;
        _active = true;
        Directory.CreateDirectory(RecordingDir);
    }

    // DockAgoraEventHandler.OnJoinChannelSuccess에서 넘겨받음.
    public void OnJoinChannelSuccess(RtcConnection connection)
    {
        if (!_active) return;
        if (connection.channelId == _channel) StartAudioRecording();
        else if (_viewConn != null && connection.channelId == _viewConn.channelId) StartViewRecording(connection);
    }

    // ───────────── 통화 음성 ─────────────

    void StartAudioRecording()
    {
        if (_audioPath != null) return; // 재접속(rejoin)으로 다시 불려도 같은 파일 유지
        _audioPath = Path.Combine(RecordingDir, _stem + ".aac");
        _audioStartTime = Time.realtimeSinceStartup;
        int ret = _engine.StartAudioRecording(new AudioRecordingConfiguration
        {
            filePath = _audioPath,
            encode = true, // AAC - 원본 PCM(wav)은 1시간에 수백 MB라
            sampleRate = 32000,
            fileRecordingType = AUDIO_FILE_RECORDING_TYPE.AUDIO_FILE_RECORDING_MIXED, // 내 마이크 + 상대(AR) 음성
            quality = AUDIO_RECORDING_QUALITY_TYPE.AUDIO_RECORDING_QUALITY_MEDIUM,
        });
        Debug.Log($"[MrCallRecorder] 통화 음성 녹음 시작 ret={ret} path={_audioPath}");
    }

    // ───────────── MR 시점 화면 ─────────────

    public void StartViewCapture(string recToken)
    {
        if (!_active || _viewConn != null) return;
        var main = Camera.main;
        if (main == null)
        {
            Debug.LogWarning("[MrCallRecorder] Camera.main 없음 - MR 화면 녹화 건너뜀");
            return;
        }

        _rt = new RenderTexture(ViewWidth, ViewHeight, 24, RenderTextureFormat.ARGB32);
        _frameBuf = new byte[ViewWidth * ViewHeight * 4];
        _rowBuf = new byte[ViewWidth * 4];

        var camGo = new GameObject("MrRecordCaptureCam");
        camGo.transform.SetParent(main.transform, false);
        _captureCam = camGo.AddComponent<Camera>();
        _captureCam.CopyFrom(main);
        _captureCam.stereoTargetEye = StereoTargetEyeMask.None; // XR 눈 렌더가 아니라 RT로 단일 시점 렌더
        _captureCam.targetTexture = _rt;
        _captureCam.fieldOfView = 60f;
        // 메인 카메라는 패스스루용 투명 배경이라 그대로 복사하면 녹화본이 까맣게/깨져 보인다.
        _captureCam.clearFlags = CameraClearFlags.SolidColor;
        _captureCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        _captureCam.enabled = false; // 캡처 주기에만 켠다 (매 프레임 두 번 렌더 방지)
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

        _viewTrackId = _engine.CreateCustomVideoTrack();
        _viewConn = new RtcConnection(_channel + RecChannelSuffix, (uint)UnityEngine.Random.Range(100000, 999999));
        var options = new ChannelMediaOptions();
        options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        options.publishCameraTrack.SetValue(false);
        options.publishMicrophoneTrack.SetValue(false); // 음성은 통화 채널에서 따로 믹스 녹음
        options.publishCustomVideoTrack.SetValue(true);
        options.customVideoTrackId.SetValue(_viewTrackId);
        options.autoSubscribeAudio.SetValue(false);
        options.autoSubscribeVideo.SetValue(false);
        int ret = _engine.JoinChannelEx(recToken, _viewConn, options);
        Debug.Log($"[MrCallRecorder] 녹화 전용 채널 join {_viewConn.channelId} ret={ret}");
    }

    void StartViewRecording(RtcConnection connection)
    {
        if (_viewRecorder != null) return;
        _viewJoined = true;

        _viewPath = Path.Combine(RecordingDir, _stem + ".mp4");
        _viewStartTime = Time.realtimeSinceStartup;
        _viewRecorder = _engine.CreateMediaRecorder(new RecorderStreamInfo(connection.channelId, connection.localUid));
        _viewRecorder.SetMediaRecorderObserver(new LogObserver());
        int ret = _viewRecorder.StartRecording(new MediaRecorderConfiguration
        {
            storagePath = _viewPath,
            containerFormat = MediaRecorderContainerFormat.FORMAT_MP4,
            streamType = MediaRecorderStreamType.STREAM_TYPE_VIDEO,
            maxDurationMs = MaxDurationMs,
            width = ViewWidth,
            height = ViewHeight,
            fps = (int)ViewFps,
            videoSourceType = VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CUSTOM,
        });
        Debug.Log($"[MrCallRecorder] MR 화면 녹화 시작 ret={ret} path={_viewPath}");
    }

    void Update()
    {
        if (!_viewJoined || _captureCam == null || _readbackPending) return;
        if (Time.unscaledTime < _nextCaptureTime) return;
        _nextCaptureTime = Time.unscaledTime + 1f / ViewFps;
        _captureCam.enabled = true; // 이번 프레임에 한 번 렌더 → OnEndCameraRendering에서 다시 끔
    }

    void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != _captureCam) return;
        _captureCam.enabled = false;
        _readbackPending = true;
        AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGBA32, OnReadback);
    }

    void OnReadback(AsyncGPUReadbackRequest req)
    {
        _readbackPending = false;
        if (req.hasError || !_viewJoined || _engine == null) return;

        req.GetData<byte>().CopyTo(_frameBuf);
        FlipRows(_frameBuf, ViewWidth * 4, ViewHeight); // Unity 텍스처는 아래쪽 행이 0번

        _engine.PushVideoFrame(new ExternalVideoFrame
        {
            type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA,
            format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA,
            buffer = _frameBuf,
            stride = ViewWidth,
            height = ViewHeight,
            rotation = 0,
            timestamp = DateTime.Now.Ticks / 10000,
        }, _viewTrackId);
    }

    void FlipRows(byte[] buf, int rowBytes, int rows)
    {
        for (int top = 0, bottom = rows - 1; top < bottom; top++, bottom--)
        {
            Buffer.BlockCopy(buf, top * rowBytes, _rowBuf, 0, rowBytes);
            Buffer.BlockCopy(buf, bottom * rowBytes, buf, top * rowBytes, rowBytes);
            Buffer.BlockCopy(_rowBuf, 0, buf, bottom * rowBytes, rowBytes);
        }
    }

    // ───────────── 종료 · 업로드 ─────────────

    // 통화 채널을 떠나기 "전에" 호출(MrCallDockPanel.LeaveAgoraChannel) - 음성 녹음은 채널 안에서 멈춰야 한다.
    public void End()
    {
        if (!_active) return;
        StopAll();

        if (string.IsNullOrEmpty(_callId))
        {
            Debug.LogWarning("[MrCallRecorder] support call id 없음 - 기기에만 저장: " + RecordingDir);
            return;
        }
        // 음성이 영상보다 먼저 시작됐으면 그만큼 음성 앞부분을 잘라야 싱크가 맞는다.
        int offsetMs = Mathf.Max(0, Mathf.RoundToInt((_viewStartTime - _audioStartTime) * 1000f));
        StartCoroutine(UploadAfterFinalize(_callId, _audioPath, _viewPath, offsetMs));
    }

    // 앱 종료 등으로 통화 중에 파괴될 때 - 파일만 닫아두고 업로드는 하지 않는다(기기에 남음).
    // 엔진 Dispose 전에 불려야 해서 MrCallDockPanel.OnDestroy가 먼저 직접 호출한다.
    public void Abort()
    {
        if (_active) StopAll();
    }

    void OnDestroy() => Abort();

    void StopAll()
    {
        _active = false;
        if (_audioPath != null) _engine.StopAudioRecording();

        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        _viewJoined = false;
        if (_viewRecorder != null)
        {
            _viewRecorder.StopRecording();
            _engine.DestroyMediaRecorder(_viewRecorder);
            _viewRecorder = null;
        }
        if (_viewConn != null)
        {
            _engine.LeaveChannelEx(_viewConn);
            _engine.DestroyCustomVideoTrack(_viewTrackId);
            _viewConn = null;
        }
        if (_captureCam != null) Destroy(_captureCam.gameObject);
        if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
    }

    // 음성 → 영상 순서로 올린다. 서버는 영상이 도착했을 때 같은 stem의 음성이 먼저 와 있으면 합친다.
    IEnumerator UploadAfterFinalize(string callId, string audioPath, string videoPath, int offsetMs)
    {
        yield return new WaitForSecondsRealtime(2f); // Stop 후 파일 마무리(mp4 moov 등) 기록이 끝날 시간

        string baseUrl = $"{SupportCallUrl}{callId}/recordings?source={Source}";
        if (IsNonEmpty(audioPath))
            yield return Upload(baseUrl + "&part=audio", audioPath, "audio/aac");
        if (IsNonEmpty(videoPath))
            yield return Upload(baseUrl + $"&offset_ms={offsetMs}", videoPath, "video/mp4");
    }

    static bool IsNonEmpty(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path) && new FileInfo(path).Length > 0) return true;
        if (!string.IsNullOrEmpty(path)) Debug.LogWarning($"[MrCallRecorder] 녹화 파일 없음/비어있음 - 업로드 건너뜀: {path}");
        return false;
    }

    static IEnumerator Upload(string url, string path, string contentType)
    {
        url += "&filename=" + UnityWebRequest.EscapeURL(Path.GetFileName(path));
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerFile(path); // 수백 MB도 메모리에 안 올리고 스트리밍
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", contentType);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[MrCallRecorder] 업로드 실패({req.responseCode}): {req.error} - 기기에 남아있음: {path}");
        else
            Debug.Log($"[MrCallRecorder] 업로드 완료: {path}");
    }

    class LogObserver : IMediaRecorderObserver
    {
        public override void OnRecorderStateChanged(string channelId, uint uid, RecorderState state, RecorderReasonCode reason)
        {
            if (state == RecorderState.RECORDER_STATE_ERROR)
                Debug.LogError($"[MrCallRecorder][{Source}] 녹화 오류: {reason}");
            else
                Debug.Log($"[MrCallRecorder][{Source}] state={state} reason={reason}");
        }
    }
}
