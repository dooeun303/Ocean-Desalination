using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 화상통화 영상 영역에 붙는 스크립트
// 아이콘이 드롭되면 WebSocket으로 AR에 전송
public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 연결")]
    public UnityEngine.UI.Image dropZoneImage;  // 드롭존 이미지 (하이라이트용)

    [Header("색상")]
    public Color normalColor = new Color(1f, 1f, 1f, 0f);       // 기본 (투명)
    public Color highlightColor = new Color(0.3f, 0.7f, 1f, 0.09f); // 드래그 올렸을 때

    [Header("완료 알림")]
    public GameObject alertPanel;   // 공유 완료/실패 토스트 패널
    public TMP_Text alertText;      // 토스트 텍스트

    void Start()
    {
        // Image 자동 찾기 (프리팹 연결 불필요)
        if (dropZoneImage == null)
            dropZoneImage = GetComponent<Image>();

        if (dropZoneImage) dropZoneImage.color = normalColor;
    }

    // ─────────────────────────────────────────
    // 드래그가 영역 위에 올라왔을 때
    // ─────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (dropZoneImage) dropZoneImage.color = highlightColor;
    }

    // ─────────────────────────────────────────
    // 드래그가 영역에서 벗어났을 때
    // ─────────────────────────────────────────
    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropZoneImage) dropZoneImage.color = normalColor;
    }

    // ─────────────────────────────────────────
    // 드롭됐을 때
    // ─────────────────────────────────────────
    public void OnDrop(PointerEventData eventData)
    {
        if (dropZoneImage) dropZoneImage.color = normalColor;

        // 드롭된 오브젝트에서 FileIconItem 가져오기
        var icon = eventData.pointerDrag?.GetComponent<FileIconItem>();
        if (icon == null)
        {
            Debug.LogWarning("[DropZone] FileIconItem을 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[DropZone] 드롭 감지: {icon.FileName} ({icon.FileId})");

        // 드롭 성공 알림
        icon.OnDropSuccess();

        // WebSocket으로 AR에 전송
        SendToAR(icon.FileId, icon.FileUrl);
    }

    // ─────────────────────────────────────────
    // AR로 전송
    // ─────────────────────────────────────────
    private void SendToAR(string fileId, string fileUrl)
    {
        var message = JsonUtility.ToJson(new FileShareMessage
        {
            type = "file_share",
            file_id = fileId,
            file_url = fileUrl
        });

        // 기존 WebSocket으로 전송
        bool sent = AlarmWebSocket.Instance.Send(message);

        Debug.Log($"[DropZone] AR로 전송: {message}");

        ShowAlert(sent ? "정보공유가 완료되었습니다." : "정보공유에 실패했습니다.");
    }

    // ─────────────────────────────────────────
    // 완료/실패 토스트 (3초 후 자동 닫힘)
    // ─────────────────────────────────────────
    private void ShowAlert(string message)
    {
        if (alertText) alertText.text = message;
        alertPanel?.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AutoHideAlert());
    }

    private IEnumerator AutoHideAlert()
    {
        yield return new WaitForSeconds(3f);
        alertPanel?.SetActive(false);
    }
}

[System.Serializable]
public class FileShareMessage
{
    public string type;
    public string file_id;
    public string file_url;
}