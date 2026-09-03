// original //
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Serialization;
//using Agora.Rtc;
//using io.agora.rtc.demo;

//namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.JoinChannelVideoToken
//{
//    public class JoinChannelVideoToken : MonoBehaviour
//    {
//        [FormerlySerializedAs("appIdInput")]
//        [SerializeField]
//        private AppIdInput _appIdInput;

//        [Header("_____________Basic Configuration_____________")]
//        [FormerlySerializedAs("APP_ID")]
//        [SerializeField]
//        private string _appID = "";

//        [FormerlySerializedAs("TOKEN")]
//        [SerializeField]
//        private string _token = "";

//        [FormerlySerializedAs("CHANNEL_NAME")]
//        [SerializeField]
//        private string _channelName = "";

//        public Text LogText;
//        internal Logger Log;
//        internal IRtcEngine RtcEngine = null;

//        internal static string _channelToken = "";
//        internal static string _tokenBase = "http://localhost:8080";
//        internal CONNECTION_STATE_TYPE _state = CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED;

//        // Use this for initialization
//        private void Start()
//        {
//            LoadAssetData();
//            if (CheckAppId())
//            {
//                InitEngine();
//                JoinChannel();
//            }
//        }

//        internal void RenewOrJoinToken(string newToken)
//        {
//            JoinChannelVideoToken._channelToken = newToken;
//            if (_state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED
//                || _state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED
//                || _state == CONNECTION_STATE_TYPE.CONNECTION_STATE_FAILED
//            )
//            {
//                // If we are not connected yet, connect to the channel as normal
//                JoinChannel();
//            }
//            else
//            {
//                // If we are already connected, we should just update the token
//                UpdateToken();
//            }
//        }

//        // Update is called once per frame
//        private void Update()
//        {
//            PermissionHelper.RequestMicrophontPermission();
//            PermissionHelper.RequestCameraPermission();
//        }

//        private void UpdateToken()
//        {
//            RtcEngine.RenewToken(JoinChannelVideoToken._channelToken);
//        }

//        private bool CheckAppId()
//        {
//            Log = new Logger(LogText);
//            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
//        }

//        //Show data in AgoraBasicProfile
//        [ContextMenu("ShowAgoraBasicProfileData")]
//        private void LoadAssetData()
//        {
//            if (_appIdInput == null) return;
//            _appID = _appIdInput.appID;
//            _token = _appIdInput.token;
//            _channelToken = _appIdInput.token;
//            _channelName = _appIdInput.channelName;
//        }

//        private void InitEngine()
//        {
//            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
//            UserEventHandler handler = new UserEventHandler(this);
//            RtcEngineContext context = new RtcEngineContext();
//            context.appId = _appID;
//            context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
//            context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT;
//            context.areaCode = AREA_CODE.AREA_CODE_GLOB;
//            RtcEngine.Initialize(context);
//            RtcEngine.InitEventHandler(handler);
//        }

//        private void JoinChannel()
//        {
//            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
//            RtcEngine.EnableAudio();
//            RtcEngine.EnableVideo();

//            if (_channelToken.Length == 0)
//            {
//                StartCoroutine(HelperClass.FetchToken(_tokenBase, _channelName, 0, this.RenewOrJoinToken));
//                return;
//            }

//            RtcEngine.JoinChannel(_channelToken, _channelName, "",0);
//        }

//        private void OnDestroy()
//        {
//            Debug.Log("OnDestroy");
//            if (RtcEngine == null) return;
//            RtcEngine.InitEventHandler(null);
//            RtcEngine.LeaveChannel();
//            RtcEngine.Dispose();
//        }

//        internal string GetChannelName()
//        {
//            return _channelName;
//        }

//        #region -- Video Render UI Logic ---

//        internal static void MakeVideoView(uint uid, string channelId = "")
//        {
//            GameObject go = GameObject.Find(uid.ToString());
//            if (!ReferenceEquals(go, null))
//            {
//                return; // reuse
//            }

//            // create a GameObject and assign to this new user
//            VideoSurface videoSurface = MakeImageSurface(uid.ToString());
//            if (!ReferenceEquals(videoSurface, null))
//            {
//                // configure videoSurface
//                if (uid == 0)
//                {
//                    videoSurface.SetForUser(uid, channelId);
//                }
//                else
//                {
//                    videoSurface.SetForUser(uid, channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
//                }

