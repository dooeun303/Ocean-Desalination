using UnityEngine;

// 2026-10-27 Quest: 원격지원 도크(UIOnTop - 깊이 비교 없이 맨 나중에 그림)가 손에 든 컨트롤러와 레이 선까지 덮었다.
// 양손 컨트롤러 계층을 "ControllerOnTop" 레이어로 옮기면 URP 렌더러가 UIOnTop 다음에 다시 그린다
// (설정은 DevTools/Editor/MrRenderSetup.SetupControllerOnTop). 컨트롤러 모델은 실행 중에 생겨서 가끔 다시 훑는다.
public class MrControllerOnTop : MonoBehaviour
{
    static readonly string[] ControllerPaths =
    {
        "시스템/XR Interaction Setup/XR Origin (XR Rig)/Camera Offset/Left Controller",
        "시스템/XR Interaction Setup/XR Origin (XR Rig)/Camera Offset/Right Controller",
    };
    const float RescanSeconds = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        int layer = LayerMask.NameToLayer("ControllerOnTop");
        if (layer < 0 || GameObject.Find(ControllerPaths[0]) == null) return;
        var go = new GameObject("[MrControllerOnTop]");
        DontDestroyOnLoad(go);
        go.AddComponent<MrControllerOnTop>().layer = layer;
    }

    int layer;
    Transform[] controllers;
    float nextScan;

    void Start()
    {
        controllers = new Transform[ControllerPaths.Length];
        for (int i = 0; i < ControllerPaths.Length; i++)
            controllers[i] = GameObject.Find(ControllerPaths[i])?.transform;
    }

    void Update()
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + RescanSeconds;
        foreach (var root in controllers)
        {
            if (root == null) continue;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer != layer) t.gameObject.layer = layer;
        }
    }
}
