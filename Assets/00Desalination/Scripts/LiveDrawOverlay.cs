using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

// 화상통화 중 라이브 스트리밍 화면 위에 실시간으로 그려서 AR로 스트리밍하는 오버레이.
// 기존 "지시하기"(InstructionDrawing.cs, 캡처 후 정지화면에 그려 최종본만 전송)와는 별개 기능 -
// 이건 그리는 즉시 점 단위로 VideoCallSignalingMR을 통해 AR(InmoLiveDrawOverlay.cs)로 스트리밍되어
// 실시간으로 나타난다. 화상통화가 수락된 동안에만 뜨고, "실시간 그리기" 토글 버튼을 눌러야 드로잉
// 서페이스가 입력을 받기 시작한다(평소엔 raycastTarget을 꺼둬서 다른 통화 UI 조작을 가리지 않음).
// 씬의 실제 화상통화 패널 좌표를 몰라서 WorkGuidePanel.cs와 같은 방식으로 카메라 앞 고정 오프셋
// (HUD)에 매달아 둔다 - 씬/프리팹 편집 불필요, [RuntimeInitializeOnLoadMethod]로 자체 부트스트랩.
// AR쪽처럼 그린 지 몇 초 지난 점은 자동으로 옅어지다 사라진다(레이저 포인터 방식).
public class LiveDrawOverlay : MonoBehaviour
{
    const float FadeDuration = 4f;
    const int TexWidth = 480;
    const int TexHeight = 270;
    const float PenWidthNorm = 0.01f;

    static readonly Color[] Palette =
    {
        new Color(0.95f, 0.25f, 0.2f), // 빨강
        new Color(0.2f, 0.75f, 0.95f), // 하늘
        new Color(1f, 0.85f, 0.2f),    // 노랑
    };

    class Stroke
    {
        public Color32 color;
        public readonly List<Vector3> points = new List<Vector3>(); // x, y(0..1), 생성시각(Time.time)
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("LiveDrawOverlay");
        go.AddComponent<LiveDrawOverlay>();
        DontDestroyOnLoad(go);
    }

    GameObject canvasGo;
    GameObject panel;
    RectTransform drawSurfaceRect;
    RawImage drawSurfaceImage;
    Image toggleBtnImage;
    Text toggleBtnLabel;

    Texture2D _tex;
    Color32[] _pixelBuf;
    readonly Dictionary<string, Stroke> _strokes = new Dictionary<string, Stroke>();

    bool _drawModeOn;
    string _channelName;
    string _currentStrokeId;
    int _paletteIndex;

    void Awake()
    {
        BuildUI();
        panel.SetActive(false);
    }

    void OnEnable()
    {
        VideoCallSignalingMR.OnCallAccepted += HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded += HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected += HandleCallRejected;
    }

    void OnDisable()
    {
        VideoCallSignalingMR.OnCallAccepted -= HandleCallAccepted;
        VideoCallSignalingMR.OnCallEnded -= HandleCallEnded;
        VideoCallSignalingMR.OnCallRejected -= HandleCallRejected;
    }

    void HandleCallAccepted(string channel)
    {
        _channelName = channel;
        panel.SetActive(true);
        SetDrawMode(false);
        _strokes.Clear();
    }

    void HandleCallEnded(string channel)
    {
        if (channel != _channelName) return;
        panel.SetActive(false);
        SetDrawMode(false);
        _strokes.Clear();
    }

    void HandleCallRejected(string channel)
    {
        panel.SetActive(false);
    }

    void SetDrawMode(bool on)
    {
        _drawModeOn = on;
        drawSurfaceImage.raycastTarget = on;
        toggleBtnLabel.text = on ? "그리기 끄기" : "실시간 그리기";
        toggleBtnImage.color = on ? new Color(0.85f, 0.3f, 0.25f) : new Color(0.25f, 0.45f, 0.85f);
    }

    void OnToggleClicked() => SetDrawMode(!_drawModeOn);

