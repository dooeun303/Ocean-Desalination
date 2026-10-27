using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2026-10-27 프로젝트 정리 ②: MR 2 씬 Hierarchy 정리.
//  - 스크립트가 지워져 아무 일도 안 하는 컴포넌트(Missing Script) 제거 - ButtonHandler/PanelHandler/CloseButtonHandler/Menu(5f237a5에서 삭제)
//  - 아무도 참조하지 않는 빈 오브젝트(공정공간/walls (1)) 삭제. 루트 ring은 pump1의 ringAnchor라 pump1 아래로(월드 위치 유지)
//  - 루트의 이름 없는 GameObject(MrCallDockPanel)를 "MrCallDockPanel"로 바꿔 시스템 아래로
// Report는 바꾸지 않고 무엇을 할지만, Apply는 실제로 바꾸고 저장한다.
//   Unity.exe -batchmode -quit -projectPath "<MR 프로젝트>" -executeMethod MrSceneCleanup.Report
public static class MrSceneCleanup
{
    const string ScenePath = "Assets/Desalination/Scenes/MR 2.unity";

    public static void Report() => Run(false);
    public static void Apply() => Run(true);

    static void Run(bool apply)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();

        int missing = 0;
        foreach (var go in all)
        {
            int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (n == 0) continue;
            missing += n;
            Debug.Log($"[Cleanup] Missing Script {n}개: {Path(go.transform)}");
            if (apply) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        var roots = scene.GetRootGameObjects();
        var empties = new List<GameObject>();
        var ring = roots.FirstOrDefault(g => g.name == "ring" && g.transform.childCount == 0 && g.GetComponents<Component>().Length == 1);
        if (ring != null) empties.Add(ring);
        var walls1 = all.FirstOrDefault(g => g.name == "walls (1)" && g.transform.parent != null && g.transform.parent.name == "공정공간"
                                             && g.transform.childCount == 0 && g.GetComponents<Component>().Length == 1);
        if (walls1 != null) empties.Add(walls1);
        foreach (var e in empties)
        {
            var refs = ReferencesTo(all, e);
            Debug.Log($"[Cleanup] 빈 오브젝트 {Path(e.transform)} 참조 {refs.Count}곳 {string.Join(", ", refs)}");
            if (!apply) continue;
            if (refs.Count == 0) { Object.DestroyImmediate(e); continue; }
            // 설비 마커의 ringAnchor로 쓰이는 루트 오브젝트 → 그 설비 아래로 옮기고 이름을 붙인다
            var owner = all.Select(g => g.GetComponent<EquipmentMarker>()).FirstOrDefault(m => m != null && m.ringAnchor == e.transform);
            if (owner != null)
            {
                e.transform.SetParent(owner.transform, true);
                e.name = "RingAnchor";
                Debug.Log($"[Cleanup] → {Path(e.transform)}");
            }
        }

        var dock = roots.FirstOrDefault(g => g.name == "GameObject" && g.GetComponent<MrCallDockPanel>() != null);
        var system = roots.FirstOrDefault(g => g.name == "시스템");
        if (dock != null)
        {
            Debug.Log($"[Cleanup] {Path(dock.transform)} → 시스템/MrCallDockPanel");
            if (apply)
            {
                dock.name = "MrCallDockPanel";
                if (system != null) dock.transform.SetParent(system.transform, true);
            }
        }

        Debug.Log($"[Cleanup] Missing Script 합계 {missing}개");
        if (apply)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Cleanup] 씬 저장");
        }
    }

    // 씬의 다른 컴포넌트가 target(또는 그 컴포넌트)을 직렬화 필드로 가리키는 곳
    static List<string> ReferencesTo(List<GameObject> all, GameObject target)
    {
        var targets = new HashSet<Object>(target.GetComponents<Component>()) { target };
        var hits = new List<string>();
        foreach (var go in all)
        {
            if (go == target) continue;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var it = new SerializedObject(c).GetIterator();
                while (it.Next(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null && targets.Contains(it.objectReferenceValue)
                        && !it.propertyPath.StartsWith("m_Children") && it.propertyPath != "m_Father") // 부모의 자식 목록은 참조로 치지 않음
                        hits.Add($"{Path(go.transform)}:{c.GetType().Name}.{it.propertyPath}");
            }
        }
        return hits;
    }

    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
