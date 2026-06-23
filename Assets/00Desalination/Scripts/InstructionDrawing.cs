using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using TMPro;

// È­»óÅëÈ­ > Áö½ÃÇÏ±â ±×¸®±â Ã¢
public class InstructionDrawing : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÆÐ³Î
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("ÆÐ³Î")]
    public GameObject drawingPanel;         // ±×¸®±â Ã¢ ·çÆ®

    [Header("UI ¿¬°á")]
    public Transform videoImageParent;      // ¿µ»ó (1) ¿ÀºêÁ§Æ® ¿¬°á
    private RawImage videoRawImage;         // ·±Å¸ÀÓ¿¡ ÀÚµ¿ Å½»ö
    public RawImage capturedImage;         // Ä¸Ã³µÈ ÀÌ¹ÌÁö Ç¥½Ã
    public RectTransform canvasRect;        // ±×¸®±â ¿µ¿ª RectTransform

    [Header("¹öÆ°")]
    public Button penModeButton;         // Ææ ¸ðµå ¹öÆ°
    public Button stickerModeButton;     // ½ºÆ¼Ä¿ ¸ðµå ¹öÆ°
    public Button undoButton;            // ÀÌÀü ¹öÆ°
    public Button sendButton;            // Àü¼Û ¹öÆ°
    public Button cancelButton;          // Ãë¼Ò ¹öÆ°

    [Header("Ææ ¼³Á¤")]
    public Color penColor = Color.red;
    public int penWidth = 5;

    [Header("½ºÆ¼Ä¿")]
    public Sprite checkSprite;           // Ã¼Å© ½ºÆ¼Ä¿ ½ºÇÁ¶óÀÌÆ®
    public GameObject stickerPrefab;        // ½ºÆ¼Ä¿ ÇÁ¸®ÆÕ (Image)

    [Header("¼­¹ö")]
    public string serverUrl = "http://192.168.0.66:3000/api/files/instruction";

    [Header("¾Ë¸²")]
    public GameObject alertPanel;       // ¾Ë¸² ÆÐ³Î
    public TMP_Text alertText;        // ¾Ë¸² ÅØ½ºÆ®

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³»ºÎ »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private enum DrawMode { Pen, Sticker }
    private DrawMode _mode = DrawMode.Pen;

    private Texture2D _drawTexture;        // ±×¸®±â¿ë ÅØ½ºÃ³
    private bool _isDrawing = false;
    private Vector2 _lastPos;

    // Undo ½ºÅÃ
    private Stack<object> _undoStack = new Stack<object>();  // Texture2D ¶Ç´Â GameObject

    void Start()
    {
        penModeButton?.onClick.AddListener(() => SetMode(DrawMode.Pen));
        stickerModeButton?.onClick.AddListener(() => SetMode(DrawMode.Sticker));
        undoButton?.onClick.AddListener(Undo);
        sendButton?.onClick.AddListener(OnSend);
        cancelButton?.onClick.AddListener(OnCancel);

        drawingPanel?.SetActive(false);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Áö½ÃÇÏ±â ¹öÆ° Å¬¸¯ ¡æ Ã¢ ¿­±â
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void Open()
    {
        // ·±Å¸ÀÓ¿¡ µ¿Àû »ý¼ºµÈ RawImage Å½»ö
        if (videoRawImage == null && videoImageParent != null)
            videoRawImage = videoImageParent.GetComponentInChildren<RawImage>(includeInactive: true);

        if (videoRawImage == null || videoRawImage.texture == null)
        {
            Debug.LogWarning("[InstructionDrawing] È­»óÅëÈ­ ÅØ½ºÃ³ ¾øÀ½");
            return;
        }

        // È­»óÅëÈ­ È­¸é Ä¸Ã³
        CaptureVideoFrame();

        // ÃÊ±âÈ­
        _undoStack.Clear();
        SetMode(DrawMode.Pen);

        drawingPanel?.SetActive(true);
        Debug.Log("[InstructionDrawing] ±×¸®±â Ã¢ ¿­¸²");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // È­»óÅëÈ­ È­¸é Ä¸Ã³
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void CaptureVideoFrame()
    {
        var src = videoRawImage.texture;
        int w = src.width;
        int h = src.height;

        // RenderTexture ¡æ Texture2D º¯È¯
        var rt = RenderTexture.GetTemporary(w, h, 0);
        Graphics.Blit(src, rt);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        _drawTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _drawTexture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        _drawTexture.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // yÃà µÚÁý±â (Agora ¿µ»óÀÌ µÚÁýÇô¼­ ³ª¿À´Â °æ¿ì º¸Á¤)
        FlipTextureVertically(_drawTexture);

        // Ä¸Ã³ ÀÌ¹ÌÁö¿¡ Ç¥½Ã
        if (capturedImage) capturedImage.texture = _drawTexture;

        Debug.Log($"[InstructionDrawing] Ä¸Ã³ ¿Ï·á: {w}x{h}");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸ðµå ÀüÈ¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void SetMode(DrawMode mode)
    {
        _mode = mode;
        Debug.Log("[InstructionDrawing] ¸ðµå: " + mode);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Æ÷ÀÎÅÍ ÀÌº¥Æ® (Ææ / ½ºÆ¼Ä¿)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_drawTexture == null) return;  // Ä¸Ã³ ÀüÀÌ¸é ¹«½Ã

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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ææ ±×¸®±â
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void DrawLine(Vector2 from, Vector2 to)
    {
        if (_drawTexture == null) return;

        int x0 = Mathf.RoundToInt(from.x);
        int y0 = Mathf.RoundToInt(from.y);
        int x1 = Mathf.RoundToInt(to.x);
        int y1 = Mathf.RoundToInt(to.y);

        // Bresenham ¶óÀÎ ¾Ë°í¸®Áò
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ½ºÆ¼Ä¿ ¹èÄ¡
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void PlaceSticker(PointerEventData eventData)
    {
        if (stickerPrefab == null || canvasRect == null) return;

        var sticker = Instantiate(stickerPrefab, canvasRect);
        var img = sticker.GetComponent<Image>();
        if (img && checkSprite) img.sprite = checkSprite;

        // À§Ä¡ ¼³Á¤
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
        sticker.GetComponent<RectTransform>().anchoredPosition = localPos;

        // Undo ½ºÅÃ¿¡ Ãß°¡
        _undoStack.Push(sticker);

        Debug.Log("[InstructionDrawing] ½ºÆ¼Ä¿ ¹èÄ¡: " + localPos);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Undo
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void SaveTextureSnapshot()
    {
        // ÇöÀç ÅØ½ºÃ³ º¹»çº» ÀúÀå
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
            // Ææ µÇµ¹¸®±â
            Graphics.CopyTexture(snapshot, _drawTexture);
            _drawTexture.Apply();
            if (capturedImage) capturedImage.texture = _drawTexture;
            Destroy(snapshot);
        }
        else if (last is GameObject sticker)
        {
            // ½ºÆ¼Ä¿ Á¦°Å
            Destroy(sticker);
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Àü¼Û
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void OnSend()
    {
        StartCoroutine(SendImage());
    }

    private IEnumerator SendImage()
    {
        yield return new WaitForEndOfFrame();

        int w = _drawTexture.width;
        int h = _drawTexture.height;

        // RenderTexture¿¡ ±×¸®±â ÅØ½ºÃ³ + ½ºÆ¼Ä¿¸¦ ÇÕ¼º
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(_drawTexture, rt);

        // ½ºÆ¼Ä¿¸¦ RenderTexture¿¡ Á÷Á¢ ±×¸®±â
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, 0, h);

        foreach (Transform child in canvasRect)
        {
            var img = child.GetComponent<Image>();
            if (img == null || img.sprite == null) continue;

            var rect = child.GetComponent<RectTransform>();
            var pos = rect.anchoredPosition;
            var size = rect.sizeDelta;

            // ½ºÆ¼Ä¿ À§Ä¡¸¦ ÅØ½ºÃ³ ÁÂÇ¥·Î º¯È¯
            float scaleX = (float)w / canvasRect.rect.width;
            float scaleY = (float)h / canvasRect.rect.height;
            float x = (pos.x - canvasRect.rect.x) * scaleX - size.x * scaleX / 2;
            float y = (pos.y - canvasRect.rect.y) * scaleY - size.y * scaleY / 2;
            float sw = size.x * scaleX;
            float sh = size.y * scaleY;

            Graphics.DrawTexture(new Rect(x, y, sw, sh), img.sprite.texture);
        }

        GL.PopMatrix();

        // RenderTexture ¡æ Texture2D
        var final = new Texture2D(w, h, TextureFormat.RGB24, false);
        final.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        final.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // YÃà µÚÁý±â
        var flipped = FlipTextureVertically(final);
        Destroy(final);

        byte[] pngData = flipped.EncodeToPNG();
        Destroy(flipped);

        var form = new WWWForm();
        form.AddBinaryData("file", pngData, $"instruction_{System.DateTime.Now:yyyyMMdd_HHmmss}.png", "image/png");

        using var req = UnityWebRequest.Post(serverUrl, form);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[InstructionDrawing] Àü¼Û ¿Ï·á");
            ShowAlert("Àü¼ÛÀÌ ¼º°øµÇ¾ú½À´Ï´Ù.");
            StartCoroutine(CloseAfterDelay(3f));
        }
        else
        {
            Debug.LogError("[InstructionDrawing] Àü¼Û ½ÇÆÐ: " + req.error);
            ShowAlert($"Àü¼ÛÀÌ ½ÇÆÐÇÏ¿´½À´Ï´Ù.{ req.error}");
        }
    }

    private Texture2D FlipTextureVertically(Texture2D src)
    {
        int w = src.width, h = src.height;
        var flipped = new Texture2D(w, h, src.format, false);
        for (int y = 0; y < h; y++)
            flipped.SetPixels(0, y, w, 1, src.GetPixels(0, h - 1 - y, w, 1));
        flipped.Apply();
        return flipped;
    }

    // ¾Ë¸² Ç¥½Ã (3ÃÊ ÈÄ ÀÚµ¿ ´ÝÈû)
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ãë¼Ò
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void OnCancel()
    {
        // ½ºÆ¼Ä¿ ¸ðµÎ Á¦°Å
        foreach (Transform child in canvasRect)
            Destroy(child.gameObject);

        _undoStack.Clear();
        drawingPanel?.SetActive(false);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // À¯Æ¿: ·ÎÄÃ ÁÂÇ¥ º¯È¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private Vector2 GetLocalPos(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

        // ÅØ½ºÃ³ ÁÂÇ¥·Î º¯È¯
        var rect = canvasRect.rect;
        float x = (localPos.x - rect.x) / rect.width * _drawTexture.width;
        float y = (localPos.y - rect.y) / rect.height * _drawTexture.height;
        return new Vector2(x, y);
    }
}