    // ───────── 입력 (DrawInputForwarder에서 전달) ─────────
    public void OnSurfacePointerDown(PointerEventData e)
    {
        if (!_drawModeOn) return;
        if (_currentStrokeId != null) return; // 이미 진행 중인 스트로크가 있으면 무시 - 마우스/XR 컨트롤러 입력 모듈이
                                               // 동시에 떠 있을 때 한 번의 클릭에 Down이 중복 발생하면 드래그 도중
                                               // strokeId가 새로 덮어써져 이전 선이 끊기는 문제 방지
        _currentStrokeId = Guid.NewGuid().ToString("N");
        var color = Palette[_paletteIndex % Palette.Length];
        _paletteIndex++;
        _strokes[_currentStrokeId] = new Stroke { color = color };
        var n = NormalizedPos(e);
        AddPoint(n);
        VideoCallSignalingMR.Instance?.SendDrawStart(_currentStrokeId, ColorToHex(color), PenWidthNorm);
        VideoCallSignalingMR.Instance?.SendDrawPoint(_currentStrokeId, n.x, n.y);
    }

    public void OnSurfaceDrag(PointerEventData e)
    {
        if (!_drawModeOn || _currentStrokeId == null) return;
        var n = NormalizedPos(e);
        AddPoint(n);
        VideoCallSignalingMR.Instance?.SendDrawPoint(_currentStrokeId, n.x, n.y);
    }

    public void OnSurfacePointerUp(PointerEventData e)
    {
        if (_currentStrokeId == null) return;
        VideoCallSignalingMR.Instance?.SendDrawEnd(_currentStrokeId);
        _currentStrokeId = null;
    }

    void AddPoint(Vector2 n)
    {
        if (_currentStrokeId == null || !_strokes.TryGetValue(_currentStrokeId, out var s)) return;
        s.points.Add(new Vector3(n.x, n.y, Time.time));
    }

