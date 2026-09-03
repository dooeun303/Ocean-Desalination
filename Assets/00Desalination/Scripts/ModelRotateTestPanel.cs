using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using GLTFast;

// "테스트: 모델 회전" - 화상통화와 무관한 독립 테스트 기능. 카메라 우측 하단에 항상 작은
// 토글 버튼이 떠 있고(WorkGuidePanel.cs와 동일한 HUD-follow 월드스페이스 캔버스 방식),
// 누르면 3D 모델(실제 설비(펌프) 모델 - MR.unity 씬의 pump1/pump2를 glTFast Export로 GLB로 뽑아
// 서버에 올려둔 것, Assets/00Desalination/Editor/PumpGltfExporter.cs 참고. AR쪽
// ModelRotateTestDemo와 동일 URL)을 카메라 앞에 소환해서
// 컨트롤러(XRGrabInteractable)로 잡고 돌릴 수 있게 한다. 잡고 있는 동안 회전값이 바뀔
// 때마다(각도 차이 0.1도 이상, 15Hz로 스로틀) VideoCallSignalingMR.SendModelRotation()으로
// 스트리밍되어 AR쪽 "모델 회전 테스트" 화면에 실시간 반영된다. 컨트롤러 레이로 특정 부위를
// 가리키면 그 부위(콜라이더=GLB 노드 이름) 이름을 model_point로 함께 보내 AR에서도 같은
// 부위가 하이라이트되게 한다.
// WorkGuidePanel.cs와 동일하게 [RuntimeInitializeOnLoadMethod]로 씬 수정 없이 자동 부팅한다.
public class ModelRotateTestPanel : MonoBehaviour
{
    public static ModelRotateTestPanel Instance;

    // 2026-08-24: MR 통합 도크(MrCallDockPanel) "3D모델" 탭에 넣기 위해 추가. 모델 자체는
    // XRGrabInteractable로 손으로 잡고 돌리는 3D 오브젝트라 평평한 UI 패널 안에 억지로 넣으면
    // 잡기 상호작용이 꼬일 수 있어서, 모델은 지금처럼 카메라 앞 공간에 그대로 띄우고 세 버튼
    // (토글/분해도/포인터모드)만 도크 탭 안으로 옮긴다. WorkGuidePanel.SetEmbedParent와 이름은
    // 비슷하지만 대상이 다름(전체 패널이 아니라 버튼들만).
    RectTransform controlsEmbedParent;
    bool controlsEmbedded;


