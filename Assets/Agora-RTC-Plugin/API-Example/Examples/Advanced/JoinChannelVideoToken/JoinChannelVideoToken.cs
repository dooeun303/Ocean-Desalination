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
using UnityEngine.Serialization;
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
        [SerializeField] private string _token = ""; // 토큰
        [SerializeField] private string _channelName = ""; // 채널명

        [Header("DEBUG UI")]
        [SerializeField] private RawImage DebugWebCamPreview; // WebCamTexture 직접 확인
        [SerializeField] private Text DebugStateText;

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

        private void Start()
        {
            LoadAssetData();
            if (!CheckAppId()) return; // AppID가 유효한지 확인 (유효하지 않으면 초기화 할 수 없음)

            InitExternalCamera(); // 외부 카메라 초기화
            InitEngine(); // 엔진 초기화
            CreateCustomVideoTrack(); // 커스텀비디오트랙 만들기
            JoinChannel(); // 채널 참여 
        }

        private void Update()
        {
            PermissionHelper.RequestCameraPermission(); // 카메라, 마이크 허가
            PermissionHelper.RequestMicrophontPermission();

            if (_webCam != null && _webCam.isPlaying)
            {
                PushExternalFrame();
            }
        }

        // 종료
        private void OnDestroy()
        {
            if (_webCam != null && _webCam.isPlaying)
                _webCam.Stop();

            if (RtcEngine != null)
            {
                RtcEngine.DestroyCustomVideoTrack(_customVideoTrackId); // 비디오트랙 파괴
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
        private void JoinChannel()
        {
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo();
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

            ChannelMediaOptions options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(false); // 카메라 퍼블리시 false
            options.publishCustomVideoTrack.SetValue(true); // 커스텀 비디오 트랙 퍼블리시 true
            options.customVideoTrackId.SetValue(_customVideoTrackId);

            DebugState("[JOIN] JoinChannel with Custom Video Track");

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
        internal static void MakeVideoView(uint uid, string channelId)
        {
            // 비디오슬롯매니저에 슬롯을 배정
            Transform slot = VideoSlotManager.Instance.AssignSlot(uid);
            if (slot == null)
            {
                Debug.LogError("[UI] Slot NULL");
                return;
            }

            // uid이름으로 오브젝트 만듦
            GameObject go = new GameObject(uid.ToString());

            // 오브젝트의 부모를 slot을 설정
            go.transform.SetParent(slot, false);

            // 오브젝트에 rawImage 컴포넌트추가
            RawImage img = go.AddComponent<RawImage>();
            img.color = Color.white;

            // 오브젝트의 엥커설정
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 오브젝트의 AspectRatioFilter 컴포넌트 추가 (얘를 조정해야함)
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 2f;
            var surface = go.AddComponent<VideoSurface>();

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
                // 슬롯 해제
                VideoSlotManager.Instance.ReleaseSlot(uid);
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

        // (자신) 채널 참여 성공
        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            _owner.DebugState("[EVENT] JoinChannelSuccess");
            JoinChannelVideoToken.MakeVideoView(0, connection.channelId);
        }

        // 타 유저 참여시
        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _owner.DebugState($"[EVENT] Remote Joined: {uid}");
            JoinChannelVideoToken.MakeVideoView(uid, connection.channelId);
        }

        // 타 유저 오프라인시
        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _owner.DebugState($"[EVENT] Remote Left: {uid}");
            JoinChannelVideoToken.DestroyVideoView(uid);
        } 

        // 연결 상태 변화시
        public override void OnConnectionStateChanged(
            RtcConnection connection,
            CONNECTION_STATE_TYPE state,
            CONNECTION_CHANGED_REASON_TYPE reason)
        {
            _owner._state = state;
            _owner.DebugState($"[STATE] {state} ({reason})");
        }
    }
}