    Vector2 NormalizedPos(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawSurfaceRect, e.position, e.pressEventCamera, out var local);
        var rect = drawSurfaceRect.rect;
        float x = (local.x - rect.x) / rect.width;
        float y = (local.y - rect.y) / rect.height;
        return new Vector2(x, y);
    }

    static string ColorToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // ───────── 로컬 미리보기 렌더 (AR쪽 InmoLiveDrawOverlay.cs와 같은 방식 - Bresenham + 프레임마다 재래스터) ─────────
    void LateUpdate()
    {
        FollowCamera();
        RenderStrokes();
    }

    void FollowCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (canvasGo.transform.parent != cam.transform)
        {
            canvasGo.transform.SetParent(cam.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.05f, 1.6f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.0015f;
        }
    }

    void RenderStrokes()
    {
        if (_strokes.Count == 0) return; // 그릴 게 없으면 텍스처는 이미 투명 상태 그대로

        float now = Time.time;
        Array.Clear(_pixelBuf, 0, _pixelBuf.Length);

        List<string> toRemove = null;
        foreach (var kv in _strokes)
        {
            var s = kv.Value;
            s.points.RemoveAll(p => now - p.z > FadeDuration);
            if (s.points.Count == 0)
            {
                if (kv.Key != _currentStrokeId)
                {
                    toRemove ??= new List<string>();
                    toRemove.Add(kv.Key);
                }
                continue;
            }

            int width = Mathf.Max(1, Mathf.RoundToInt(PenWidthNorm * TexWidth));
            for (int i = 1; i < s.points.Count; i++)
            {
                var a = s.points[i - 1];
                var b = s.points[i];
                float alpha = 1f - (now - b.z) / FadeDuration;
                if (alpha <= 0f) continue;
                var c = s.color;
                c.a = (byte)(alpha * 255);
                DrawLine(new Vector2(a.x, a.y), new Vector2(b.x, b.y), c, width);
            }
        }

        if (toRemove != null)
            foreach (var id in toRemove) _strokes.Remove(id);

        _tex.SetPixels32(_pixelBuf);
        _tex.Apply();
    }

    void DrawLine(Vector2 fromNorm, Vector2 toNorm, Color32 color, int width)
    {
        int x0 = Mathf.RoundToInt(fromNorm.x * TexWidth);
        int y0 = Mathf.RoundToInt(fromNorm.y * TexHeight);
        int x1 = Mathf.RoundToInt(toNorm.x * TexWidth);
        int y1 = Mathf.RoundToInt(toNorm.y * TexHeight);

        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int half = width / 2;

        while (true)
        {
            for (int px = x0 - half; px <= x0 + half; px++)
                for (int py = y0 - half; py <= y0 + half; py++)
                    if (px >= 0 && px < TexWidth && py >= 0 && py < TexHeight)
                        _pixelBuf[py * TexWidth + px] = color;

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    void BuildUI()
    {
        var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 36);

        canvasGo = new GameObject("LiveDrawCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 560);
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;

        _tex = new Texture2D(TexWidth, TexHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        _pixelBuf = new Color32[TexWidth * TexHeight];
        _tex.SetPixels32(_pixelBuf);
        _tex.Apply();

        var surfaceGo = new GameObject("DrawSurface");
        surfaceGo.transform.SetParent(panel.transform, false);
        drawSurfaceImage = surfaceGo.AddComponent<RawImage>();
        drawSurfaceImage.texture = _tex;
        drawSurfaceImage.raycastTarget = false;
        drawSurfaceRect = drawSurfaceImage.rectTransform;
        drawSurfaceRect.anchorMin = new Vector2(0f, 0f);
        drawSurfaceRect.anchorMax = new Vector2(1f, 0.88f); // 위쪽에 토글 버튼 자리 남김
        drawSurfaceRect.offsetMin = Vector2.zero; drawSurfaceRect.offsetMax = Vector2.zero;
        surfaceGo.AddComponent<DrawInputForwarder>().owner = this;

        var bgGo = new GameObject("Bg"); // 서페이스 위치 파악용 옅은 배경
        bgGo.transform.SetParent(surfaceGo.transform, false);
        bgGo.transform.SetAsFirstSibling();
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.15f);
        bg.raycastTarget = false;
        var bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        var btnGo = new GameObject("ToggleBtn");
        btnGo.transform.SetParent(panel.transform, false);
        toggleBtnImage = btnGo.AddComponent<Image>();
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = toggleBtnImage;
        btn.onClick.AddListener(OnToggleClicked);
        var btnRect = toggleBtnImage.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 1f); btnRect.anchorMax = new Vector2(0.5f, 1f);
        btnRect.sizeDelta = new Vector2(280, 70); btnRect.anchoredPosition = new Vector2(0, -40);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        toggleBtnLabel = labelGo.AddComponent<Text>();
        toggleBtnLabel.font = font;
        toggleBtnLabel.fontSize = 26;
        toggleBtnLabel.alignment = TextAnchor.MiddleCenter;
        toggleBtnLabel.color = Color.white;
        toggleBtnLabel.raycastTarget = false;
        var labelRect = toggleBtnLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;

        SetDrawMode(false);
    }
}

// DrawSurface 오브젝트에 붙어 포인터 입력을 LiveDrawOverlay로 그대로 전달하는 브릿지.
// (LiveDrawOverlay 컴포넌트 자신은 DontDestroyOnLoad 루트에 있고, 실제 레이캐스트를 받는 DrawSurface는
// 카메라 자식으로 옮겨붙는 별도 오브젝트라 포인터 인터페이스를 분리했다.)
class DrawInputForwarder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public LiveDrawOverlay owner;
    public void OnPointerDown(PointerEventData e) => owner?.OnSurfacePointerDown(e);
    public void OnDrag(PointerEventData e) => owner?.OnSurfaceDrag(e);
    public void OnPointerUp(PointerEventData e) => owner?.OnSurfacePointerUp(e);
}