//                videoSurface.OnTextureSizeModify += (int width, int height) =>
//                {
//                    var transform = videoSurface.GetComponent<RectTransform>();
//                    if (transform)
//                    {
//                        //If render in RawImage. just set rawImage size.
//                        transform.sizeDelta = new Vector2(width / 2, height / 2);
//                        transform.localScale = Vector3.one;
//                    }
//                    else
//                    {
//                        //If render in MeshRenderer, just set localSize with MeshRenderer
//                        float scale = (float)height / (float)width;
//                        videoSurface.transform.localScale = new Vector3(-1, 1, scale);
//                    }
//                    Debug.Log("OnTextureSizeModify: " + width + "  " + height);
//                };

//                videoSurface.SetEnable(true);
//            }
//        }

//        // VIDEO TYPE 1: 3D Object
//        private static VideoSurface MakePlaneSurface(string goName)
//        {
//            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

//            if (go == null)
//            {
//                return null;
//            }

//            go.name = goName;
//            var mesh = go.GetComponent<MeshRenderer>();
//            if (mesh != null)
//            {
//                Debug.LogWarning("VideoSureface update shader");
//                mesh.material = new Material(Shader.Find("Unlit/Texture"));
//            }
//            // set up transform
//            go.transform.Rotate(-90.0f, 0.0f, 0.0f);
//            go.transform.position = Vector3.zero;
//            go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

//            // configure videoSurface
//            var videoSurface = go.AddComponent<VideoSurfaceYUV>();
//            return videoSurface;
//        }

//        // Video TYPE 2: RawImage
//        private static VideoSurface MakeImageSurface(string goName)
//        {
//            GameObject go = new GameObject();

//            if (go == null)
//            {
//                return null;
//            }

//            go.name = goName;
//            // to be renderered onto
//            go.AddComponent<RawImage>();
//            // make the object draggable
//            go.AddComponent<UIElementDrag>();
//            GameObject canvas = GameObject.Find("VideoCanvas");
//            if (canvas != null)
//            {
//                go.transform.parent = canvas.transform;
//                Debug.Log("add video view");
//            }
//            else
//            {
//                Debug.Log("Canvas is null video view");
//            }

//            // set up transform
//            go.transform.Rotate(0f, 0.0f, 180.0f);
//            go.transform.localPosition = Vector3.zero;
//            go.transform.localScale = new Vector3(3f, 4f, 1f);

//            // configure videoSurface
//            var videoSurface = go.AddComponent<VideoSurfaceYUV>();
//            return videoSurface;
//        }

//        internal static void DestroyVideoView(uint uid)
//        {
//            GameObject go = GameObject.Find(uid.ToString());
//            if (!ReferenceEquals(go, null))
//            {
//                Object.Destroy(go);
//            }
//        }

//        #endregion
//    }

//    #region -- Agora Event ---

//    internal class UserEventHandler : IRtcEngineEventHandler
//    {
//        private readonly JoinChannelVideoToken _helloVideoTokenAgora;

//        internal UserEventHandler(JoinChannelVideoToken helloVideoTokenAgora)
//        {
//            _helloVideoTokenAgora = helloVideoTokenAgora;
//        }

//        public override void OnError(int err, string msg)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
//        }

//        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
//        {
//            int build = 0;
//            _helloVideoTokenAgora.Log.UpdateLog(string.Format("sdk version: ${0}",
//                _helloVideoTokenAgora.RtcEngine.GetVersion(ref build)));
//            _helloVideoTokenAgora.Log.UpdateLog(
//                string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
//                    connection.channelId, connection.localUid, elapsed));
//            _helloVideoTokenAgora.Log.UpdateLog(string.Format("New Token: {0}",
//                JoinChannelVideoToken._channelToken));
//            // HelperClass.FetchToken(tokenBase, channelName, 0, this.RenewOrJoinToken);
//            JoinChannelVideoToken.MakeVideoView(0);
//        }

//        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog("OnRejoinChannelSuccess");
//        }

//        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog("OnLeaveChannel");
//            JoinChannelVideoToken.DestroyVideoView(0);
//        }

//        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole,
//            CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog("OnClientRoleChanged");
//        }

//        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid,
//                elapsed));
//            JoinChannelVideoToken.MakeVideoView(uid, _helloVideoTokenAgora.GetChannelName());
//        }

//        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
//                (int)reason));
//            JoinChannelVideoToken.DestroyVideoView(uid);
//        }