    // 2026-08-14 펌프 대신 컨트롤러로 교체 - "건전지 교체" 데모 시나리오에 맞춰 XR Interaction
    // Toolkit 샘플의 컨트롤러 프리팹을 glTFast Export로 뽑았다(Assets/00Desalination/Editor/
    // ControllerGltfExporter.cs 참고). AR쪽 ModelRotateTestDemo와 동일 URL.
    // 2026-08-25 시연 시나리오가 "컨트롤러 건전지 교체"에서 "고압펌프 베어링 마모 점검"으로
    // 바뀌면서 다시 펌프 모델로 - AR쪽 ModelRotateTestDemo.TestModelUrl과 반드시 동일해야 함.
    const string TestModelUrl = "http://192.168.0.66:3000/uploads/pump_export.glb";
    // 베어링 인출/유격 측정 단계에서 펌프 전체 대신 보여줄 단독 베어링 모델 - AR쪽
    // ModelRotateTestDemo.BearingModelUrl과 반드시 동일해야 함.
    const string BearingModelUrl = "http://192.168.0.66:3000/uploads/bearing_export.glb";
    const float TargetSize = 0.5f;
    const float SendInterval = 1f / 15f; // 15Hz로 스트리밍 - 너무 자주 보내면 서버/소켓에 부담
    const float PointSendInterval = 1f / 15f;
    const float KeyboardRotateSpeed = 90f; // 에디터 테스트용 W/S 키보드 회전 속도(도/초)
    const float PointerToleranceRadius = 0.02f; // 베어링처럼 작은 부품 포인팅 보조 SphereCast 반지름
    const float PointerMaxDistance = 10f; // 보조 SphereCast 최대 사거리

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("ModelRotateTestPanel");
        go.AddComponent<ModelRotateTestPanel>();
        DontDestroyOnLoad(go);
    }

    GameObject toggleCanvasGo;
    Text toggleLabel;
    Image toggleBg;

    GameObject explodeCanvasGo;
    Text explodeLabel;
    Image explodeBg;

    GameObject markerCanvasGo;
    Text markerLabel;
    Image markerBg;
    bool markerMode; // false = 부위 하이라이트, true = 포인트 마커(빨간 구) - AR쪽 "모델 회전 테스트"/"모델 포인터 테스트" 두 메뉴와 동일한 두 표시 방식을 MR에서는 버튼 하나로 전환
    GameObject pointMarker;

    GameObject modelRoot;
    GltfImport currentGltf;
    XRGrabInteractable grabInteractable;
    bool modelActive;
    bool loadInFlight;
    float sendTimer;
    Quaternion lastSentRotation;
    string currentModelUrl; // 지금 떠 있는 모델이 어느 URL인지 - 작업가이드 단계에 맞춰 바꿀 때 비교용
    int loadToken; // AR쪽 ModelRotateTestDemo와 동일한 이유(Load 도중 또 Load가 불리면 먼저 것이
    // 나중에 끝나며 modelRoot를 덮어써 이전 모델이 안 지워지고 남는 버그 방지)로 순번표를 둔다.
    bool lastEmbedVisible = true; // 3D모델 탭이 지금 보이는지 - 바뀔 때만 AR에 model_view를 보내기 위한 비교용

    readonly List<Collider> pointColliders = new List<Collider>();
    readonly Dictionary<string, Renderer> partRenderers = new Dictionary<string, Renderer>();
    Renderer highlightedRenderer;
    Material[] highlightedOriginalMaterials;
    float pointSendTimer;
    bool wasPointing;
    XRRayInteractor[] rayInteractors;

    const float ExplodeDistance = 0.01f; // TargetSize(0.25) 기준 - 부품이 원래 크기의 약 4% 거리로 흩어짐
    const float ExplodeSpeed = 2f; // 1이 "완전히 펼쳐짐"이 되는 데 걸리는 속도(초당)
    readonly Dictionary<Transform, (Vector3 origLocal, Vector3 dir)> explodeData = new Dictionary<Transform, (Vector3, Vector3)>();
    bool exploded;
    float explodeT;

    string channelName;

    void Awake()
    {
        Instance = this;
        BuildToggleUI();
        BuildExplodeButton();
        BuildMarkerModeButton();
        // 2026-08-24: "화상통화 시작 전부터 토글 버튼이 항상 떠 있음" 피드백 - 원래는 이 스크립트만
        // WorkGuidePanel/LiveDrawOverlay와 달리 통화 상태를 구독하지 않아서 앱 시작부터 계속 떠
        // 있었다. 나머지 두 패널과 동일하게 OnCallAccepted/OnCallEnded에 맞춰 켜고 끈다.
        toggleCanvasGo.SetActive(false);
    }

    // 세 버튼을 parent 안 오른쪽에 위→아래로 쌓는다. 버튼 자체는 260x80 고정 크기 그대로 두고
    // (도크 탭 영역이 그 정도는 넉넉히 들어가는 크기라 별도 축소 없이 자연스러운 크기로 맞음)
    // 앵커 위치만 parent 기준 위/중간/아래 세 지점으로 바꾼다.
    // 2026-08-24: 모델 본체도 이 박스 안에 같이 들어오게 되면서(OpenModel 참고) 버튼을 중앙(0.5)
    // 대신 오른쪽(0.86)으로 밀어 왼쪽에 모델이 놓일 자리를 비워뒀다.
    public void SetControlsEmbedParent(RectTransform parent)
    {
        if (parent == null) return;
        controlsEmbedParent = parent;
        controlsEmbedded = true;

        PlaceEmbeddedControl(toggleCanvasGo.transform, parent, 0.83f);
        PlaceEmbeddedControl(explodeCanvasGo.transform, parent, 0.5f);
        PlaceEmbeddedControl(markerCanvasGo.transform, parent, 0.17f);
    }

    static void PlaceEmbeddedControl(Transform canvasTransform, RectTransform parent, float verticalAnchor)
    {
        canvasTransform.SetParent(parent, false);
        var rt = (RectTransform)canvasTransform;
        rt.anchorMin = new Vector2(0.86f, verticalAnchor);
        rt.anchorMax = new Vector2(0.86f, verticalAnchor);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnCallAccepted += HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded += HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected += HandleCallRejected;
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallAccepted -= HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected -= HandleCallRejected;
    }

    void HandleCallAccepted(string channel)
    {
        channelName = channel;
        toggleCanvasGo.SetActive(true);
    }

    void HandleCallEnded(string channel)
    {
        if (channel != channelName) return;
        toggleCanvasGo.SetActive(false);
        if (modelActive) CloseModel(); // 통화 끝나면 열려 있던 모델/분해도 상태도 같이 정리
    }

    void HandleCallRejected(string channel)
    {
        toggleCanvasGo.SetActive(false);
    }

    void OnToggleClicked()
    {
        if (modelActive) CloseModel();
        else OpenModel();
    }

    void OnExplodeClicked()
    {
        if (!modelActive) return;
        exploded = !exploded;
        explodeLabel.text = exploded ? "조립하기" : "분해도 보기";
        explodeBg.color = exploded ? MrUiTheme.Warn : MrUiTheme.AccentSoft;
        VideoCallSignalingMR.Instance?.SendModelExplode(exploded);
    }

    // AR쪽처럼 "부위 하이라이트"와 "포인트 마커(빨간 구)"를 별도 메뉴로 나누는 대신, MR은 이미
    // 열려있는 패널에 버튼 하나로 전환한다 - 모델 회전 테스트 하나만 여는 흐름이라 메뉴를 늘리기보다
    // 토글이 자연스럽다. 네트워크로 보내는 model_point 데이터 자체는 모드와 무관하게 항상 동일하고
    // (부위 이름 + 로컬좌표), 그걸 로컬에서 "어떻게 보여줄지"만 모드에 따라 갈린다 - AR쪽도 마찬가지로
    // 수신 데이터는 하나인데 표시 방식만 두 가지(ModelRotateTestDemo.useMarkerMode)로 나뉘어 있다.
    void OnMarkerModeClicked()
    {
        markerMode = !markerMode;
        markerLabel.text = markerMode ? "포인터: 마커" : "포인터: 하이라이트";
        markerBg.color = markerMode ? MrUiTheme.Warn : MrUiTheme.AccentSoft;
        // 모드를 바꾸는 순간 이전 모드가 남겨둔 표시(하이라이트든 마커든)를 정리해야
        // 다음 UpdatePointing()에서 두 표시가 동시에 남아있는 상태가 안 생긴다.
        RestoreHighlight();
        if (pointMarker != null) pointMarker.SetActive(false);
        wasPointing = false;
    }

    // url을 넘기면 그 모델을(펌프 대신 베어링 등), 안 넘기면 기본 펌프를 연다. 이미 모델이 떠 있는
    // 상태에서 다른 url로 다시 부르면(Update의 단계 체크) 기존 모델을 지우고 새로 불러온다.
    async void OpenModel(string url = null)
    {
        if (loadInFlight) return;
        int myToken = ++loadToken;
        loadInFlight = true;
        modelActive = true;
        toggleLabel.text = "모델 회전 테스트 닫기";
        toggleBg.color = MrUiTheme.Danger;

        string loadUrl = url ?? TestModelUrl;
        var gltf = new GltfImport();
        bool loaded = await gltf.Load(loadUrl);
        if (myToken != loadToken) { gltf.Dispose(); return; } // 그 사이 더 최신 호출이 시작됨 - 조용히 포기
        if (!loaded)
        {
            Debug.LogWarning("[ModelRotateTestPanel] 모델 로드 실패: " + loadUrl);
            gltf.Dispose();
            loadInFlight = false;
            CloseModel();
            return;
        }

        // 이미 떠 있던 모델(단계가 바뀌어 다시 불린 경우)은 여기서 지운다 - 새 모델 인스턴스화
        // 직전에 지워야 화면에 잠깐이라도 두 모델이 겹쳐 보이는 일이 없다.
        DestroyCurrentModel();

        modelRoot = new GameObject("MRRotateTestModel");
        // 2026-08-24: "AR처럼 모델도 박스 안에 있어야 한다" 피드백 - 도크 "3D모델" 탭에 임베드된
        // 경우엔 그 박스 안(controlsEmbedParent 기준 로컬좌표)에 두고, 임베드 안 된 경우(다른 씬)엔
        // 기존처럼 카메라 앞 1m에 띄운다(손을 뻗어 잡을 수 있는 거리).
        if (controlsEmbedParent != null)
        {
            modelRoot.transform.SetParent(controlsEmbedParent, false);
            modelRoot.transform.localPosition = new Vector3(-150f, 0f, 0f); // 오른쪽 버튼 칸(X=0.86)을 피해 빈 공간 중앙쯤에 위치
            modelRoot.transform.localRotation = Quaternion.identity;
        }
        else
        {
            var cam = Camera.main;
            if (cam != null)
            {
                modelRoot.transform.position = cam.transform.position + cam.transform.forward * 1.0f;
                modelRoot.transform.rotation = Quaternion.identity;
            }
        }

        bool instantiated = await gltf.InstantiateMainSceneAsync(modelRoot.transform);
        if (myToken != loadToken) { gltf.Dispose(); Destroy(modelRoot); modelRoot = null; return; } // 위와 동일한 이유로 포기
        if (!instantiated)
        {
            Debug.LogWarning("[ModelRotateTestPanel] 모델 인스턴스화 실패: " + loadUrl);
            gltf.Dispose();
            Destroy(modelRoot);
            modelRoot = null;
            loadInFlight = false;
            CloseModel();
            return;
        }

        currentGltf = gltf;
        currentModelUrl = loadUrl;
        NormalizeScale(modelRoot);
        // 2026-08-26: "3D모델 눌러도 안 보인다" 실기 확인 - NormalizeScale은 Renderer.bounds(월드
        // 공간)로 재는데, 이 시점엔 modelRoot가 이미 controlsEmbedParent 밑에 들어가 있어서
        // bounds 자체가 부모의 HudScale(0.0015)까지 이미 반영된 상태다. 즉 TargetSize/maxDim
        // 계산만으로 이미 부모 스케일이 자동으로 상쇄돼 정확히 TargetSize(0.5m) 크기가 나온다.
        // 그런데 예전엔 여기서 lossyScale로 한 번 더 나눠서 "이중 보정"을 했었다 - 그 결과 부모
        // 스케일의 역수(1/0.0015 ≈ 667배)만큼 모델이 실제보다 훨씬 커져서, 카메라가 모델 속에
        // 파묻혀 아무것도 안 보이는 상태였다(콜라이더는 존재해서 "레이 인터랙터 찾음" 로그는 정상
        // 출력됐음). AR쪽 NormalizeAndCenter도 이런 추가 보정이 없다 - 그냥 지운다.
        SetupGrabInteraction(modelRoot);
        loadInFlight = false;

        // "MR에서 모델 회전 테스트를 열면 AR에도 같은 모델이 떠야 한다" - 실제로 열렸을 때만 보낸다
        // (embed된 탭이 지금 화면에 보이는 상태일 때만 - 안 그러면 탭 전환 중 잠깐 열렸다 닫히는
        // 걸로 오인해서 AR에 안 보이는 채로 열렸다는 신호를 보낼 수 있음).
        lastEmbedVisible = controlsEmbedParent == null || controlsEmbedParent.gameObject.activeInHierarchy;
        if (lastEmbedVisible)
            VideoCallSignalingMR.Instance?.SendModelView(true, currentModelUrl); // 실제로 로드한 URL(펌프 또는 베어링) 그대로
    }

    // AR쪽 ModelRotateTestDemo.NormalizeAndCenter와 반드시 동일해야 한다 - 여기서 중심을
    // 로컬 원점으로 옮기지 않으면, "모델 로컬좌표"의 기준점 자체가 MR/AR에서 어긋나서
    // model_point로 보낸 좌표가 양쪽에서 서로 다른 부위를 가리키게 된다(실기 확인:
    // MR에서 목을 가리켰는데 AR엔 머리 위에 찍히는 식으로 계속 어긋남).
    static void NormalizeScale(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        var localCenter = root.transform.InverseTransformPoint(bounds.center);
        foreach (Transform child in root.transform)
            child.localPosition -= localCenter;

        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDim > 0.0001f)
            root.transform.localScale = Vector3.one * (TargetSize / maxDim);
    }

    // XR Interaction Toolkit으로 컨트롤러가 잡을 수 있게 만든다 - Rigidbody+Collider는
    // glTF 임포트에 안 딸려오므로 직접 붙인다. 회전뿐 아니라 위치도 손을 따라가는 기본
    // XRGrabInteractable 동작을 그대로 쓴다(순수 제자리 회전만 원하면 나중에 위치 고정
    // 로직을 추가하면 됨 - 지금은 "잡고 돌리기"가 되는지 확인하는 첫 단계라 단순하게 감).
    //
    // 콜라이더는 그랩/포인팅 공용으로 각 메시 그대로의 MeshCollider만 쓴다 - 처음엔 그랩을
    // 더 너그럽게 하려고 오리 전체를 감싸는 구(Sphere) 콜라이더를 따로 뒀었는데, 그러면 레이가
    // (보이지 않는) 구 표면에 먼저 맞아버려서 그 안쪽의 실제 메시 콜라이더까지 도달을 못 하고,
    // 구와 실제 표면이 거의 겹치는 부분(목 근처)에서만 어느 쪽에 맞을지 애매해 결과가 들쭉날쭉
    // 했다(실기 확인). 콜라이더를 하나로 통일하면 이 가림 문제 자체가 없어진다.
    void SetupGrabInteraction(GameObject root)
    {
        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true; // XRGrabInteractable이 직접 포즈를 옮기므로 물리 시뮬레이션 불필요

        // XRBaseInteractable.Awake()가 colliders 리스트를 안 채워주면 GetComponentsInChildren로
        // 자동 수집하므로(트리거는 제외), AddComponent<XRGrabInteractable>() 전에 메시 콜라이더를
        // 먼저 붙여둬야 그랩이 이 콜라이더들을 인식한다.
        pointColliders.Clear();
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false; // Kinematic Rigidbody라 non-convex도 허용됨 - 실제 표면 그대로 씀
            pointColliders.Add(mc);
        }

        grabInteractable = root.AddComponent<XRGrabInteractable>();
        grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
        grabInteractable.throwOnDetach = false;
        // 2026-08-26: "잡고 W/S 누르면 모델이 메뉴 뒤로 이동하거나 화면에 너무 가까워짐" - 처음엔
        // trackPosition을 통째로 꺼서 고쳤는데, 그러면 A/D(X축 이동)까지 같이 죽어버렸다("A/D가
        // 기존 그대로였으면 좋겠어" 피드백) - trackPosition은 축 단위로 끌 수 없어서, 대신 원인인
        // W/S 자체를 XR Device Simulator의 "Keyboard Z Translate" 바인딩에서 아예 빼버렸다
        // (XR Device Simulator Controls.inputactions 참고 - 이제 그 액션엔 아무 키도 안 물려있음).
        // 그래서 trackPosition은 그대로 켜둔 채(A/D는 원래처럼 동작), W/S만 순수하게 회전 전용
        // 키가 된다.

