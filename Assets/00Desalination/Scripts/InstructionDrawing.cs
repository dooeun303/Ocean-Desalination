using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using TMPro;

// 화상통화 > 지시하기 그리기 창
public class InstructionDrawing : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // ─────────────────────────────────────────
    // 패널
    // ─────────────────────────────────────────
    [Header("패널")]
    public GameObject drawingPanel;         // 그리기 창 루트

    [Header("UI 연결")]
    public Transform videoImageParent;      // 영상 (1) 오브젝트 연결
    private RawImage videoRawImage;         // 런타임에 자동 탐색
    public RawImage capturedImage;         // 캡처된 이미지 표시
    public RectTransform canvasRect;        // 그리기 영역 RectTransform

    [Header("버튼")]
    public Button penModeButton;         // 펜 모드 버튼
    public Button stickerModeButton;     // 스티커 모드 버튼
    public Button undoButton;            // 이전 버튼
    public Button sendButton;            // 전송 버튼
    public Button cancelButton;          // 취소 버튼

    [Header("펜 설정")]
    public Color penColor = Color.red;
    public int penWidth = 5;

    [Header("스티커")]
    public Sprite checkSprite;           // 체크 스티커 스프라이트
    public GameObject stickerPrefab;        // 스티커 프리팹 (Image)

    [Header("서버")]
    public string serverUrl = "http://192.168.0.66:3000/api/files/instruction";

    [Header("알림")]
    public GameObject alertPanel;       // 알림 패널
    public TMP_Text alertText;        // 알림 텍스트

    // ─────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────
    private enum DrawMode { Pen, Sticker }
    private DrawMode _mode = DrawMode.Pen;

    private Texture2D _drawTexture;        // 그리기용 텍스처
    private bool _isDrawing = false;
    private Vector2 _lastPos;

    // Undo 스택
    private Stack<object> _undoStack = new Stack<object>();  // Texture2D 또는 GameObject

    void Start()
    {
        penModeButton?.onClick.AddListener(() => SetMode(DrawMode.Pen));
        stickerModeButton?.onClick.AddListener(() => SetMode(DrawMode.Sticker));
        undoButton?.onClick.AddListener(Undo);
        sendButton?.onClick.AddListener(OnSend);
        cancelButton?.onClick.AddListener(OnCancel);

        drawingPanel?.SetActive(false);
    }

    // ─────────────────────────────────────────
    // 지시하기 버튼 클릭 → 창 열기
    // ─────────────────────────────────────────
    public void Open()
    {
        // 런타임에 동적 생성된 RawImage 탐색
        if (videoRawImage == null && videoImageParent != null)
            videoRawImage = videoImageParent.GetComponentInChildren<RawImage>(includeInactive: true);

        if (videoRawImage == null || videoRawImage.texture == null)
        {
            Debug.LogWarning("[InstructionDrawing] 화상통화 텍스처 없음");
            return;
        }

        // 화상통화 화면 캡처
        CaptureVideoFrame();

        // 초기화
        _undoStack.Clear();
        SetMode(DrawMode.Pen);

        drawingPanel?.SetActive(true);
        Debug.Log("[InstructionDrawing] 그리기 창 열림");
    }

    // ─────────────────────────────────────────
    // 화상통화 화면 캡처
    // ─────────────────────────────────────────
    private void CaptureVideoFrame()
    {
        var src = videoRawImage.texture;
        int w = src.width;
        int h = src.height;

        // RenderTexture → Texture2D 변환
        var rt = RenderTexture.GetTemporary(w, h, 0);
        Graphics.Blit(src, rt);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        if (_drawTexture != null) Destroy(_drawTexture);
        _drawTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _drawTexture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        _drawTexture.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // 2026-08-26: "화면 캡처가 위아래 반대로 나온다"는 실기 확인으로 여기 있던 상하 반전
        // 보정을 없앴다 - 이 소스(화상통화 RawImage 텍스처)는 Blit→ReadPixels 라운드트립에서
        // 실제로는 뒤집히지 않는데, 여기서 한 번 더 뒤집어서 오히려 화면 미리보기와 그 위에 그리는
        // 좌표(펜/스티커)까지 전부 위아래가 뒤집힌 채로 어긋나 있었다. SendImage()도 짝을 맞춰서
        // 같이 없앴다(그동안은 이 위치의 잘못된 반전과 그쪽의 반전이 우연히 서로 상쇄돼서 AR로
        // 전송되는 최종 이미지만 정상으로 보였을 뿐).

        // 캡처 이미지에 표시
        if (capturedImage) capturedImage.texture = _drawTexture;

        Debug.Log($"[InstructionDrawing] 캡처 완료: {w}x{h}");
    }

    // ─────────────────────────────────────────
    // 모드 전환
    // ─────────────────────────────────────────
    private void SetMode(DrawMode mode)
    {
        _mode = mode;
        Debug.Log("[InstructionDrawing] 모드: " + mode);
    }

    // ─────────────────────────────────────────
    // 포인터 이벤트 (펜 / 스티커)
    // ─────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_drawTexture == null) return;  // 캡처 전이면 무시

        if (_mode == DrawMode.Pen)
        {
            _isDrawing = true;
            SaveTextureSnapshot();
            _lastPos = GetLocalPos(eventData);
        }
        else if (_mode == DrawMode.Sticker)
        {
            PlaceSticker(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mode != DrawMode.Pen || !_isDrawing) return;

        Vector2 currentPos = GetLocalPos(eventData);
        DrawLine(_lastPos, currentPos);
        _lastPos = currentPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDrawing = false;
    }

    // ─────────────────────────────────────────
    // 펜 그리기
    // ─────────────────────────────────────────
    private void DrawLine(Vector2 from, Vector2 to)
    {
        if (_drawTexture == null) return;

        int x0 = Mathf.RoundToInt(from.x);
        int y0 = Mathf.RoundToInt(from.y);
        int x1 = Mathf.RoundToInt(to.x);
        int y1 = Mathf.RoundToInt(to.y);

        // Bresenham 라인 알고리즘
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            PaintPixels(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        _drawTexture.Apply();
    }

    private void PaintPixels(int cx, int cy)
    {
        int half = penWidth / 2;
        for (int x = cx - half; x <= cx + half; x++)
            for (int y = cy - half; y <= cy + half; y++)
                if (x >= 0 && x < _drawTexture.width && y >= 0 && y < _drawTexture.height)
                    _drawTexture.SetPixel(x, y, penColor);
    }

    // ─────────────────────────────────────────
    // 스티커 배치
    // ─────────────────────────────────────────
    private void PlaceSticker(PointerEventData eventData)
    {
        if (stickerPrefab == null || canvasRect == null) return;

        var sticker = Instantiate(stickerPrefab, canvasRect);
        sticker.AddComponent<StickerMarker>();
        var img = sticker.GetComponent<Image>();
        if (img && checkSprite) img.sprite = checkSprite;

        // 위치 설정
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
        sticker.GetComponent<RectTransform>().anchoredPosition = localPos;

        // Undo 스택에 추가
        _undoStack.Push(sticker);

        Debug.Log("[InstructionDrawing] 스티커 배치: " + localPos);
    }

    // ─────────────────────────────────────────
    // Undo
    // ─────────────────────────────────────────
    private void SaveTextureSnapshot()
    {
        // 현재 텍스처 복사본 저장
        var snapshot = new Texture2D(_drawTexture.width, _drawTexture.height, TextureFormat.RGBA32, false);
        Graphics.CopyTexture(_drawTexture, snapshot);
        _undoStack.Push(snapshot);
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        var last = _undoStack.Pop();

        if (last is Texture2D snapshot)
        {
            // 펜 되돌리기
            Graphics.CopyTexture(snapshot, _drawTexture);
            _drawTexture.Apply();
            if (capturedImage) capturedImage.texture = _drawTexture;
            Destroy(snapshot);
        }
        else if (last is GameObject sticker)
        {
            // 스티커 제거
            Destroy(sticker);
        }
    }

    // ─────────────────────────────────────────
    // 전송
    // ─────────────────────────────────────────
    public void OnSend()
    {
        StartCoroutine(SendImage());
    }

    private IEnumerator SendImage()
    {
        yield return new WaitForEndOfFrame();

        int w = _drawTexture.width;
        int h = _drawTexture.height;

        // RenderTexture에 그리기 텍스처 + 스티커를 합성
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(_drawTexture, rt);

        // 스티커를 RenderTexture에 직접 그리기
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, 0, h);

        foreach (var marker in canvasRect.GetComponentsInChildren<StickerMarker>())
        {
            var child = marker.transform;
            var img = child.GetComponent<Image>();
            if (img == null || img.sprite == null) continue;

            var rect = child.GetComponent<RectTransform>();
            var pos = rect.anchoredPosition;
            var size = rect.sizeDelta;

            // 스티커 위치를 텍스처 좌표로 변환
            float scaleX = (float)w / canvasRect.rect.width;
            float scaleY = (float)h / canvasRect.rect.height;
            float x = (pos.x - canvasRect.rect.x) * scaleX - size.x * scaleX / 2;
            float y = (pos.y - canvasRect.rect.y) * scaleY - size.y * scaleY / 2;
            float sw = size.x * scaleX;
            float sh = size.y * scaleY;

            Graphics.DrawTexture(new Rect(x, y, sw, sh), img.sprite.texture);
        }

        GL.PopMatrix();

        // RenderTexture → Texture2D
        var final = new Texture2D(w, h, TextureFormat.RGB24, false);
        final.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        final.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // 2026-08-26: 여기 반전도 CaptureVideoFrame()과 짝을 맞춰서 없앴다(위 주석 참고) -
        // 이제 화면에 보이던 그대로(정상 방향) 인코딩해서 보낸다.
        byte[] pngData = final.EncodeToPNG();
        Destroy(final);

        var form = new WWWForm();
        form.AddBinaryData("file", pngData, $"instruction_{System.DateTime.Now:yyyyMMdd_HHmmss}.png", "image/png");

        using var req = UnityWebRequest.Post(serverUrl, form);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[InstructionDrawing] 전송 완료");
            ShowAlert("전송이 성공되었습니다.");
            StartCoroutine(CloseAfterDelay(3f));
        }
        else
        {
            Debug.LogError("[InstructionDrawing] 전송 실패: " + req.error);
            ShowAlert($"전송이 실패하였습니다.{ req.error}");
        }
    }

    // 알림 표시 (3초 후 자동 닫힘)
    private IEnumerator CloseAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        OnCancel();
    }

    private void ShowAlert(string message)
    {
        if (alertText) alertText.text = message;
        alertPanel?.SetActive(true);
        StartCoroutine(AutoHideAlert());
    }

    private IEnumerator AutoHideAlert()
    {
        yield return new WaitForSeconds(3f);
        alertPanel?.SetActive(false);
    }

    // ─────────────────────────────────────────
    // 취소
    // ─────────────────────────────────────────
    public void OnCancel()
    {
        // 스티커만 제거 (canvasRect에는 undo 버튼 등 다른 UI도 같이 매달려 있어
        // 자식을 전부 지우면 안 됨 — 마커가 붙은 스티커만 골라 지운다)
        foreach (var marker in canvasRect.GetComponentsInChildren<StickerMarker>())
            Destroy(marker.gameObject);

        _undoStack.Clear();
        drawingPanel?.SetActive(false);
    }

    // 런타임에 생성된 스티커를 canvasRect의 다른 UI 자식(버튼 등)과 구분하기 위한 마커
    private class StickerMarker : MonoBehaviour { }

    // ─────────────────────────────────────────
    // 유틸: 로컬 좌표 변환
    // ─────────────────────────────────────────
    private Vector2 GetLocalPos(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

        // 텍스처 좌표로 변환
        var rect = canvasRect.rect;
        float x = (localPos.x - rect.x) / rect.width * _drawTexture.width;
        float y = (localPos.y - rect.y) / rect.height * _drawTexture.height;
        return new Vector2(x, y);
    }
}