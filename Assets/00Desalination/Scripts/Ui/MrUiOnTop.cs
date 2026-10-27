using System.Collections.Generic;
using UnityEngine;

// 2026-10-27: "설비랑 패널이 겹치니까 패널이 안 보인다" - 패널의 Canvas를 UIOnTop 레이어로 옮기면 URP 렌더러의
// RenderObjects(UIOnTop) 기능이 투명 오브젝트 다음에 깊이 비교 없이 그려서 설비에 가려지지 않는다
// (설정은 DevTools/Editor/MrRenderSetup). UI는 Canvas의 레이어로 그려지므로 Canvas만 바꾸면 된다.
// 씬에 놓인 패널(설비 정보 패널 등)은 원격지원 중에만 올리고 Restore로 원래 레이어로 되돌린다 - 평소에도
// 맨 앞이면 벽 너머의 패널까지 비쳐 보인다.
public static class MrUiOnTop
{
    static int _layer = -2;
    static int Layer => _layer != -2 ? _layer : (_layer = LayerMask.NameToLayer("UIOnTop"));

    static readonly Dictionary<Canvas, int> Saved = new Dictionary<Canvas, int>();

    public static void Apply(GameObject root)
    {
        if (root == null || Layer < 0) return;
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
        {
            if (!Saved.ContainsKey(c)) Saved[c] = c.gameObject.layer;
            c.gameObject.layer = Layer;
        }
    }

    public static void Restore(GameObject root)
    {
        if (root == null) return;
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
        {
            if (!Saved.TryGetValue(c, out int original)) continue;
            c.gameObject.layer = original;
            Saved.Remove(c);
        }
    }
}
