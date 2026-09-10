using UnityEngine;

// AR쪽 InmoIconFactory.cs의 RoundedRect 부분만 MR로 옮긴 것 - MrUiTheme.Round()가 쓴다.
// 아이콘 그리기(체크리스트/다이얼 등)는 MR 패널들이 필요로 하지 않아서 안 옮겼다.
public static class MrIconFactory
{
    public static Sprite RoundedRect(int radius)
    {
        int size = radius * 2 + 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = InsideRoundedRect(x, y, size, radius) ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();

        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    static bool InsideRoundedRect(int x, int y, int size, int r)
    {
        float cx = x + 0.5f, cy = y + 0.5f;
        bool nearLeft = cx < r, nearRight = cx > size - r;
        bool nearBottom = cy < r, nearTop = cy > size - r;

        if (nearLeft && nearBottom) return CornerDist(cx, cy, r, r) <= r;
        if (nearLeft && nearTop) return CornerDist(cx, cy, r, size - r) <= r;
        if (nearRight && nearBottom) return CornerDist(cx, cy, size - r, r) <= r;
        if (nearRight && nearTop) return CornerDist(cx, cy, size - r, size - r) <= r;
        return true;
    }

    static float CornerDist(float x, float y, float cx, float cy) =>
        Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
}
