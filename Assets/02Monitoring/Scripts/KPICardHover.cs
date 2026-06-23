using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class KPICardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject hoverImage;
    private Vector2 originalPos;
    private RectTransform rect;
    private bool isInitialized = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        if (hoverImage != null) hoverImage.SetActive(false);
    }

    void OnDisable()
    {
        if (rect != null && isInitialized)
        {
            rect.DOKill();
            rect.anchoredPosition = originalPos;
        }
        isInitialized = false;
    }

    private void EnsureInitialized()
    {
        if (!isInitialized)
        {
            originalPos = rect.anchoredPosition;
            isInitialized = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverImage != null) hoverImage.SetActive(true);
        EnsureInitialized();
        rect.DOKill();
        rect.DOAnchorPos(originalPos + new Vector2(0, 2), 0.15f).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverImage != null) hoverImage.SetActive(false);
        if (isInitialized)
        {
            rect.DOKill();
            rect.DOAnchorPos(originalPos, 0.15f).SetEase(Ease.OutQuad);
        }
    }
}