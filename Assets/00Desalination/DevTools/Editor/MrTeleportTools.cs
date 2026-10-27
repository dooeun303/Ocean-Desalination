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
    const string ScenePath = "Assets/01Scenes/MR 2.unity";

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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/00Desalination/Prefab/공정정보조회 패널.prefab");
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
}
