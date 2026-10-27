using UnityEditor;
using UnityEngine;
using GLTFast.Export;

// "건전지 교체" 데모 시나리오용 - 펌프 대신 실제 컨트롤러 모델을 쓰고 싶다는 요청으로 추가.
// 씬에 컨트롤러가 배치돼있지 않아서(XR Interaction Toolkit 샘플 프리팹만 있음, MR.unity에는 없음)
// PumpGltfExporter.cs처럼 Hierarchy에서 직접 선택하게 하는 대신, 프리팹을 코드로 바로 불러와
// 임시로 인스턴스화한 뒤 내보내고 지운다 - 씬을 건드릴 필요가 없다.
static class ControllerGltfExporter
{
    // Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab
    // - Controller_Base/Bumper/Button_A/Button_B/Button_Home/ThumbStick/ThumbStick_Base/TouchPad/Trigger
    // 9개 파츠로 이뤄진 실제 컨트롤러 메시(빈 트래킹 앵커가 아님).
    const string PrefabPath = "Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab";

    [MenuItem("Tools/Export Controller To GLB")]
    static async void ExportController()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[ControllerGltfExporter] 프리팹을 못 찾음: " + PrefabPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        try
        {
            Debug.Log("[ControllerGltfExporter] 내보낼 오브젝트: " + instance.name);

            var exportSettings = new ExportSettings { Format = GltfFormat.Binary };
            var export = new GameObjectExport(exportSettings);
            export.AddScene(new[] { instance }, "Controller");

            string path = System.IO.Path.Combine(Application.dataPath, "..", "controller_export.glb");
            bool success = await export.SaveToFileAndDispose(path);

            if (success) Debug.Log($"[ControllerGltfExporter] 익스포트 완료: {path}");
            else Debug.LogError("[ControllerGltfExporter] 익스포트 실패");
        }
        finally
        {
            Object.DestroyImmediate(instance); // 씬에 흔적을 안 남기고 임시 인스턴스만 정리
        }
    }
}
