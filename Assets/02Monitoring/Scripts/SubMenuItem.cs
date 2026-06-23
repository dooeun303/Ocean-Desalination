using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SubMenuItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public GameObject targetPage;        // 타겟 페이지
    public MenuButtonGroup pageManager;  // 페이지 관리자

    [Header("Hover Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0f);    
    public Color hoverColor = new Color(1f, 1f, 1f, 0.1f);   
    public Color clickColor = new Color(1f, 1f, 1f, 0.25f);  

    private Image bgImage;

    void Awake()
    {
        bgImage = GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (bgImage != null)
        {
            bgImage.DOKill();
            bgImage.DOColor(hoverColor, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (bgImage != null)
        {
            bgImage.DOKill();
            bgImage.DOColor(normalColor, 0.2f).SetEase(Ease.OutQuad);
        }
        
        // 마우스가 빠져나갈 때 스케일 원상복구 (안전 장치)
        transform.DOKill();
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 누를 때 살짝 작아지는 인터랙션 (스타일 가이드 마이크로 애니메이션)
        transform.DOKill();
        transform.DOScale(0.95f, 0.1f).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 뗄 때 원래 크기로 쫀득하게 돌아오는 인터랙션
        transform.DOKill();
        transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetPage == null || pageManager == null) return;
        pageManager.ShowPage(targetPage);

        // 클릭(선택) 피드백 이펙트
        if (bgImage != null)
        {
            bgImage.DOKill();
            bgImage.color = clickColor;
            bgImage.DOColor(hoverColor, 0.3f);
        }
    }
}