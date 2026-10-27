using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 2026-10-27: "원격지원 패널이 설비에 가려져 안 보인다" - 패널을 설비보다 항상 앞에 그리기 위한 1회성 설정.
//  1) 레이어 "UIOnTop" 추가
//  2) URP 렌더러(Mobile=Quest, PC=에디터)에 RenderObjects 기능 추가: 이 레이어를 투명 오브젝트 다음에 깊이 비교 없이(Always) 그린다
//  3) 기본 불투명/투명 패스에서는 이 레이어를 빼서 두 번 그리지 않게 한다
// 어느 패널을 이 레이어에 둘지는 런타임 MrUiOnTop이 정한다. 다시 실행해도 이미 있으면 건너뛴다.
//   Unity.exe -batchmode -quit -projectPath "<MR 프로젝트>" -executeMethod MrRenderSetup.SetupUiOnTop
public static class MrRenderSetup
{
    const string LayerName = "UIOnTop";
    const int PreferredLayer = 6;
    static readonly string[] RendererPaths = { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" };

    [MenuItem("Tools/MR/패널 항상 앞에 그리기 설정")]
    public static void SetupUiOnTop()
    {
        int layer = EnsureLayer();
        foreach (var path in RendererPaths)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (data == null) { Debug.LogError("[MrRenderSetup] 렌더러 없음: " + path); continue; }

            int bit = 1 << layer;
            data.opaqueLayerMask &= ~bit;
            data.transparentLayerMask &= ~bit;

            if (data.rendererFeatures.Any(f => f != null && f.name == LayerName))
            {
                Debug.Log("[MrRenderSetup] 이미 있음: " + path);
            }
            else
            {
                var feature = ScriptableObject.CreateInstance<RenderObjects>();
                feature.name = LayerName;
                var s = feature.settings;
                s.passTag = LayerName;
                s.Event = RenderPassEvent.AfterRenderingTransparents;
                s.filterSettings.RenderQueueType = RenderQueueType.Transparent;
                s.filterSettings.LayerMask = bit;
                s.overrideDepthState = true;
                s.depthCompareFunction = CompareFunction.Always;
                s.enableWrite = false;

                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                var so = new SerializedObject(data);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[MrRenderSetup] RenderObjects(UIOnTop) 추가: " + path);
            }
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
    }

    // 2026-10-27 Quest: 원격지원 도크를 끌 수도, 버튼을 레이로 누를 수도 없었다.
    //  - 컨트롤러 레이(Ray Interactor)의 raycastMask가 Default|UI|31이라 UIOnTop 레이어로 옮긴 도크에 안 맞았다
    //    → UI를 다루는 레이에 UIOnTop을 더한다(씬 저장).
    //  - UIOnTop은 투명 다음에 깊이 비교 없이 그려서 손에 든 컨트롤러·레이 선까지 도크 뒤로 묻혔다
    //    → 레이어 "ControllerOnTop"을 만들고 UIOnTop 다음에 다시 그린다(깊이 비교는 보통대로라 설비 같은
    //      실제 물체에는 가려지고, 깊이를 쓰지 않는 도크 위에만 보인다). 컨트롤러를 이 레이어로 옮기는 건 런타임 MrControllerOnTop.
    const string ControllerLayerName = "ControllerOnTop";
    const string ScenePath = "Assets/Desalination/Scenes/MR 2.unity";

    public static void SetupControllerOnTop()
    {
        int uiLayer = EnsureLayer(LayerName, PreferredLayer);
        int ctrlLayer = EnsureLayer(ControllerLayerName, 7);
        int bit = 1 << ctrlLayer;
        foreach (var path in RendererPaths)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (data == null) { Debug.LogError("[MrRenderSetup] 렌더러 없음: " + path); continue; }
            data.opaqueLayerMask &= ~bit;
            data.transparentLayerMask &= ~bit;
            // UIOnTop 다음 순서로 붙어야 그 위에 그려진다 - 렌더러 기능 목록 순서대로 실행된다.
            AddFeature(data, ControllerLayerName + "_Opaque", s =>
            {
                s.Event = RenderPassEvent.AfterRenderingTransparents;
                s.filterSettings.RenderQueueType = RenderQueueType.Opaque;
                s.filterSettings.LayerMask = bit;
            });
            AddFeature(data, ControllerLayerName + "_Transparent", s =>
            {
                s.Event = RenderPassEvent.AfterRenderingTransparents;
                s.filterSettings.RenderQueueType = RenderQueueType.Transparent;
                s.filterSettings.LayerMask = bit;
            });
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        foreach (var r in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!r.enableUIInteraction || (r.raycastMask.value & (1 << uiLayer)) != 0) continue;
            r.raycastMask |= 1 << uiLayer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            Debug.Log($"[MrRenderSetup] 레이 마스크에 UIOnTop 추가: {r.transform.parent?.name}/{r.name}");
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }

    static void AddFeature(UniversalRendererData data, string name, System.Action<RenderObjects.RenderObjectsSettings> configure)
    {
        if (data.rendererFeatures.Any(f => f != null && f.name == name)) { Debug.Log($"[MrRenderSetup] 이미 있음: {name}"); return; }
        var feature = ScriptableObject.CreateInstance<RenderObjects>();
        feature.name = name;
        feature.settings.passTag = name;
        configure(feature.settings);
        AssetDatabase.AddObjectToAsset(feature, data);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
        var so = new SerializedObject(data);
        var list = so.FindProperty("m_RendererFeatures");
        var map = so.FindProperty("m_RendererFeatureMap");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
        map.arraySize++;
        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[MrRenderSetup] RenderObjects({name}) 추가: {AssetDatabase.GetAssetPath(data)}");
    }

    static int EnsureLayer() => EnsureLayer(LayerName, PreferredLayer);

    static int EnsureLayer(string layerName, int preferred)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;

        int slot = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferred).stringValue) ? preferred : -1;
        for (int i = 8; slot < 0 && i < layers.arraySize; i++)
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) slot = i;
        layers.GetArrayElementAtIndex(slot).stringValue = layerName;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[MrRenderSetup] 레이어 추가: {layerName} = {slot}");
        return slot;
    }
}