//        public override void OnTokenPrivilegeWillExpire(RtcConnection connection, string token)
//        {
//            _helloVideoTokenAgora.StartCoroutine(HelperClass.FetchToken(JoinChannelVideoToken._tokenBase,
//                _helloVideoTokenAgora.GetChannelName(), 0, _helloVideoTokenAgora.RenewOrJoinToken));
//        }

//        public override void OnConnectionStateChanged(RtcConnection connection, CONNECTION_STATE_TYPE state,
//            CONNECTION_CHANGED_REASON_TYPE reason)
//        {
//            _helloVideoTokenAgora._state = state;
//        }

//        public override void OnConnectionLost(RtcConnection connection)
//        {
//            _helloVideoTokenAgora.Log.UpdateLog(string.Format("OnConnectionLost "));
//        }
//    }

//    #endregion
//}




//ver 2 화상연결 //
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using TMPro;
using Agora.Rtc;
using io.agora.rtc.demo;

namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.JoinChannelVideoToken
{
    public class JoinChannelVideoToken : MonoBehaviour
    {
        // 아고라 채널 정보
        [FormerlySerializedAs("appIdInput")]
        [SerializeField] private AppIdInput _appIdInput; // 앱 아이디 이나풋

        [Header("Basic Configuration")]
        [SerializeField] private string _appID = ""; // 앱 아이디
        [SerializeField] private string _token = ""; // 토큰 (더 이상 수동 입력 안 해도 됨 — 자동발급으로 대체)
        [SerializeField] private string _channelName = ""; // 채널명

        [Header("토큰 자동발급 서버")]
        [SerializeField] private string _tokenServerUrl = "http://192.168.0.66:3000"; // TempServer 주소

        [Header("DEBUG UI")]
        [SerializeField] private RawImage DebugWebCamPreview; // WebCamTexture 직접 확인
        [SerializeField] private TMP_Text DebugStateText; // 화면에 보이는 상태 텍스트 (TextMeshPro)

        [Header("AR 영상 표시 위치")]
        // MR-AR는 항상 1:1이라서 VideoSlotManager(여러 명 중 빈 슬롯 찾기)를 거치지 않고
        // 이 자리에 바로 AR 영상을 띄운다. 여기에 영상이 들어갈 빈 오브젝트(RectTransform)를 연결할 것.
        [SerializeField] internal Transform remoteVideoContainer;

        // log text
        public Text LogText; // 로그 텍스트
        internal Logger Log;

        // rtc engine
        internal IRtcEngineEx RtcEngine; // rtc engine

        // 채널토큰
        // 연결상태 타입
        internal static string _channelToken = "";
        internal CONNECTION_STATE_TYPE _state = CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED;

        /* ---------- External Camera ---------- */
        /// <summary>
        /// 외부 카메라
        /// </summary> 
        private WebCamTexture _webCam; // 웹캠텍스쳐
        private byte[] _videoBuffer; // 비디오 버퍼
        private uint _customVideoTrackId; // 커스텀비디오트랙id
        private int _pushFrameCounter = 0; // 푸시 프레임 카운터

        /* ================= Unity Lifecycle ================= */

        // 통화 요청/수신 연동:
        // MR은 videoCallPanel이 켜지는 즉시 채널에 join하지 않고,
        // "AR이 수락했다"는 신호(VideoCallSignalingMR.OnCallAccepted)를 받은 뒤에만 join한다.
        // MR은 화면(영상)을 보낼 필요가 없고 AR 화면만 받아서 보면 되므로,
        // 카메라/커스텀 비디오트랙 관련 코드는 당분간 사용하지 않는다 (아래 주석 처리).
        private bool _isEngineReady = false;

        // 통화 종료 처리용 상태
        // _hasJoined: 실제로 채널에 join한 적이 있는지 (join 전에 패널이 닫히면 상대에게 종료 신호를 보낼 필요 없음)
        // _endingFromRemote: 상대(AR)가 먼저 끊어서 닫히는 중인지 (이 경우엔 다시 종료 신호를 보내지 않음 — 핑퐁 방지)
        internal bool _hasJoined = false;
        private bool _endingFromRemote = false;

        private void Start()
        {
            LoadAssetData();
            if (!CheckAppId()) return; // AppID가 유효한지 확인 (유효하지 않으면 초기화 할 수 없음)

            InitEngine(); // 엔진만 미리 초기화 (join은 통화 수락 후)
            _isEngineReady = true;

            VideoCallSignalingMR.OnCallAccepted += HandleCallAccepted;
            VideoCallSignalingMR.OnCallRejected += HandleCallRejected;
            VideoCallSignalingMR.OnCallEnded += HandleCallEnded;

            DebugState("[WAIT] 통화 수락 대기 중...");
        }

        private void Update()
        {
            PermissionHelper.RequestCameraPermission(); // 카메라, 마이크 허가
            PermissionHelper.RequestMicrophontPermission();

            // MR은 영상을 보내지 않으므로 외부 카메라 프레임 push는 하지 않음
            //if (_webCam != null && _webCam.isPlaying)
            //{
            //    PushExternalFrame();
            //}
        }

        // 통화 수락됨 → 토큰 자동발급 받은 뒤 해당 채널로 join
        private void HandleCallAccepted(string channelName)
        {
            if (!_isEngineReady) return;
            _channelName = channelName;
            DebugState($"[CALL] 수락됨 → 토큰 발급 중: {channelName}");
            StartCoroutine(FetchTokenAndJoin(channelName));
        }

        // 토큰 서버(TempServer/agora.js, /rtc/:channelName/:uid)에서 토큰을 받아온 뒤 join
        private IEnumerator FetchTokenAndJoin(string channelName)
        {
            string url = $"{_tokenServerUrl}/rtc/{channelName}/0";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                DebugState($"[TOKEN] 발급 실패: {request.error}");
                yield break;
            }

            var tokenResponse = JsonUtility.FromJson<TokenServerResponse>(request.downloadHandler.text);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.token))
            {
                DebugState("[TOKEN] 응답에 토큰 없음");
                yield break;
            }

            _channelToken = tokenResponse.token;
            DebugState("[TOKEN] 발급 완료 → join 시도");
            JoinChannel();
        }

        // 통화 거절됨 → 로그만 남기고 패널은 호출한 쪽(팝업)에서 닫음
        private void HandleCallRejected(string channelName)
        {
            DebugState($"[CALL] 거절됨: {channelName}");
        }

        // AR이 통화를 끊음 → 나도 같이 나가야 함 (패널을 꺼서 OnDisable이 처리하게 함)
        private void HandleCallEnded(string channelName)
        {
            if (channelName != _channelName) return; // 다른 통화면 무시
            DebugState("상대방이 통화를 종료했습니다");
            _endingFromRemote = true;
            gameObject.SetActive(false);
        }

        // 패널이 꺼지면(통화 종료) 채널에서 나간다.
        // 내가 먼저 끊는 경우(패널을 직접 닫는 경우)엔 AR에게도 종료 신호를 보낸다.
        private void OnDisable()
        {
            if (RtcEngine != null)
            {
                if (_hasJoined && !_endingFromRemote)
                {
                    VideoCallSignalingMR.Instance?.EndCall(_channelName);
                }
                RtcEngine.LeaveChannel();
            }

            // 내가 먼저 나갈 땐 OnUserOffline이 오지 않아 예전 AR 영상 뷰가 그대로 남는다.
            // 남겨두면 재연결 시 MakeVideoView가 "이미 있다"고 착각해 재사용해버리고,
            // 그 VideoSurface는 이전 세션에 바인딩된 상태라 새 영상을 못 받는다 (화면 안 뜸).
            // 재연결 때 항상 새로 만들어지도록 여기서 확실히 지운다.
            ClearRemoteVideoViews();

            _hasJoined = false;
            _endingFromRemote = false;
        }

        private void ClearRemoteVideoViews()
        {
            if (remoteVideoContainer == null) return;

            for (int i = remoteVideoContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(remoteVideoContainer.GetChild(i).gameObject);
            }
        }

        // 종료
        private void OnDestroy()
        {
            VideoCallSignalingMR.OnCallAccepted -= HandleCallAccepted;
            VideoCallSignalingMR.OnCallRejected -= HandleCallRejected;
            VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;

            if (_webCam != null && _webCam.isPlaying)
                _webCam.Stop();

            if (RtcEngine != null)
            {
                //RtcEngine.DestroyCustomVideoTrack(_customVideoTrackId); // MR은 커스텀 비디오트랙을 만들지 않으므로 파괴할 것도 없음
                RtcEngine.LeaveChannel(); // 채널 떠나기
                RtcEngine.Dispose();
            }
        }

        /* ================= Init ================= */

        // 채널 정보 초기화
        private void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID; // appID
            _token = _appIdInput.token; // token
            _channelToken = _appIdInput.token; // 채널토큰
            _channelName = _appIdInput.channelName; // 채널이름 
        }

        private bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Invalid AppID");
        }

        private void InitEngine()
        {
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngineEx();

            var context = new RtcEngineContext
            {
                appId = _appID,
                channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
                areaCode = AREA_CODE.AREA_CODE_GLOB
            };

            // RTC Engine에 appID, 
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(new UserEventHandler(this));

            DebugState("[ENGINE] Initialized");
        }

        /* ================= External Camera ================= */

        private void InitExternalCamera() // 외부카메라초기화
        {
            // 디바이스 가져오기
            WebCamDevice[] devices = WebCamTexture.devices;

            DebugState($"[CAM] Device Count = {devices.Length}");

            for (int i = 0; i < devices.Length; i++)
            {
                DebugState($"[CAM] {i}: {devices[i].name}, front={devices[i].isFrontFacing}");
            }

            if (devices.Length == 0)
            {
                DebugState("[ERROR] No WebCam Detected");
                return;
            }


            _webCam = new WebCamTexture(devices[0].name, 640, 480, 15);
            _webCam.Play();

            if (DebugWebCamPreview != null)
            {
                DebugWebCamPreview.texture = _webCam;
                DebugState("[CAM] WebCamTexture bound to Debug Preview");
            }
        }

        /* ================= Custom Track ================= */
        // 비디오 트랙 만들기
        private void CreateCustomVideoTrack()
        {
            _customVideoTrackId = RtcEngine.CreateCustomVideoTrack();
            DebugState($"[TRACK] Custom Track Created: {_customVideoTrackId}");
        }

        // 채널 참여
        // MR: 오디오만 publish (마이크), 비디오는 publish 안 하고 AR쪽 영상만 구독(subscribe)
        private void JoinChannel()
        {
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo(); // 원격(AR) 영상을 렌더링하려면 비디오 모듈 자체는 켜져 있어야 함
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

            ChannelMediaOptions options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(false);       // MR 카메라 publish 안 함
            options.publishCustomVideoTrack.SetValue(false);  // 커스텀 비디오트랙도 publish 안 함
            options.publishMicrophoneTrack.SetValue(true);    // 마이크(오디오)는 publish
            options.autoSubscribeVideo.SetValue(true);        // AR 영상은 구독
            options.autoSubscribeAudio.SetValue(true);        // AR 오디오도 구독

            DebugState("연결 시도 중...");

            RtcEngine.JoinChannel(_channelToken, _channelName, 0, options);
        }

        /* ================= Push Frame ================= */
        /// <summary>
        /// 외부 프레임 푸시
        /// </summary>
        private void PushExternalFrame()
        {
            if (_webCam.width < 16 || _webCam.height < 16)
            {
                DebugState("[WAIT] WebCam not ready");
                return;
            }

            Color32[] pixels = _webCam.GetPixels32();
            int width = _webCam.width; // 가로
            int height = _webCam.height; // 세로

            // 00
            if (_videoBuffer == null || _videoBuffer.Length != pixels.Length * 4)
                _videoBuffer = new byte[pixels.Length * 4];


            for (int i = 0; i < pixels.Length; i++)
            {
                int idx = i * 4;
                _videoBuffer[idx] = pixels[i].r;
                _videoBuffer[idx + 1] = pixels[i].g;
                _videoBuffer[idx + 2] = pixels[i].b;
                _videoBuffer[idx + 3] = pixels[i].a;
            }

            // 외부 비디오 프레임
            ExternalVideoFrame frame = new ExternalVideoFrame
            {
                type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA,
                format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA,
                buffer = _videoBuffer,
                stride = width,
                height = height,
                rotation = 0,
                timestamp = System.DateTime.Now.Ticks / 10000
            };


            int ret = RtcEngine.PushVideoFrame(frame, _customVideoTrackId);
            _pushFrameCounter++;

            if (_pushFrameCounter % 30 == 0)
            {
                DebugState($"[PUSH] frame={_pushFrameCounter}, ret={ret}, {width}x{height}");
            }
        }

        /* ================= UI ================= */
        // 비디오 뷰 띄우는 함수
        // MR-AR 1:1 전용: 슬롯 배정 없이 remoteVideoContainer에 바로 붙인다
        internal static void MakeVideoView(uint uid, string channelId, Transform container)
        {
            if (container == null)
            {
                Debug.LogError("[UI] remoteVideoContainer가 연결되지 않았습니다.");
                return;
            }

            // 이미 떠있으면 재사용 (중복 생성 방지)
            GameObject existing = GameObject.Find(uid.ToString());
            if (existing != null) return;

            // uid이름으로 오브젝트 만듦
            GameObject go = new GameObject(uid.ToString());

            // 오브젝트의 부모를 remoteVideoContainer로 설정
            go.transform.SetParent(container, false);

            // 오브젝트에 rawImage 컴포넌트추가
            RawImage img = go.AddComponent<RawImage>();
            img.color = Color.white;

            // 오브젝트의 엥커설정
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 오브젝트의 AspectRatioFilter 컴포넌트 추가 - 실제 수신 영상 크기가 도착하면
            // OnTextureSizeModify로 비율을 갱신한다(AR이 어떤 해상도를 보내든 레터박스가 맞게).
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f; // 실제 값 도착 전 임시 기본값
            var surface = go.AddComponent<VideoSurface>();
            surface.OnTextureSizeModify += (int width, int height) =>
            {
                if (height > 0) fitter.aspectRatio = (float)width / height;
            };

            surface.SetForUser(
                uid,
                channelId,
                uid == 0
                    ? VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CUSTOM // UID가 0이면 CUSTOM
                    : VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE // UID가 0이 아니면 REMOTE
            );

            surface.SetEnable(true);
        }

        // 비디오뷰 삭제
        internal static void DestroyVideoView(uint uid)
        {
            // UID 찾기
            GameObject go = GameObject.Find(uid.ToString());
            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        /* ================= Debug Helper ================= */

        internal void DebugState(string msg)
        {
            Debug.Log(msg);
            if (DebugStateText != null)
                DebugStateText.text = msg;
            if (Log != null)
                Log.UpdateLog(msg);
        }
    }

    /* ================= Agora Events ================= */

    internal class UserEventHandler : IRtcEngineEventHandler
    {

        // 비디오 토큰
        private readonly JoinChannelVideoToken _owner;

        // 유저 이벤트 핸들러
        internal UserEventHandler(JoinChannelVideoToken owner)
        {
            _owner = owner;
        }

        // (자신) 채널 참여 성공 → 방에 들어감
        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            _owner.DebugState("방에 들어갔습니다");
            _owner._hasJoined = true; // 이후 패널이 꺼질 때 AR에게 종료 신호를 보내도 되는 상태
            // MR은 자기 영상을 publish하지 않으므로 자신(uid 0)의 비디오 슬롯은 만들지 않음
            //JoinChannelVideoToken.MakeVideoView(0, connection.channelId);
        }

        // 타 유저 참여시 → 상대방(AR)이 방에 들어옴
        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _owner.DebugState($"상대방이 들어왔습니다 (연결됨)");
            JoinChannelVideoToken.MakeVideoView(uid, connection.channelId, _owner.remoteVideoContainer);

            // 실제 영상이 연결됐으니 "연결 대기중" 오버레이는 숨김
            _owner.GetComponent<PanelConnectingOverlay>()?.HideOverlay();
        }

        // 타 유저 오프라인시 → 상대방이 나감
        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _owner.DebugState($"상대방이 나갔습니다");
            JoinChannelVideoToken.DestroyVideoView(uid);

            // 상대방이 나갔으니 다시 "연결 대기중" 오버레이 표시
            _owner.GetComponent<PanelConnectingOverlay>()?.ShowOverlay();
        }

        // 연결 상태 변화시 → 시도중/연결됨/끊김 등을 화면에 알기 쉽게 표시
        public override void OnConnectionStateChanged(
            RtcConnection connection,
            CONNECTION_STATE_TYPE state,
            CONNECTION_CHANGED_REASON_TYPE reason)
        {
            _owner._state = state;

            string label = state switch
            {
                CONNECTION_STATE_TYPE.CONNECTION_STATE_CONNECTING => "연결 시도 중...",
                CONNECTION_STATE_TYPE.CONNECTION_STATE_CONNECTED => "연결됨",
                CONNECTION_STATE_TYPE.CONNECTION_STATE_RECONNECTING => "재연결 시도 중...",
                CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED => "연결 끊김 (대기 중)",
                CONNECTION_STATE_TYPE.CONNECTION_STATE_FAILED => "연결 실패",
                _ => state.ToString()
            };

            _owner.DebugState($"{label}");
        }
    }

    // 토큰 서버(TempServer/agora.js) 응답 파싱용
    // 서버 응답 형식: { "token": "..." }
    [System.Serializable]
    internal class TokenServerResponse
    {
        public string token;
    }
}