#if UNITY_EDITOR
        // "회전이 이상하다" - trackRotation이 켜진 채로는 XRGrabInteractable이 매 프레임 인터랙터
        // 회전값으로 modelRoot.rotation을 절대값 덮어쓰기 하고 있어서, 아래 Update()의 W/S
        // 키보드 회전(Rotate 누적)이 그 다음 프레임에 바로 지워지며 서로 충돌했다(떨리거나
        // 눌러도 거의 안 도는 증상). 에디터(키보드 테스트) 빌드에서만 trackRotation을 꺼서
        // 회전을 키보드가 전적으로 담당하게 한다 - 실기기 빌드는 이 블록 자체가 없어서 실제
        // 컨트롤러를 손으로 돌리는 기존 동작(trackRotation)에는 전혀 영향 없다.
        grabInteractable.trackRotation = false;
#endif

        rayInteractors = FindObjectsOfType<XRRayInteractor>();

        partRenderers.Clear();
        foreach (var r in root.GetComponentsInChildren<Renderer>())
            partRenderers[r.gameObject.name] = r; // AR쪽 partRenderers 키(=같은 GLB 노드 이름)와 그대로 매칭됨

        BuildExplodeData();

        Debug.Log($"[ModelRotateTestPanel][진단] 레이 인터랙터 {rayInteractors.Length}개 찾음, " +
            $"메시 콜라이더 {pointColliders.Count}개, 파츠 {partRenderers.Count}개 생성");
    }

    static readonly Vector3[] ExplodeDirections =
    {
        Vector3.up, Vector3.right, Vector3.forward, Vector3.down, Vector3.left, Vector3.back
    };

    // 각 부위를 modelRoot 중심에서 바깥으로 밀어낼 방향/원위치를 미리 계산해둔다. modelRoot의
    // "로컬" 좌표 기준으로 계산해서(InverseTransformPoint), 모델을 잡고 돌리는 중이어도
    // Update()에서 매 프레임 다시 월드좌표로 환산하면 항상 올바른 방향으로 펼쳐진다.
    //
    // 방향은 지오메트리(transform.position이든 렌더러 바운드 중심이든)로 추정하지 않고 부위
    // 순서대로 그냥 배정한다 - ToyCar의 바퀴처럼 모델 중심 기준으로 대칭인 부위는 노드
    // transform도, 렌더러 바운드 중심도 죄다 원점 근처로 나와서, 지오메트리 기반으로는 어떻게
    // 계산해도 부위들이 다같이 한 덩어리처럼 같은 방향/거리로 움직여버렸다(실기 확인 2회).
    // "물리적으로 자연스러운 방향"은 포기하고, 최소한 부위별로 확실히 갈라지는 걸 우선한다.
    void BuildExplodeData()
    {
        explodeData.Clear();
        int i = 0;
        foreach (var r in partRenderers.Values)
        {
            var t = r.transform;
            Vector3 origLocal = modelRoot.transform.InverseTransformPoint(t.position);
            Vector3 dir = ExplodeDirections[i % ExplodeDirections.Length];
            explodeData[t] = (origLocal, dir);
            i++;
        }
    }

    void UpdateExplodeAnimation()
    {
        if (modelRoot == null || explodeData.Count == 0) return;
        float target = exploded ? 1f : 0f;
        if (Mathf.Approximately(explodeT, target)) return;

        explodeT = Mathf.MoveTowards(explodeT, target, Time.deltaTime * ExplodeSpeed);
        foreach (var kv in explodeData)
        {
            var part = kv.Key;
            var (origLocal, dir) = kv.Value;
            Vector3 targetLocal = origLocal + dir * (ExplodeDistance * explodeT);
            part.position = modelRoot.transform.TransformPoint(targetLocal);
        }
    }

    void ApplyHighlight(Renderer target)
    {
        highlightedRenderer = target;
        highlightedOriginalMaterials = target.sharedMaterials;

        var highlightMats = new Material[highlightedOriginalMaterials.Length];
        for (int i = 0; i < highlightMats.Length; i++)
            highlightMats[i] = new Material(Shader.Find("Unlit/Color")) { color = MrUiTheme.Warn };
        target.sharedMaterials = highlightMats;
    }

    void RestoreHighlight()
    {
        if (highlightedRenderer == null) return;
        highlightedRenderer.sharedMaterials = highlightedOriginalMaterials;
        highlightedRenderer = null;
        highlightedOriginalMaterials = null;
    }

    // 화면에 보이는 실제 마커 크기 목표(TargetSize의 5%) - modelRoot 로컬(원본) 단위 고정값을
    // 썼더니, 컨트롤러처럼 원본이 작은 모델에서는 확대 배율(S=TargetSize/원본최대치수)이 펌프보다
    // 훨씬 커서 마커가 모델을 거의 뒤덮을 만큼 커져버렸다(실기 확인: "빨간 원이 너무 크게 나와" -
    // 컨트롤러로 모델을 바꾼 뒤에 나타남, AR쪽도 동일하게 고침). modelRoot.localScale로 나눠서
    // 항상 "실제 보이는 크기" 기준으로 계산하면 원본 모델이 뭐든 마커 크기가 일정하게 유지된다.
    const float MarkerWorldRadius = TargetSize * 0.05f;
    // localScale이 아니라 lossyScale(월드 기준 최종 배율)로 나눠야 한다 - modelRoot는 도크(부모)
    // 스케일까지 겹쳐 있어서 localScale만으로는 실제 월드 크기를 못 구한다. 예전엔 NormalizeScale의
    // "이중 보정" 버그 때문에 localScale이 우연히 아주 큰 값이라 이 나눗셈이 그럭저럭 비슷하게
    // 맞아떨어졌는데, 그 버그를 고치고 나니 이 마커만 실제로는 안 보일 만큼 작아졌다(실기 확인:
    // "빨간 포인터가 안 보여").
    float MarkerLocalRadius => MarkerWorldRadius / Mathf.Max(0.0001f, modelRoot.transform.lossyScale.x);

    // 포인터 모드에서 가리킨 지점에 뜨는 빨간 구 마커 - modelRoot의 자식으로 붙여서
    // localPosition을 그대로 InverseTransformPoint 결과로 세팅하면 되고, 모델이 회전/이동해도
    // 같이 따라간다. 콜라이더를 없애야 마커 자신이 다음 포인팅 레이캐스트에 걸리지 않는다.
    GameObject CreatePointMarker()
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "MRPointMarker";
        var col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);
        marker.transform.SetParent(modelRoot.transform, false);
        marker.transform.localScale = Vector3.one * MarkerLocalRadius;
        var rend = marker.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Color")) { color = MrUiTheme.Danger };
        marker.SetActive(false);
        return marker;
    }

    // 씬에 이미 있는 XR 레이 인터랙터(컨트롤러 UI 클릭용으로 쓰던 것)를 그대로 재사용해서,
    // 그 레이가 지금 모델의 어느 부위(콜라이더=GLB 노드 이름)에 맞고 있는지 확인한다 - 새 입력
    // 바인딩을 따로 만들 필요가 없다. 맞고 있으면 그 부위 이름+로컬좌표를 AR로 스트리밍하고,
    // 확인용으로 MR에도 같은 부위를 하이라이트한다. 레이가 모델을 벗어나면 pointing=false를
    // 한 번만 보내 AR의 하이라이트도 해제시킨다.
    void UpdatePointing()
    {
        if (pointColliders.Count == 0 || rayInteractors == null) return;

        bool pointing = false;
        Vector3 localPoint = Vector3.zero;
        Vector3 localNormal = Vector3.zero;
        string partName = null;
        foreach (var ray in rayInteractors)
        {
            if (ray == null) continue;

            bool hitFound = ray.TryGetCurrent3DRaycastHit(out var hit) && pointColliders.Contains(hit.collider);

            // 2026-08-26: "베어링은 포인터가 간헐적으로만 뜸" - 베어링처럼 작고 얇은 부품(볼/링)은
            // 씬 공용 XRRayInteractor의 얇은 직선 레이로는 살짝만 빗나가도 못 맞춘다. 그 인터랙터
            // 자체의 레이캐스트 설정(다른 잡기/UI 클릭에도 영향을 줌)은 안 건드리고, 이 모델의
            // 콜라이더(pointColliders)만 대상으로 두꺼운 SphereCast를 보조로 한 번 더 쏴서
            // 관용도를 넓힌다 - 원래 레이가 이미 맞혔으면 이 보조 캐스트는 안 돈다(성능 절약).
            if (!hitFound)
            {
                ray.GetLineOriginAndDirection(out var origin, out var direction);
                var hits = Physics.SphereCastAll(origin, PointerToleranceRadius, direction, PointerMaxDistance);
                float closestDist = float.MaxValue;
                foreach (var h in hits)
                {
                    if (!pointColliders.Contains(h.collider)) continue;
                    if (h.distance < closestDist) { closestDist = h.distance; hit = h; hitFound = true; }
                }
            }

            if (hitFound)
            {
                pointing = true;
                localPoint = modelRoot.transform.InverseTransformPoint(hit.point);
                // 위치와 달리 "방향"이라 InverseTransformDirection을 써야 한다(평행이동 영향을
                // 안 받아야 함) - AR쪽 포인터 마커를 표면 바깥으로 띄우는 데 그대로 씀.
                localNormal = modelRoot.transform.InverseTransformDirection(hit.normal).normalized;
                partName = hit.collider.gameObject.name;
                break;
            }
        }

        if (markerMode)
        {
            if (pointMarker == null) pointMarker = CreatePointMarker();
            pointMarker.SetActive(pointing);
            // 마커 중심을 히트 지점에 그대로 두면 부피의 절반이 표면 안쪽에 파묻혀 보인다 -
            // 표면 법선 방향으로 마커 반지름만큼 밀어내서 표면 위에 얹힌 것처럼 보이게 한다.
            if (pointing) pointMarker.transform.localPosition = localPoint + localNormal * MarkerLocalRadius;
        }
        else
        {
            Renderer target = (pointing && partName != null && partRenderers.TryGetValue(partName, out var r)) ? r : null;
            if (target != highlightedRenderer)
            {
                RestoreHighlight();
                if (target != null) ApplyHighlight(target);
            }
        }

        pointSendTimer += Time.deltaTime;
        bool intervalElapsed = pointSendTimer >= PointSendInterval;
        bool stoppedPointing = wasPointing && !pointing;

        if (pointing && intervalElapsed)
        {
            pointSendTimer = 0f;
            VideoCallSignalingMR.Instance?.SendModelPoint(true, partName, localPoint, localNormal);
        }
        else if (stoppedPointing)
        {
            VideoCallSignalingMR.Instance?.SendModelPoint(false, null, Vector3.zero, Vector3.zero);
        }

        wasPointing = pointing;
    }

    void Update()
    {
        // 토글 버튼을 카메라 시야 우측에 항상 붙여둔다(HUD) - WorkGuidePanel.cs와 동일 패턴.
        // Z(거리)/스케일을 MrUiTheme.HudDepth/HudScale로 통일 - 나머지는 X/Y만 다르게 해서 서로
        // 다른 "칸"에 떠 있는 것처럼 보이게 한다.
        //
        // 2026-08-24: 기존 Y값(-0.25/-0.4/-0.55)이 WorkGuidePanel(그때 Y=-0.25)과 거의 같은 자리라
        // 실제로 겹쳐 있었다. WorkGuidePanel을 Y=-0.62 아래쪽으로 옮긴 김에, 이 세 버튼은 그 위
        // 빈 공간(대략 LiveDrawOverlay 아래쪽 끝 Y≈-0.37과 WorkGuidePanel 위쪽 끝 Y≈-0.40 사이)에
        // 세로로 쌓이게 조정. 빌드해서 실기기로 세 패널이 실제로 안 겹치는지 확인 필요.
        // 2026-08-24: MrCallDockPanel "3D모델" 탭에 임베드됐으면(SetControlsEmbedParent 호출됨)
        // 이 세 버튼은 이미 그 탭의 자식으로 옮겨져 있으므로 카메라 추적을 안 한다 - 도크 자신이
        // 위치를 관리함(모델 본체는 임베드 대상이 아니라서 이 조건과 무관하게 계속 카메라 앞에 뜸).
        var cam = Camera.main;
        if (!controlsEmbedded && cam != null)
        {
            if (toggleCanvasGo.transform.parent != cam.transform)
                toggleCanvasGo.transform.SetParent(cam.transform, false);
            toggleCanvasGo.transform.localPosition = new Vector3(0.45f, 0.02f, MrUiTheme.HudDepth);
            toggleCanvasGo.transform.localRotation = Quaternion.identity;
            toggleCanvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;

            if (explodeCanvasGo.transform.parent != cam.transform)
                explodeCanvasGo.transform.SetParent(cam.transform, false);
            explodeCanvasGo.transform.localPosition = new Vector3(0.45f, -0.14f, MrUiTheme.HudDepth); // 토글 버튼 바로 아래
            explodeCanvasGo.transform.localRotation = Quaternion.identity;
            explodeCanvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;

            if (markerCanvasGo.transform.parent != cam.transform)
                markerCanvasGo.transform.SetParent(cam.transform, false);
            markerCanvasGo.transform.localPosition = new Vector3(0.45f, -0.30f, MrUiTheme.HudDepth); // 분해도 버튼 바로 아래
            markerCanvasGo.transform.localRotation = Quaternion.identity;
            markerCanvasGo.transform.localScale = Vector3.one * MrUiTheme.HudScale;
        }

        explodeCanvasGo.SetActive(modelActive);
        markerCanvasGo.SetActive(modelActive);

        // "MR이 3D모델 탭을 벗어나면 AR에서도 없어져야 한다" - 3D모델 탭이 안 보이는 동안엔(다른
        // 탭 선택 등) modelContent가 SetActive(false)돼서 modelRoot도 같이 안 보이게 되는데, 그
        // 시점을 감지해서 AR에도 닫힘/다시 열림을 알려준다. MR 쪽 modelActive 자체(닫기 버튼)는
        // 안 건드리고, 화면에 보이는지 여부만 AR로 중계한다.
        if (modelActive && controlsEmbedParent != null)
        {
            bool nowVisible = controlsEmbedParent.gameObject.activeInHierarchy;
            if (nowVisible != lastEmbedVisible)
            {
                lastEmbedVisible = nowVisible;
                VideoCallSignalingMR.Instance?.SendModelView(nowVisible, nowVisible ? currentModelUrl : null);
            }
        }

        // 작업가이드 단계가 "베어링 인출/육안검사"·"유격 측정"이면 펌프 대신 베어링 단독 모델로
        // 자동 전환한다(AR쪽과 동일 연동, manualIndex 3,4에 대응). 모델이 이미 열려 있을 때만
        // 반응하고, 안 열려 있으면(토글 안 누른 상태) 그냥 둔다 - 도구 버튼을 억지로 안 연다.
        if (modelActive && !loadInFlight)
        {
            var guide = WorkGuidePanel.Instance;
            if (guide != null)
            {
                bool wantBearing = guide.StepIndex == 3 || guide.StepIndex == 4;
                string desiredUrl = wantBearing ? BearingModelUrl : TestModelUrl;
                if (currentModelUrl != desiredUrl)
                    OpenModel(desiredUrl);
            }
        }

        if (!modelActive || modelRoot == null || grabInteractable == null) return;

        UpdateExplodeAnimation();
        UpdatePointing();

        if (!grabInteractable.isSelected) return;

#if UNITY_EDITOR
        // 2026-08-26: "컨트롤러로 모델을 잡고 w를 누르면 앞면이, s를 누르면 뒷면이 나오게 회전하고
        // 싶어" - 에디터에서 XR Device Simulator로 테스트할 때는 실제 컨트롤러를 손으로 돌릴 수
        // 없으니, 잡은 상태에서 W/S로 Y축 회전을 직접 조작할 수 있게 한다(실기기에서는 이 블록이
        // 컴파일 자체가 안 되므로 실제 컨트롤러로 손목을 돌리는 동작에는 영향 없음).
        float keyboardRotateInput = 0f;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // 프로젝트가 Input System 패키지 전용으로 설정돼 있어 레거시 UnityEngine.Input은
            // InvalidOperationException을 던진다(실기 확인) - Keyboard.current로 대체.
            if (keyboard.wKey.isPressed) keyboardRotateInput += 1f;
            if (keyboard.sKey.isPressed) keyboardRotateInput -= 1f;
        }
        if (keyboardRotateInput != 0f)
            // 요청: "옆면(ㅡ)에서 위에서 보는 모습(ㅇ)으로" - Y축(수직) 회전은 계속 옆면(테두리)만
            // 보여준다(베어링 링 축이 Y라서). 위/아래로 기울여야(X축 피치) 옆면 → 상판 뷰로 바뀐다.
            modelRoot.transform.Rotate(Vector3.right, keyboardRotateInput * KeyboardRotateSpeed * Time.deltaTime, Space.World);
