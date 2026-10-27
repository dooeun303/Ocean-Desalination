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

    static int EnsureLayer()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == LayerName) return i;

        int slot = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(PreferredLayer).stringValue) ? PreferredLayer : -1;
        for (int i = 8; slot < 0 && i < layers.arraySize; i++)
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) slot = i;
        layers.GetArrayElementAtIndex(slot).stringValue = LayerName;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[MrRenderSetup] 레이어 추가: {LayerName} = {slot}");
        return slot;
    }
}
