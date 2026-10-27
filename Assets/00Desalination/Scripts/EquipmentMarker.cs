using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class EquipmentMarker : MonoBehaviour
{
    [Header("DB equipment 테이블의 id")]
    public string equipmentId;

    [Header("텔레포트 포인트 설비 앞 위치")]
    public Transform teleportPoint;

    [Header("하이라이트")]
    [Tooltip("외곽선 대상. 비우면 이 마커. 설비 메시가 마커와 다른 오브젝트에 있으면 그 루트를 지정")]
    public Transform highlightTarget;
    [Tooltip("바닥 링을 놓을 위치. 지정하면 이 Transform 위치에 그대로 뜬다(바운즈 계산 무시)")]
    public Transform ringAnchor;
    public Color rimColor = new Color(0.35f, 0.8f, 1f, 1f);
    public Color ringColor = new Color(0.35f, 0.8f, 1f, 1f);
    [Tooltip("Quick Outline 두께(대략 2~8). 0이면 외곽선 생략")]
    public float outlineWidth = 6f;
    [Tooltip("바닥 링 반지름(m). > 0 이면 이 값을 그대로. 0 이면 설비 바운즈에서 자동(0.4~1.5m)")]
    public float ringRadius = 0.6f;
    [Tooltip("하이라이트 때 설비 아래 바닥 링을 띄울지. 2026-10-20 요청으로 기본 끔(외곽선만)")]
    public bool showGroundRing = false;

    [Header("설비 정보 패널 (월드 스페이스)")]
    [Tooltip("표시할 정보 패널 루트. 비우면 이 오브젝트의 EquipmentPanel 하위 Canvas")]
    public GameObject infoPanel;
    [Tooltip("★가장 확실함★ 씬에서 패널 위치를 직접 잡아두고, 도착 시엔 켜기만 한다(이동/회전/스케일 안 건드림). infoPanelAnchor 도 무시")]
    public bool infoPanelJustActivate = false;
    [Tooltip("특정 Transform 위치/회전에 스냅. 비우면 XR Rig 기준 왼쪽 오프셋으로 계산 배치")]
    public Transform infoPanelAnchor;
    [Tooltip("XR Rig(바닥 원점) 로컬 기준 패널 위치 (x-: 왼쪽, z+: 앞, y: 높이). y는 바닥부터라 눈높이면 ~1.3")]
    public Vector3 infoPanelLocalOffset = new Vector3(-0.6f, 1.2f, 1.2f);
    [Tooltip("패널을 시야 중앙 쪽으로 돌리는 각도(도)")]
    public float infoPanelYaw = 20f;
    [Tooltip("패널 표시 스케일(월드). 0 이면 패널 원래 크기 유지. 이 프로젝트 패널은 대략 0.05")]
    public float infoPanelScale = 0.05f;
    [Tooltip("패널의 RectTransform 중심을 목표 지점에 맞춘다(피벗이 구석이어도 엉뚱한 곳으로 안 감)")]
    public bool infoPanelRecenter = true;
    [Tooltip("패널 앞면(+Z)이 사용자를 향하도록 회전. 글씨가 뒤집혀 보이면 이 값을 끄거나 Face Flip 을 켜라")]
    public bool infoPanelFaceViewer = true;
    [Tooltip("패널의 읽는 면이 -Z 인 경우 켜서 180도 뒤집는다")]
    public bool infoPanelFaceFlip = false;

    [Header("도크(화상통화) 패널 배치")]
    [Tooltip("도크 패널을 정보 패널 옆에 같이 띄운다(카메라 추적 HUD 대신 월드 고정)")]
    public bool placeDockBesideInfo = true;
    [Tooltip("정보 패널 로컬 기준 도크 위치(x+: 오른쪽). 정보 패널 폭에 맞게 조절")]
    public Vector3 dockPanelOffset = new Vector3(1.6f, 0f, 0f);

    // 마지막으로 Highlight()를 시작한 설비를 기억 - 어디서든 StopHighlight() 할 수 있게. MR-AR는 1:1.
    public static EquipmentMarker CurrentlyHighlighted { get; private set; }

    bool _highlighting;
    LineRenderer _ring;
    Coroutine _pulseCo;
    Outline _outline;

    // 2026-10-21: "공정정보 조회 패널은 그랩이 안돼?" - 설비(pump1)의 XRSimpleInteractable은 Colliders 목록이
    // 비어 있어서 자식 Collider를 전부 자기 것으로 가져가는데, 정보 패널이 그 자식이라 패널의 잡기용
    // BoxCollider까지 설비가 가져갔다(콘솔: "collider ... will remain associated with pump1").
    // 시작 후 한 프레임 뒤(XRI 등록이 끝난 뒤) 설비 쪽에서 패널 Collider를 빼고, 둘 다 다시 등록시킨다.
    IEnumerator Start()
    {
        yield return null;
        ReleasePanelCollidersFromParentInteractables();
    }

    void ReleasePanelCollidersFromParentInteractables()
    {
        var p = ResolveInfoPanel();
        if (p == null || p.transform.parent == null) return;
        var panelColliders = p.GetComponentsInChildren<Collider>(true);
        if (panelColliders.Length == 0) return;

        bool changed = false;
        foreach (var owner in p.transform.parent.GetComponentsInParent<XRBaseInteractable>(true))
        {
            int before = owner.colliders.Count;
            bool wasEnabled = owner.enabled;
            if (wasEnabled) owner.enabled = false; // 매니저 등록 해제 → 목록 수정 → 다시 등록
            owner.colliders.RemoveAll(c => System.Array.IndexOf(panelColliders, c) >= 0);
            if (wasEnabled) owner.enabled = true;
            changed |= owner.colliders.Count != before;
        }
        if (!changed) return;

        // 패널 쪽 잡기도 다시 등록해야 비워진 Collider를 가져간다.
        foreach (var grab in p.GetComponentsInChildren<XRBaseInteractable>(true))
        {
            if (!grab.enabled) continue;
            grab.enabled = false;
            grab.enabled = true;
        }
        Debug.Log($"[EquipmentMarker] {name}: 정보 패널 Collider를 설비 인터랙터블에서 분리 - 패널 잡기 가능");
    }

    // 텔레포트 시 XR Rig에 줄 회전 - 바라보는 수평 방향(Y축)만 쓰고 기울기(X/Z)는 버린다.
    // teleportPoint를 기울어진 설비 모델(FBX X -90 등)의 자식으로 두면 회전을 통째로 복사했을 때
    // 시점이 같이 기울었다. forward가 거의 수직이면(모델이 눕혀진 경우) up 축을 대신 쓴다.
    public static Quaternion UprightRotation(Transform point)
    {
        Vector3 dir = Vector3.ProjectOnPlane(point.forward, Vector3.up);
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.ProjectOnPlane(point.up, Vector3.up);
        return dir.sqrMagnitude < 0.0001f ? Quaternion.identity : Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    // 2026-09-03: 설비 외곽선 - Quick Outline(Asset) 컴포넌트를 하이라이트 동안 붙였다 제거 + 바닥 링 펄스.
    // StopHighlight() 전까지 유지(일회성 아님).
    public IEnumerator Highlight()
    {
        if (CurrentlyHighlighted != null && CurrentlyHighlighted != this)
            CurrentlyHighlighted.StopHighlight();

        if (_highlighting || _ring != null || _outline != null) StopHighlight(); // 자가 복구

        CurrentlyHighlighted = this;
        _highlighting = true;
        Debug.Log("[EquipmentMarker] Highlight 시작: " + name);

        Transform root = highlightTarget != null ? highlightTarget : transform;

        // ── 외곽선 (Quick Outline) ──
        // [DisallowMultipleComponent] 이라 파괴/재생성하면 다음 회차에 AddComponent가 실패한다.
        // 컴포넌트는 재사용하고 enable/disable 로만 켜고 끈다(Quick Outline은 OnDisable에서 머티리얼 정리).
        if (outlineWidth > 0f)
        {
            _outline = root.GetComponent<Outline>();
            if (_outline == null) _outline = root.gameObject.AddComponent<Outline>();
            _outline.OutlineMode = Outline.Mode.OutlineAll;
            _outline.OutlineColor = rimColor;
            _outline.OutlineWidth = outlineWidth;
            _outline.enabled = true;
            Debug.Log("[EquipmentMarker] Quick Outline 적용: " + root.name + " (enabled)");
        }

        // ── 바닥 링 ──
        if (!showGroundRing)
        {
            _pulseCo = StartCoroutine(Pulse()); // 외곽선 펄스만
            yield break;
        }
        Vector3 groundCenter;
        float boundsRadius = 0f;
        if (ringAnchor != null)
        {
            groundCenter = ringAnchor.position + new Vector3(0f, 0.02f, 0f);
        }
        else
        {
            var rends = root.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                    if (rends[i] != null) b.Encapsulate(rends[i].bounds);
                groundCenter = new Vector3(b.center.x, b.min.y + 0.02f, b.center.z);
                boundsRadius = Mathf.Clamp(Mathf.Max(b.extents.x, b.extents.z) * 0.6f, 0.4f, 1.5f);
            }
            else
            {
                groundCenter = root.position + new Vector3(0f, 0.02f, 0f);
            }
        }
        float baseRadius = ringRadius > 0f ? ringRadius : (boundsRadius > 0f ? boundsRadius : 0.6f);

        var ringGo = new GameObject("HighlightRing"); // 부모 없이 월드에(마커 스케일 영향 방지)
        ringGo.transform.position = groundCenter;
        ringGo.transform.rotation = Quaternion.identity;
        _ring = ringGo.AddComponent<LineRenderer>();
        _ring.useWorldSpace = false;
        _ring.loop = true;
        _ring.widthMultiplier = Mathf.Clamp(baseRadius * 0.04f, 0.015f, 0.05f);
        _ring.numCornerVertices = 2;
        _ring.material = new Material(Shader.Find("Sprites/Default"));
        _ring.startColor = _ring.endColor = ringColor;
        const int seg = 64;
        _ring.positionCount = seg;
        for (int i = 0; i < seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * baseRadius, 0f, Mathf.Sin(a) * baseRadius));
        }

        _pulseCo = StartCoroutine(Pulse());
        yield break;
    }

    IEnumerator Pulse()
    {
        float t = 0f;
        while (_highlighting)
        {
            t += Time.deltaTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(t * 3.0f);

            if (_ring != null)
            {
                _ring.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 3.0f));
                var c = ringColor; c.a = 0.55f + 0.45f * wave;
                _ring.startColor = _ring.endColor = c;
            }
            if (_outline != null)
                _outline.OutlineWidth = outlineWidth * (0.65f + 0.5f * wave); // 두께 은은한 펄스

            yield return null;
        }
    }

    // 밖에서 호출해 하이라이트를 즉시 멈추고 원래 상태로 되돌린다.
    public void StopHighlight()
    {
        _highlighting = false;
        if (CurrentlyHighlighted == this) CurrentlyHighlighted = null;

        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }

        if (_outline != null) { _outline.enabled = false; _outline = null; }
        if (_ring != null) { Destroy(_ring.gameObject); _ring = null; }
        HideInfo();
    }

    void OnDisable()
    {
        if (_highlighting) StopHighlight();
    }

    // ── 설비 정보 패널 ──
    GameObject _resolvedPanel; // ShowInfo가 부모를 떼면 GetComponentInChildren로는 다시 못 찾으므로 캐시
    bool? _infoWasActive;      // ShowInfo 직전 패널 켜짐 상태 - HideInfo가 되돌린다
    bool _infoHomeSaved;       // ShowInfo/MoveInfoPanel 전 원래 자리(부모·로컬 위치·회전·크기)
    Transform _infoHomeParent;
    Vector3 _infoHomePos;
    Quaternion _infoHomeRot;
    Vector3 _infoHomeScale;

    // 2026-10-27: 예전엔 MoveInfoPanel에서 위치·회전만 기억해서, ShowInfo가 부모를 떼고 Info Panel Scale(=1)로 키운 패널이
    // 통화가 끝나도 부모 없이 1:1 크기로 남았다(맨 앞에 그리기 후 다음 선택 때 화면을 뒤덮음). 맨 처음 손대기 전에 한 번 기억한다.
    void SaveInfoHome(GameObject p)
    {
        if (_infoHomeSaved) return;
        _infoHomeParent = p.transform.parent;
        _infoHomePos = p.transform.localPosition;
        _infoHomeRot = p.transform.localRotation;
        _infoHomeScale = p.transform.localScale;
        _infoHomeSaved = true;
    }

    GameObject ResolveInfoPanel()
    {
        if (_resolvedPanel != null) return _resolvedPanel;
        if (infoPanel != null) { _resolvedPanel = infoPanel; return _resolvedPanel; }
        var ep = GetComponent<EquipmentPanel>();
        if (ep == null) return null;
        var c = ep.GetComponentInChildren<Canvas>(true);
        _resolvedPanel = c != null ? c.gameObject : null;
        return _resolvedPanel;
    }

    // 데이터 갱신 + 패널을 "지금 viewer(XR Rig)가 있는 자리 기준 왼쪽"으로 계산해 월드에 고정 표시.
    // viewer 의 현재 위치/방향은 한 번만 참고하고, 패널은 부모 없이(SetParent null) 월드 좌표에 못박는다.
    // → 이후 고개를 돌려도 패널은 따라오지 않는다.
    public void ShowInfo(Transform viewer)
    {
        GetComponent<EquipmentPanel>()?.OnSelected();
        var p = ResolveInfoPanel();
        if (p == null) { Debug.LogWarning("[EquipmentMarker] 정보 패널을 못 찾음 - infoPanel 또는 EquipmentPanel 확인"); return; }

        // 통화가 끝나면(HideInfo) 이 상태로 되돌린다 - 씬에서 원래 켜져 있던 패널이 꺼진 채 남지 않게.
        if (_infoWasActive == null) _infoWasActive = p.activeSelf;
        MrArrivalState.Marker = this;

        // ★ 씬에 잡아둔 위치 그대로 쓰고 켜기만 한다. 이동/회전/스케일/리페어런트 전부 생략.
        // 2026-10-21: Info Panel Anchor가 패널 자기 자신(또는 그 자식)이면 "씬 자리 그대로"라는 뜻이라 같은 처리.
        // 예전엔 부모(설비)에서 떼고 Info Panel Scale(=1)을 덮어써서, 설비 안에서 작게 줄어 있던 패널이
        // 캔버스 원래 크기(수백 m)로 커지며 설비 밖으로 빠져나갔다(pump1).
        bool anchoredToSelf = infoPanelAnchor != null &&
                              (infoPanelAnchor == p.transform || infoPanelAnchor.IsChildOf(p.transform));
        if (infoPanelJustActivate || anchoredToSelf)
        {
            p.SetActive(true);
            PublishDockPose(p.transform);
            Debug.Log($"[EquipmentMarker] 정보 패널 표시(활성화만): {p.name} pos={p.transform.position} scale={p.transform.lossyScale.x:F3}");
            return;
        }

        // 부모를 떼서 월드에 고정(스틱 회전으로 Rig가 돌아도 패널은 안 따라감).
        // SetParent(null, true)는 월드 스케일을 그대로 보존한다 → 원래 보이던 크기 유지.
        SaveInfoHome(p);
        p.transform.SetParent(null, true);
        if (infoPanelScale > 0f) p.transform.localScale = Vector3.one * infoPanelScale;

        Vector3 pos; Quaternion rot;
        if (infoPanelAnchor != null)
        {
            pos = infoPanelAnchor.position;
            rot = infoPanelAnchor.rotation;
        }
        else if (viewer != null)
        {
            pos = viewer.position + viewer.rotation * infoPanelLocalOffset;
            if (infoPanelFaceViewer)
            {
                // 패널 위치에서 사용자(대략 Rig 위치) 쪽을 바라보게. 수평만.
                Vector3 toViewer = viewer.position - pos; toViewer.y = 0f;
                if (toViewer.sqrMagnitude < 0.0001f) toViewer = -viewer.forward;
                rot = Quaternion.LookRotation(toViewer.normalized)
                      * Quaternion.Euler(0f, infoPanelYaw + (infoPanelFaceFlip ? 180f : 0f), 0f);
            }
            else
            {
                rot = viewer.rotation * Quaternion.Euler(0f, infoPanelYaw + (infoPanelFaceFlip ? 180f : 0f), 0f);
            }
        }
        else
        {
            pos = p.transform.position; rot = p.transform.rotation;
            Debug.LogWarning("[EquipmentMarker] viewer(XR Rig) 없음 - 패널 위치 미조정");
        }
        p.transform.SetPositionAndRotation(pos, rot);
        p.SetActive(true);

        // 패널 루트의 피벗이 구석/원점이면 SetPosition 만으로는 눈앞이 아니라 엉뚱한 곳에 걸린다.
        // RectTransform의 실제 화면 중심을 계산해 목표 지점(pos)으로 당겨 붙인다.
        if (infoPanelRecenter && infoPanelAnchor == null)
        {
            Canvas.ForceUpdateCanvases();
            var rt = p.GetComponent<RectTransform>();
            if (rt == null) rt = p.GetComponentInChildren<RectTransform>();
            if (rt != null)
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                p.transform.position += (pos - center);
            }
        }
        PublishDockPose(p.transform);
        Debug.Log($"[EquipmentMarker] 정보 패널 표시: {p.name} 목표={pos} 실제={p.transform.position} scale={p.transform.lossyScale.x:F3} viewer={(viewer != null ? viewer.name : "null")}");
    }

    // 정보 패널의 최종 위치/회전 기준으로 "도크 패널은 이 옆에" 를 MrArrivalState에 남긴다.
    // MrCallDockPanel이 통화 수락 시 읽어서 그 자리에 월드 고정으로 띄운다.
    void PublishDockPose(Transform panel)
    {
        if (!placeDockBesideInfo) return;
        Vector3 dockPos = panel.position + panel.rotation * dockPanelOffset;
        MrArrivalState.SetDockPose(dockPos, panel.rotation);
    }

    public void ShowInfo() => ShowInfo(null);

    // 원격지원 전 켜짐 상태·자리로 되돌린다(원래 켜져 있던 패널은 켜진 채로, 설비 옆 원래 위치로).
    public void HideInfo()
    {
        var p = ResolveInfoPanel();
        if (p != null)
        {
            MrUiOnTop.Restore(p);
            if (_infoHomeSaved)
            {
                p.transform.SetParent(_infoHomeParent, false);
                p.transform.localPosition = _infoHomePos;
                p.transform.localRotation = _infoHomeRot;
                p.transform.localScale = _infoHomeScale;
                _infoHomeSaved = false;
            }
            p.SetActive(_infoWasActive ?? false);
        }
        _infoWasActive = null;
    }

    // 원격지원 화면 배치용(MrCallDockPanel) - 정보 패널을 사용자 앞 지정 위치로 옮긴다. 부모·크기는 그대로,
    // 원래 자리(로컬 위치/회전)는 처음 한 번 기억해 두고 HideInfo에서 되돌린다.
    public Transform InfoPanelTransform => ResolveInfoPanel()?.transform;

    // worldWidth > 0 이면 보이는 판의 가로 폭을 그 값(m)으로 맞춘다 - 2026-10-27: 씬의 설비 정보 패널 대부분이
    // 1:1 크기(수백 m)로 놓여 있어 왼쪽 자리로 옮기면 화면을 뒤덮었다(맨 앞에 그리기 전엔 설비에 가려 안 보였음).
    public void MoveInfoPanel(Vector3 worldCenter, Quaternion worldRot, float worldWidth = 0f)
    {
        var p = ResolveInfoPanel();
        if (p == null) return;
        SaveInfoHome(p);
        if (worldWidth > 0f && p.transform is RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float width = Vector3.Distance(corners[0], corners[3]);
            if (width > 0.0001f) p.transform.localScale *= worldWidth / width;
        }
        PlaceCentered(p.transform, worldCenter, worldRot);
        MrUiOnTop.Apply(p); // 원격지원 중에는 설비에 가려지지 않게 - HideInfo에서 되돌림
    }

    // RectTransform 피벗이 가운데가 아니어도 "보이는 판의 중심"이 worldCenter에 오게 놓는다.
    public static void PlaceCentered(Transform t, Vector3 worldCenter, Quaternion worldRot)
    {
        t.rotation = worldRot;
        t.position = worldCenter;
        if (t is RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            t.position += worldCenter - center;
        }
    }
}