#endif

        sendTimer += Time.deltaTime;
        if (sendTimer < SendInterval) return;
        sendTimer = 0f;

        var rot = modelRoot.transform.rotation;
        if (Quaternion.Angle(rot, lastSentRotation) > 0.1f)
        {
            VideoCallSignalingMR.Instance?.SendModelRotation(rot);
            lastSentRotation = rot;
        }
    }

    // 모델 지오메트리/상호작용 상태만 정리한다(modelActive나 버튼 라벨은 안 건드림) - 단계가
    // 바뀌어 펌프↔베어링으로 다시 불러올 때(OpenModel)도 재사용하기 위해 CloseModel에서 분리했다.
    void DestroyCurrentModel()
    {
        currentGltf?.Dispose();
        currentGltf = null;
        currentModelUrl = null;
        if (modelRoot != null) Destroy(modelRoot);
        modelRoot = null;
        grabInteractable = null;
        pointColliders.Clear(); // modelRoot의 자식이라 이미 같이 파괴됨 - 참조만 정리
        partRenderers.Clear();
        highlightedRenderer = null;
        highlightedOriginalMaterials = null;
        pointMarker = null; // modelRoot의 자식이라 이미 같이 파괴됨 - 참조만 정리
        wasPointing = false;
        explodeData.Clear();
        exploded = false;
        explodeT = 0f;
        explodeLabel.text = "분해도 보기";
        explodeBg.color = MrUiTheme.AccentSoft;
    }

    void CloseModel()
    {
        modelActive = false;
        toggleLabel.text = "모델 회전 테스트";
        toggleBg.color = MrUiTheme.AccentSoft;
        DestroyCurrentModel();
        VideoCallSignalingMR.Instance?.SendModelView(false);
    }

    void OnDestroy()
    {
        currentGltf?.Dispose();
    }

    void BuildToggleUI()
    {
        var font = MrUiTheme.CreateFont();

        toggleCanvasGo = new GameObject("ModelRotateTestToggleCanvas");
        var canvas = toggleCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = toggleCanvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260, 80);
        toggleCanvasGo.AddComponent<GraphicRaycaster>();
        toggleCanvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        var btnGo = new GameObject("ToggleButton");
        btnGo.transform.SetParent(toggleCanvasGo.transform, false);
        toggleBg = btnGo.AddComponent<Image>();
        toggleBg.color = MrUiTheme.AccentSoft;
        MrUiTheme.Round(toggleBg, 16);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero; btnRect.anchorMax = Vector2.one; btnRect.offsetMin = Vector2.zero; btnRect.offsetMax = Vector2.zero;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = toggleBg;
        btn.onClick.AddListener(OnToggleClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        toggleLabel = labelGo.AddComponent<Text>();
        toggleLabel.font = font;
        toggleLabel.text = "모델 회전 테스트";
        toggleLabel.fontSize = 22;
        toggleLabel.alignment = TextAnchor.MiddleCenter;
        toggleLabel.color = MrUiTheme.Ink;
        toggleLabel.raycastTarget = false;
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
    }

    // 모델 회전 테스트 토글 버튼 바로 아래에 뜨는 "분해도 보기" 버튼 - 모델이 떠 있을 때만
    // 보이게 Update()에서 SetActive로 켜고 끈다(BuildToggleUI와 동일한 HUD 패턴).
    void BuildExplodeButton()
    {
        var font = MrUiTheme.CreateFont();

        explodeCanvasGo = new GameObject("ModelExplodeToggleCanvas");
        var canvas = explodeCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = explodeCanvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260, 80);
        explodeCanvasGo.AddComponent<GraphicRaycaster>();
        explodeCanvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();
        explodeCanvasGo.SetActive(false);

        var btnGo = new GameObject("ExplodeButton");
        btnGo.transform.SetParent(explodeCanvasGo.transform, false);
        explodeBg = btnGo.AddComponent<Image>();
        explodeBg.color = MrUiTheme.AccentSoft;
        MrUiTheme.Round(explodeBg, 16);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero; btnRect.anchorMax = Vector2.one; btnRect.offsetMin = Vector2.zero; btnRect.offsetMax = Vector2.zero;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = explodeBg;
        btn.onClick.AddListener(OnExplodeClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        explodeLabel = labelGo.AddComponent<Text>();
        explodeLabel.font = font;
        explodeLabel.text = "분해도 보기";
        explodeLabel.fontSize = 22;
        explodeLabel.alignment = TextAnchor.MiddleCenter;
        explodeLabel.color = MrUiTheme.Ink;
        explodeLabel.raycastTarget = false;
        var labelRect2 = labelGo.GetComponent<RectTransform>();
        labelRect2.anchorMin = Vector2.zero; labelRect2.anchorMax = Vector2.one; labelRect2.offsetMin = Vector2.zero; labelRect2.offsetMax = Vector2.zero;
    }

    // 분해도 버튼 바로 아래에 뜨는 "포인터: 하이라이트"/"포인터: 마커" 전환 버튼 - BuildExplodeButton과
    // 동일한 HUD 패턴.
    void BuildMarkerModeButton()
    {
        var font = MrUiTheme.CreateFont();

        markerCanvasGo = new GameObject("ModelPointerModeToggleCanvas");
        var canvas = markerCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = markerCanvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260, 80);
        markerCanvasGo.AddComponent<GraphicRaycaster>();
        markerCanvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();
        markerCanvasGo.SetActive(false);

        var btnGo = new GameObject("MarkerModeButton");
        btnGo.transform.SetParent(markerCanvasGo.transform, false);
        markerBg = btnGo.AddComponent<Image>();
        markerBg.color = MrUiTheme.AccentSoft;
        MrUiTheme.Round(markerBg, 16);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero; btnRect.anchorMax = Vector2.one; btnRect.offsetMin = Vector2.zero; btnRect.offsetMax = Vector2.zero;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = markerBg;
        btn.onClick.AddListener(OnMarkerModeClicked);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        markerLabel = labelGo.AddComponent<Text>();
        markerLabel.font = font;
        markerLabel.text = "포인터: 하이라이트";
        markerLabel.fontSize = 22;
        markerLabel.alignment = TextAnchor.MiddleCenter;
        markerLabel.color = MrUiTheme.Ink;
        markerLabel.raycastTarget = false;
        var labelRect3 = labelGo.GetComponent<RectTransform>();
        labelRect3.anchorMin = Vector2.zero; labelRect3.anchorMax = Vector2.one; labelRect3.offsetMin = Vector2.zero; labelRect3.offsetMax = Vector2.zero;
    }
}
