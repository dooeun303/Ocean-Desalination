using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2026-10-27: 설비별 원격지원 텔레포트 지점 점검/보정 - "설비에 너무 붙어서 패널이 가려진다".
// RayReport: 설비마다 텔레포트 지점의 눈높이에서 정면/뒤로 실제 메시까지 거리를 로그로 남긴다.
// PushBack: PushBackPlan의 지점만 바라보는 반대 방향으로 물려 씬을 저장한다.
//   Unity.exe -batchmode -quit -projectPath "<MR 프로젝트>" -executeMethod MrTeleportTools.RayReport
public static class MrTeleportTools
{
    const string ScenePath = "Assets/Desalination/Scenes/MR 2.unity";

    // 눈높이 몇 군데에서 정면/뒤로 레이를 쏴 실제 메시까지 거리를 잰다(씬 전체에 임시 MeshCollider - 저장 안 함).
    public static void RayReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var temp = new System.Collections.Generic.List<Collider>();
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            if (mf.GetComponentInParent<Canvas>(true) != null) continue;
            temp.Add(mf.gameObject.AddComponent<MeshCollider>());
        }
        Physics.SyncTransforms();
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name))
        {
            var tp = m.teleportPoint;
            if (tp == null) continue;
            Vector3 fwd = Vector3.ProjectOnPlane(EquipmentMarker.UprightRotation(tp) * Vector3.forward, Vector3.up).normalized;
            string line = $"[Ray] {m.name}:";
            foreach (float h in new[] { 0.9f, 1.3f, 1.7f })
            {
                Vector3 eye = tp.position + Vector3.up * h;
                string f = Physics.Raycast(eye, fwd, out var hf, 20f) ? $"{hf.distance:F2}({hf.collider.name})" : "-";
                string bk = Physics.Raycast(eye, -fwd, out var hb, 20f) ? $"{hb.distance:F2}({hb.collider.name})" : "-";
                line += $" | h{h}: 앞 {f} 뒤 {bk}";
            }
            Debug.Log(line);
        }
        foreach (var c in temp) Object.DestroyImmediate(c);
    }

    // 설비 마커별 정보 패널 구성(경로·위치·크기·EquipmentPanel 여부)
    public static void PanelReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name))
        {
            var ep = m.GetComponent<EquipmentPanel>();
            var canvas = m.infoPanel != null ? m.infoPanel.GetComponent<Canvas>() : m.GetComponentInChildren<Canvas>(true);
            string panel = canvas == null ? "패널 없음" :
                $"{Path(canvas.transform)} active={canvas.gameObject.activeSelf} pos={canvas.transform.position:F2} local={canvas.transform.localPosition:F3} lossy={canvas.transform.lossyScale.x:F4} rot={canvas.transform.eulerAngles:F0} prefab={PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(canvas.gameObject)}";
            Debug.Log($"[Panel] {Path(m.transform)} | EquipmentPanel={(ep != null)} | infoPanel={(m.infoPanel != null ? m.infoPanel.name : "-")} justAct={m.infoPanelJustActivate} | 마커pos={m.transform.position:F2} | {panel}");
        }
    }

    public static void PanelDetail()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var name in new[] { "ERD패널", "NF막 패널", "공정정보조회 패널 (1)" })
        {
            var t = all.FirstOrDefault(x => x.name == name);
            if (t == null) continue;
            var lines = t.GetComponentsInChildren<Transform>(true).Select(c => Path(c).Substring(Path(t).Length) + (c.GetComponent<TMPro.TextMeshProUGUI>() is var tx && tx != null ? $" \"{tx.text}\"" : ""));
            Debug.Log($"[Detail] {Path(t)}\n" + string.Join("\n", lines.Take(60)));
        }
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.name.Contains("NF") || x.name.Contains("ERD")))
            Debug.Log($"[Detail] 마커 {Path(m.transform)} tp={(m.teleportPoint ? Path(m.teleportPoint) : "-")} highlight={(m.highlightTarget ? Path(m.highlightTarget) : "-")} ring={(m.ringAnchor ? m.ringAnchor.name : "-")} comps={string.Join(",", m.GetComponents<Component>().Select(c => c.GetType().Name))}");
    }

    // 2026-10-27: "ERD와 NF 설비에도 다른 설비처럼 공정정보 패널을 붙여줘"
    //  - 2.ERD: 공정정보조회 패널 프리팹을 기존(옛 구성, 꺼져 있음) ERD패널 자리에 붙이고 EquipmentPanel 추가.
    //    EquipmentPanel은 첫 번째 Canvas를 쓰므로 새 패널을 첫 자식으로 둔다.
    //  - 6. NF: 자식 NF막과 같은 설비 id·텔레포트 지점의 중복 마커라 먼저 찾히면서 패널을 못 찾았다 → 중복 마커만 삭제.
    public static void AttachErdNfPanels()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var markers = Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var erd = markers.FirstOrDefault(m => m.name == "2.ERD");
        var oldPanel = erd != null ? erd.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "ERD패널") : null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Desalination/Prefab/공정정보조회 패널.prefab");
        if (erd == null || oldPanel == null || prefab == null) { Debug.LogError("[Attach] ERD 마커/ERD패널/프리팹 없음"); return; }
        if (erd.GetComponent<EquipmentPanel>() == null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, erd.transform);
            inst.transform.SetPositionAndRotation(oldPanel.position, oldPanel.rotation);
            inst.transform.localScale = Vector3.one * (oldPanel.lossyScale.x / erd.transform.lossyScale.x);
            inst.transform.SetSiblingIndex(0);
            inst.SetActive(false);
            erd.gameObject.AddComponent<EquipmentPanel>();
            Debug.Log($"[Attach] 2.ERD: 패널 추가 pos={inst.transform.position:F2} lossy={inst.transform.lossyScale.x:F4} + EquipmentPanel");
        }

        var nfDup = markers.FirstOrDefault(m => m.name == "6. NF");
        var nf = markers.FirstOrDefault(m => m.name == "NF막");
        if (nfDup != null && nf != null && nfDup.equipmentId == nf.equipmentId)
        {
            Object.DestroyImmediate(nfDup);
            Debug.Log("[Attach] 6. NF: 중복 EquipmentMarker 삭제(NF막 마커 사용)");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Attach] 씬 저장");
    }

    // 설비 선택(클릭) 구성 - 콜라이더와 XRSimpleInteractable 이벤트 연결
    public static void InteractReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name))
        {
            var it = m.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();
            var col = m.GetComponent<Collider>();
            string ev = "-";
            if (it != null)
            {
                var so = new SerializedObject(it);
                ev = string.Join(" ; ", new[] { "m_FirstSelectEntered" }.Select(p =>
                {
                    var calls = so.FindProperty(p + ".m_PersistentCalls.m_Calls");
                    if (calls == null) return p + "?";
                    return p + "=[" + string.Join(",", Enumerable.Range(0, calls.arraySize).Select(i =>
                    {
                        var c = calls.GetArrayElementAtIndex(i);
                        var target = c.FindPropertyRelative("m_Target").objectReferenceValue;
                        string tn = target is Component tc ? Path(tc.transform) + ":" + target.GetType().Name : target is GameObject tg ? Path(tg.transform) : "null";
                        return $"{tn}.{c.FindPropertyRelative("m_MethodName").stringValue}({c.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue})";
                    })) + "]";
                }));
                ev += $" colliders={it.colliders.Count}";
            }
            Debug.Log($"[Interact] {Path(m.transform)} | collider={(col != null ? col.GetType().Name + " " + col.bounds.size.ToString("F2") : "-")} | interactable={(it != null)} | {ev}");
        }
    }

    // 2026-10-27: "erd도 해줘" - 다른 설비처럼 ERD를 컨트롤러로 선택하면 공정정보 패널이 뜨게.
    // 다른 설비와 같은 구성: 설비 크기의 BoxCollider + XRSimpleInteractable, First Select Entered에
    // EquipmentPanel.OnSelected → 패널 SetActive(true) (→ 안내 문구가 있으면 SetActive(false)).
    public static void AttachErdInteract()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var erd = Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m.name == "2.ERD");
        var ep = erd != null ? erd.GetComponent<EquipmentPanel>() : null;
        if (erd == null || ep == null) { Debug.LogError("[Attach] 2.ERD 또는 EquipmentPanel 없음"); return; }
        if (erd.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>() != null) { Debug.Log("[Attach] 이미 있음"); return; }

        var rs = erd.GetComponentsInChildren<Renderer>(true).Where(r => r.GetComponentInParent<Canvas>(true) == null).ToArray();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        var box = erd.gameObject.AddComponent<BoxCollider>();
        box.center = erd.transform.InverseTransformPoint(b.center);
        Vector3 s = erd.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        var it = erd.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();
        var panel = erd.GetComponentInChildren<Canvas>(true).gameObject; // AttachErdNfPanels가 첫 자식으로 둔 공정정보조회 패널
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(it.firstSelectEntered, ep.OnSelected);
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(it.firstSelectEntered, panel.SetActive, true);
        var guide = erd.GetComponentsInChildren<Transform>(true).Concat(erd.transform.parent.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Contains("ERD") && t.name.Contains("가이드"));
        if (guide != null) UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(it.firstSelectEntered, guide.gameObject.SetActive, false);

        Debug.Log($"[Attach] 2.ERD: BoxCollider size={box.size:F2}(월드 {b.size:F2}) + XRSimpleInteractable → OnSelected, {panel.name}.SetActive(true){(guide != null ? ", " + guide.name + ".SetActive(false)" : "")}");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

    // RayReport 결과(2026-10-27): 눈높이 1.3m에서 정면 설비까지 2.6m(도크 2.2m + 여유)가 안 되던 지점만,
    // 바라보는 반대 방향으로 이만큼 물린다. 뒤쪽 벽까지는 모두 3m 이상 남는다.
    static readonly (string marker, float back)[] PushBackPlan =
    {
        ("2.ERD", 0.7f), ("RO막", 1.0f), ("WaterTank", 1.4f), ("pump2", 1.5f),
    };

    public static void PushBack()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var markers = Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var (name, back) in PushBackPlan)
        {
            var m = markers.FirstOrDefault(x => x.name == name);
            if (m == null || m.teleportPoint == null) { Debug.LogWarning($"[Teleport] {name}: 마커/지점 없음"); continue; }
            var tp = m.teleportPoint;
            Vector3 fwd = Vector3.ProjectOnPlane(EquipmentMarker.UprightRotation(tp) * Vector3.forward, Vector3.up).normalized;
            Vector3 before = tp.position;
            tp.position = before - fwd * back;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tp);
            Debug.Log($"[Teleport] {name}: {before:F2} → {tp.position:F2} ({back}m 뒤로)");
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Teleport] 씬 저장");
    }

    // 2026-10-27 Quest: 알람 "설비로 이동"으로 OARO에 가면 너무 가깝고 조이스틱 이동이 안 됐다.
    // 원인: 지점마다 표시용 기둥(…공정공간이동 포인트/Cylinder)에 막힌 CapsuleCollider가 있어 플레이어가 그 속에
    // 내려앉았고, 몇 지점은 설비 콜라이더(BoxCollider)와도 겹쳤다.
    // FitReport: 지점마다(표시 기둥 제외) 몸 캡슐이 겹치는 것, 설비 바운즈 가장자리까지 거리.
    // FitApply: 표시 기둥 콜라이더를 끄고(기둥 모양은 그대로), 겹치거나 설비에 FitClearance보다 가까운 지점을
    //           바라보는 반대 방향으로 물린다. 씬 저장.
    const float BodyRadius = 0.3f;
    const float FitClearance = 2.0f;   // 설비 바운즈 가장자리에서 몸 중심까지 최소 거리(다른 지점들이 2.6m 안팎)
    const float HugeEquipment = 6f;    // 바운즈가 이보다 크면(여러 설비를 묶은 마커) 거리 기준은 안 쓴다

    // 텔레포트 지점 아래 오브젝트들의 컴포넌트(콜라이더 트리거 여부, 렌더러 켜짐)
    public static void PointReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name))
        {
            if (m.teleportPoint == null) continue;
            foreach (var t in m.teleportPoint.GetComponentsInChildren<Transform>(true))
                Debug.Log($"[Point] {m.name}: {Path(t)} active={t.gameObject.activeInHierarchy} layer={LayerMask.LayerToName(t.gameObject.layer)} | " +
                    string.Join(", ", t.GetComponents<Component>().Select(c => c.GetType().Name +
                        (c is Collider col ? $"(trigger={col.isTrigger},on={col.enabled})" : c is Renderer r ? $"(on={r.enabled})" : ""))));
        }
    }

    // 마커별 하이라이트 대상·색·인터랙터블, XR 리그의 이동 관련 컴포넌트
    public static void RigReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name))
        {
            var it = m.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable>();
            var root = m.highlightTarget != null ? m.highlightTarget : m.transform;
            Debug.Log($"[Rig] marker {Path(m.transform)} | 하이라이트 대상 {Path(root)} (렌더러 {root.GetComponentsInChildren<Renderer>(true).Length}) rim={m.rimColor} | interactable={(it != null ? it.GetType().Name + " colliders=" + it.colliders.Count : "없음")} | Outline 미리 붙음={root.GetComponent<Outline>() != null}");
        }
        foreach (var o in Object.FindObjectsByType<Outline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Debug.Log($"[Rig] Outline {Path(o.transform)} on={o.enabled} color={o.OutlineColor}");
        var rig = GameObject.Find("XR Origin (XR Rig)");
        if (rig == null) { Debug.Log("[Rig] XR Origin 없음"); return; }
        foreach (var c in rig.GetComponentsInChildren<Component>(true))
        {
            string n = c.GetType().Name;
            if (n.Contains("Move") || n.Contains("Locomotion") || n.Contains("Teleport") || n.Contains("Turn") || n.Contains("CharacterController") || n.Contains("Climb") || n.Contains("Grab") && n.Contains("Move") || n.Contains("Body") || n.Contains("Gravity"))
                Debug.Log($"[Rig] {Path(c.transform)} {n} on={(c is Behaviour b ? b.enabled : c is Collider col ? col.enabled : true)} active={c.gameObject.activeInHierarchy}" + (c is CharacterController cc ? $" r={cc.radius} h={cc.height} center={cc.center}" : ""));
        }
        Debug.Log($"[Rig] Physics.autoSyncTransforms={Physics.autoSyncTransforms}");
    }

    // 마커별 선택 콜라이더(XRSimpleInteractable이 가져가는 자식 콜라이더)의 월드 바운즈
    public static void ColliderReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var m in Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name))
            foreach (var c in m.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && c.GetComponentInParent<Canvas>(true) == null && !UnderPoint(m, c.transform)))
                Debug.Log($"[Col] {m.name}: {Path(c.transform)} {c.GetType().Name} trigger={c.isTrigger} 중심 {c.bounds.center:F1} 크기 {c.bounds.size:F1}");
    }

    // 2026-10-27: pump1은 하이라이트 대상이 펌프 묶음(1.PUMP) 전체에 색도 주황빨강이라, 알람으로 pump1에 가면
    // pump2까지 붉게 감쌌다. 자기 렌더러가 있는 마커는 자기 자신을 대상으로, 색은 기본 하늘색으로 맞춘다.
    public static void FixHighlight()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var markers = Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var defaultRim = new Color(0.35f, 0.8f, 1f, 1f);
        foreach (var m in markers)
        {
            bool changed = false;
            var t = m.highlightTarget;
            if (t != null && t != m.transform && markers.Any(o => o != m && o.transform.IsChildOf(t)) && m.GetComponentsInChildren<Renderer>(true).Length > 0)
            {
                Debug.Log($"[Highlight] {m.name}: 대상 {Path(t)} → 자기 자신 (다른 설비까지 감싸던 묶음)");
                m.highlightTarget = null;
                changed = true;
            }
            if (m.rimColor != defaultRim)
            {
                Debug.Log($"[Highlight] {m.name}: 색 {m.rimColor} → 기본 하늘색");
                m.rimColor = defaultRim;
                changed = true;
            }
            if (changed) PrefabUtility.RecordPrefabInstancePropertyModifications(m);

            // 선택용 BoxCollider가 설비 메시보다 훨씬 크면(2.ERD: 정보 패널까지 감싸 앞쪽 3.6m를 덮음) 메시에 맞춘다.
            var box = m.GetComponent<BoxCollider>();
            if (box != null && TryEquipmentBounds(m, out var rb))
            {
                float Vol(Vector3 s) => s.x * s.y * s.z;
                if (Vol(box.bounds.size) > Vol(rb.size) * 1.5f)
                {
                    var before = box.bounds;
                    var tr = box.transform;
                    box.center = tr.InverseTransformPoint(rb.center);
                    Vector3 ls = tr.InverseTransformVector(rb.size);
                    box.size = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                    Debug.Log($"[Highlight] {m.name}: 선택 콜라이더 {before.center:F1}/{before.size:F1} → {box.bounds.center:F1}/{box.bounds.size:F1}");
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // FitApply가 2.ERD 지점을 물린 건 너무 큰 선택 콜라이더(FixHighlight로 줄임) 때문이었다 - 원래 자리로.
    public static void RestoreErdPoint()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var m = Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == "2.ERD");
        var before = m.teleportPoint.position;
        m.teleportPoint.position = new Vector3(before.x, before.y, 5.68f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(m.teleportPoint);
        var temp = AddTempMeshColliders();
        Debug.Log($"[Fit] 2.ERD: {before:F2} → {m.teleportPoint.position:F2} | {Describe(m, m.teleportPoint.position, TryEquipmentBounds(m, out var b) ? b : default, true)}");
        foreach (var c in temp) Object.DestroyImmediate(c);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // "메뉴"라는 이름의 오브젝트들: 경로·켜짐·컴포넌트·월드 위치(리그 기준 로컬)
    public static void MenuReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.Contains("메뉴") || t.name.ToLower().Contains("menu")))
            Debug.Log($"[Menu] {Path(t)} activeSelf={t.gameObject.activeSelf} inHierarchy={t.gameObject.activeInHierarchy} local={t.localPosition:F2} rot={t.localEulerAngles:F0} scale={t.lossyScale:F3} | " +
                string.Join(", ", t.GetComponents<Component>().Select(c => c.GetType().Name)));
    }

    // 레이 인터랙터의 레이캐스트 마스크(물리·UI)와 메뉴 캔버스 레이캐스터 설정
    public static void RayMaskReport()
    {
        EditorSceneManager.OpenScene(ScenePath);
        string Mask(int m) => m == ~0 ? "Everything" : string.Join("|", Enumerable.Range(0, 32).Where(i => (m & (1 << i)) != 0).Select(i => $"{i}:{LayerMask.LayerToName(i)}"));
        foreach (var r in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Debug.Log($"[RayMask] {Path(r.transform)} {r.GetType().Name} raycastMask={Mask(r.raycastMask.value)} enableUI={r.enableUIInteraction} triggerInteraction={r.raycastTriggerInteraction}");
        foreach (var g in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Debug.Log($"[RayMask] raycaster {Path(g.transform)} layer={LayerMask.LayerToName(g.gameObject.layer)} 3D가림검사={g.checkFor3DOcclusion} blockingMask={Mask(g.blockingMask.value)}");
    }

    public static void FitReport() => Fit(false);
    public static void FitApply() => Fit(true);

    static void Fit(bool apply)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var markers = Object.FindObjectsByType<EquipmentMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(x => x.name).ToArray();
        if (apply)
            foreach (var m in markers.Where(x => x.teleportPoint != null))
                foreach (var c in m.teleportPoint.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && !c.isTrigger))
                {
                    c.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                    Debug.Log($"[Fit] 표시 기둥 콜라이더 끔: {Path(c.transform)}");
                }

        var temp = AddTempMeshColliders();
        foreach (var m in markers)
        {
            var tp = m.teleportPoint;
            if (tp == null) continue;
            bool hasBounds = TryEquipmentBounds(m, out var b);
            bool useEdge = hasBounds && Mathf.Max(b.size.x, b.size.z) < HugeEquipment;
            Debug.Log(Describe(m, tp.position, b, hasBounds));
            if (!apply) continue;

            Vector3 back = -Vector3.ProjectOnPlane(EquipmentMarker.UprightRotation(tp) * Vector3.forward, Vector3.up).normalized;
            bool Bad(Vector3 q) => Overlaps(m, q, BodyRadius + 0.2f).Length > 0 || (useEdge && DistanceFlat(b, q) < FitClearance);
            Vector3 p = tp.position;
            if (!Bad(p)) continue;
            for (int i = 0; i < 40 && Bad(p); i++) p += back * 0.1f;
            Vector3 before = tp.position;
            tp.position = p;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tp);
            Debug.Log($"[Fit] {m.name}: {before:F2} → {p:F2} ({Vector3.Distance(before, p):F2}m 뒤로){(Bad(p) ? " ※4m 물려도 여유 부족" : "")} | 이후 {Describe(m, p, b, hasBounds)}");
        }
        foreach (var c in temp) Object.DestroyImmediate(c);
        if (apply) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Debug.Log("[Fit] 씬 저장"); }
    }

    static string Describe(EquipmentMarker m, Vector3 p, Bounds b, bool hasBounds)
    {
        var hits = Overlaps(m, p, BodyRadius);
        string edge = hasBounds ? $"설비 가장자리까지 {DistanceFlat(b, p):F2}m (설비 크기 {b.size.x:F1}x{b.size.z:F1})" : "설비 렌더러 없음";
        return $"[Fit] {m.name}: 지점 {p:F2} · {edge} · 몸 겹침 {(hits.Length == 0 ? "없음" : string.Join(",", hits.Select(h => Path(h.transform) + (h is MeshCollider ? "" : "[" + h.GetType().Name + "]")).Distinct().Take(5)))}";
    }

    // 설비(하이라이트 대상) 렌더러를 합친 바운즈. 정보 패널(Canvas)과 텔레포트 지점(표시 기둥)은 뺀다.
    static bool TryEquipmentBounds(EquipmentMarker m, out Bounds b)
    {
        b = default;
        var root = m.highlightTarget != null ? m.highlightTarget : m.transform;
        var rends = root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.GetComponentInParent<Canvas>(true) == null && !(r is ParticleSystemRenderer) && !UnderPoint(m, r.transform)).ToArray();
        if (rends.Length == 0) return false;
        b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return true;
    }

    static bool UnderPoint(EquipmentMarker m, Transform t) => m.teleportPoint != null && t.IsChildOf(m.teleportPoint);

    // 바운즈 XZ 사각형까지의 거리(안이면 0)
    static float DistanceFlat(Bounds b, Vector3 p)
    {
        float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x);
        float dz = Mathf.Max(b.min.z - p.z, 0f, p.z - b.max.z);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // 무릎~머리 높이 캡슐(바닥은 제외)과 겹치는 콜라이더. 정보 패널과 표시 기둥은 뺀다.
    static Collider[] Overlaps(EquipmentMarker m, Vector3 p, float radius) =>
        Physics.OverlapCapsule(p + Vector3.up * (0.4f + radius), p + Vector3.up * (1.8f - radius), radius, ~0, QueryTriggerInteraction.Ignore)
            .Where(c => c.enabled && c.GetComponentInParent<Canvas>(true) == null && !UnderPoint(m, c.transform)).ToArray();


    static System.Collections.Generic.List<Collider> AddTempMeshColliders()
    {
        var temp = new System.Collections.Generic.List<Collider>();
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            if (mf.GetComponentInParent<Canvas>(true) != null) continue;
            temp.Add(mf.gameObject.AddComponent<MeshCollider>());
        }
        Physics.SyncTransforms();
        return temp;
    }
}
