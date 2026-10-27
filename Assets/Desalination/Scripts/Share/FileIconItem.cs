using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 정보공유 아이템 프리팹에 붙는 스크립트
// 드래그 기능 + 파일 ID 보유
public class FileIconItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI 연결")]
    public Image fileImage;    // 파일 이미지
    public TMP_Text fileName;     // 파일 이름

    [Header("드래그 설정")]
    public Canvas rootCanvas;   // 루트 캔버스 (Inspector 연결)

    // 파일 데이터
    public string FileId { get; private set; }
    public string FileName { get; private set; }
    public string FileUrl { get; private set; }

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Vector3 _originPosition;   // 드래그 시작 위치
    private Transform _originParent;     // 드래그 시작 부모
    private bool _droppedOnZone;    // 드롭존에 성공적으로 놓였는지

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup 없으면 자동 추가
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // 외부에서 데이터 설정
    public void SetData(string fileId, string name, string url, Sprite sprite = null)
    {
        FileId = fileId;
        FileName = name;
        FileUrl = url;

        if (fileName) fileName.text = name;
        if (fileImage && sprite != null) fileImage.sprite = sprite;
    }

    // ─────────────────────────────────────────
    // 드래그 시작
    // ─────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        _originPosition = _rect.position;
        _originParent = transform.parent;

        // 루트 캔버스로 올려서 다른 UI 위에 그려지도록
        transform.SetParent(rootCanvas.transform);
        transform.SetAsLastSibling();

        // 드롭존이 레이캐스트 받을 수 있도록 본인은 차단 해제
        _canvasGroup.blocksRaycasts = false;

        Debug.Log($"[FileIconItem] 드래그 시작: {FileName} ({FileId})");
    }

    // ─────────────────────────────────────────
    // 드래그 중
    // ─────────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        // 컨트롤러 포인터 위치로 이동
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPoint
        );
        _rect.position = worldPoint;
    }

    // ─────────────────────────────────────────
    // 드롭존에서 호출 — 드롭 성공 처리
    // ─────────────────────────────────────────
    public void OnDropSuccess()
    {
        _droppedOnZone = true;

        // 드롭 성공해도 원위치로 복귀
        transform.SetParent(_originParent);
        _rect.position = _originPosition;
    }

    // ─────────────────────────────────────────
    // 드래그 끝 (드롭존에 안 놓인 경우 원위치)
    // ─────────────────────────────────────────
    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        if (!_droppedOnZone)
        {
            // 드롭존 미적중 — 원위치
            transform.SetParent(_originParent);
            _rect.position = _originPosition;
            Debug.Log($"[FileIconItem] 원위치: {FileName}");
        }

        _droppedOnZone = false;
    }
}