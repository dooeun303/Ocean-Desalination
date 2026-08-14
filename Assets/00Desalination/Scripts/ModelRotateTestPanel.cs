using System.Collections.Generic;
using UnityEngine;
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
    const string TestModelUrl = "http://192.168.0.66:3000/uploads/pump_export.glb"; // 실제 설비(펌프) 모델 - MR.unity 씬의 pump1/pump2를 glTFast Export로 뽑아 서버에 올려둔 것
    const float TargetSize = 0.5f;
    const float SendInterval = 1f / 15f; // 15Hz로 스트리밍 - 너무 자주 보내면 서버/소켓에 부담
    const float PointSendInterval = 1f / 15f;

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

    void Awake()
    {
        BuildToggleUI();
        BuildExplodeButton();
        BuildMarkerModeButton();
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
        explodeBg.color = exploded ? new Color(0.75f, 0.55f, 0.15f) : new Color(0.3f, 0.3f, 0.35f);
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
        markerBg.color = markerMode ? new Color(0.75f, 0.25f, 0.65f) : new Color(0.3f, 0.3f, 0.35f);
        // 모드를 바꾸는 순간 이전 모드가 남겨둔 표시(하이라이트든 마커든)를 정리해야
        // 다음 UpdatePointing()에서 두 표시가 동시에 남아있는 상태가 안 생긴다.
        RestoreHighlight();
        if (pointMarker != null) pointMarker.SetActive(false);
        wasPointing = false;
    }

    async void OpenModel()
    {
        if (loadInFlight) return;
        loadInFlight = true;
        modelActive = true;
        toggleLabel.text = "모델 회전 테스트 닫기";
        toggleBg.color = new Color(0.75f, 0.25f, 0.25f);

        var gltf = new GltfImport();
        bool loaded = await gltf.Load(TestModelUrl);
        if (!loaded)
        {
            Debug.LogWarning("[ModelRotateTestPanel] 모델 로드 실패: " + TestModelUrl);
            gltf.Dispose();
            loadInFlight = false;
            CloseModel();
            return;
        }

        modelRoot = new GameObject("MRRotateTestModel");
        // 카메라 앞 1m 지점에 배치 - 손을 뻗어 잡을 수 있는 거리
        var cam = Camera.main;
        if (cam != null)
        {
            modelRoot.transform.position = cam.transform.position + cam.transform.forward * 1.0f;
            modelRoot.transform.rotation = Quaternion.identity;
        }

        bool instantiated = await gltf.InstantiateMainSceneAsync(modelRoot.transform);
        if (!instantiated)
        {
            Debug.LogWarning("[ModelRotateTestPanel] 모델 인스턴스화 실패: " + TestModelUrl);
            gltf.Dispose();
            Destroy(modelRoot);
            modelRoot = null;
            loadInFlight = false;
            CloseModel();
            return;
        }

        currentGltf = gltf;
        NormalizeScale(modelRoot);
        SetupGrabInteraction(modelRoot);
        loadInFlight = false;
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
            highlightMats[i] = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.55f, 0.1f) };
        target.sharedMaterials = highlightMats;
    }

    void RestoreHighlight()
    {
        if (highlightedRenderer == null) return;
        highlightedRenderer.sharedMaterials = highlightedOriginalMaterials;
        highlightedRenderer = null;
        highlightedOriginalMaterials = null;
    }

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
        marker.transform.localScale = Vector3.one * (TargetSize * 0.06f);
        var rend = marker.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.9f, 0.15f, 0.15f) };
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
        string partName = null;
        foreach (var ray in rayInteractors)
        {
            if (ray == null) continue;
            if (ray.TryGetCurrent3DRaycastHit(out var hit) && pointColliders.Contains(hit.collider))
            {
                pointing = true;
                localPoint = modelRoot.transform.InverseTransformPoint(hit.point);
                partName = hit.collider.gameObject.name;
                break;
            }
        }

        if (markerMode)
        {
            if (pointMarker == null) pointMarker = CreatePointMarker();
            pointMarker.SetActive(pointing);
            if (pointing) pointMarker.transform.localPosition = localPoint;
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
            VideoCallSignalingMR.Instance?.SendModelPoint(true, partName, localPoint);
        }
        else if (stoppedPointing)
        {
            VideoCallSignalingMR.Instance?.SendModelPoint(false, null, Vector3.zero);
        }

        wasPointing = pointing;
    }

    void Update()
    {
        // 토글 버튼을 카메라 시야 우측 하단에 항상 붙여둔다(HUD) - WorkGuidePanel.cs와 동일 패턴.
        var cam = Camera.main;
        if (cam != null)
        {
            if (toggleCanvasGo.transform.parent != cam.transform)
                toggleCanvasGo.transform.SetParent(cam.transform, false);
            toggleCanvasGo.transform.localPosition = new Vector3(0.45f, -0.25f, 1.2f);
            toggleCanvasGo.transform.localRotation = Quaternion.identity;
            toggleCanvasGo.transform.localScale = Vector3.one * 0.0015f;

            if (explodeCanvasGo.transform.parent != cam.transform)
                explodeCanvasGo.transform.SetParent(cam.transform, false);
            explodeCanvasGo.transform.localPosition = new Vector3(0.45f, -0.4f, 1.2f); // 토글 버튼 바로 아래
            explodeCanvasGo.transform.localRotation = Quaternion.identity;
            explodeCanvasGo.transform.localScale = Vector3.one * 0.0015f;

            if (markerCanvasGo.transform.parent != cam.transform)
                markerCanvasGo.transform.SetParent(cam.transform, false);
            markerCanvasGo.transform.localPosition = new Vector3(0.45f, -0.55f, 1.2f); // 분해도 버튼 바로 아래
            markerCanvasGo.transform.localRotation = Quaternion.identity;
            markerCanvasGo.transform.localScale = Vector3.one * 0.0015f;
        }

        explodeCanvasGo.SetActive(modelActive);
        markerCanvasGo.SetActive(modelActive);

        if (!modelActive || modelRoot == null || grabInteractable == null) return;

        UpdateExplodeAnimation();
        UpdatePointing();

        if (!grabInteractable.isSelected) return;

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

    void CloseModel()
    {
        modelActive = false;
        toggleLabel.text = "모델 회전 테스트";
        toggleBg.color = new Color(0.25f, 0.45f, 0.85f);
        currentGltf?.Dispose();
        currentGltf = null;
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
        explodeBg.color = new Color(0.3f, 0.3f, 0.35f);
    }

    void OnDestroy()
    {
        currentGltf?.Dispose();
    }

    void BuildToggleUI()
    {
        var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 36);

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
        toggleBg.color = new Color(0.25f, 0.45f, 0.85f);
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
        toggleLabel.color = Color.white;
        toggleLabel.raycastTarget = false;
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
    }

    // 모델 회전 테스트 토글 버튼 바로 아래에 뜨는 "분해도 보기" 버튼 - 모델이 떠 있을 때만
    // 보이게 Update()에서 SetActive로 켜고 끈다(BuildToggleUI와 동일한 HUD 패턴).
    void BuildExplodeButton()
    {
        var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 36);

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
        explodeBg.color = new Color(0.3f, 0.3f, 0.35f);
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
        explodeLabel.color = Color.white;
        explodeLabel.raycastTarget = false;
        var labelRect2 = labelGo.GetComponent<RectTransform>();
        labelRect2.anchorMin = Vector2.zero; labelRect2.anchorMax = Vector2.one; labelRect2.offsetMin = Vector2.zero; labelRect2.offsetMax = Vector2.zero;
    }

    // 분해도 버튼 바로 아래에 뜨는 "포인터: 하이라이트"/"포인터: 마커" 전환 버튼 - BuildExplodeButton과
    // 동일한 HUD 패턴.
    void BuildMarkerModeButton()
    {
        var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 36);

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
        markerBg.color = new Color(0.3f, 0.3f, 0.35f);
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
        markerLabel.color = Color.white;
        markerLabel.raycastTarget = false;
        var labelRect3 = labelGo.GetComponent<RectTransform>();
        labelRect3.anchorMin = Vector2.zero; labelRect3.anchorMax = Vector2.one; labelRect3.offsetMin = Vector2.zero; labelRect3.offsetMax = Vector2.zero;
    }
}
