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
