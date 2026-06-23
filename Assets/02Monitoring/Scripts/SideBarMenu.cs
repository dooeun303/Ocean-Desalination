using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SidebarMenu : MonoBehaviour, IPointerClickHandler
{
    public GameObject subMenu;       // 서브메뉴 루트
    public SidebarGroup group;       // 사이드바 그룹 매니저

    private CanvasGroup subMenuCG;
    private LayoutElement subMenuLayout;
    private RectTransform subMenuRect;
    
    private bool isOpen = false;
    private float animDuration = 0.35f; // 좀 더 부드럽고 여유로운 시간
    private float targetHeight;

    void Awake()
    {
        if (subMenu != null)
        {
            subMenuCG = subMenu.GetComponent<CanvasGroup>();
            if (subMenuCG == null)
                subMenuCG = subMenu.AddComponent<CanvasGroup>();

            subMenuLayout = subMenu.GetComponent<LayoutElement>();
            if (subMenuLayout == null)
                subMenuLayout = subMenu.AddComponent<LayoutElement>();
                
            subMenuRect = subMenu.GetComponent<RectTransform>();

            // 실제 펼쳐졌을 때의 목표 높이 계산 (LayoutRebuilder를 통해 강제 계산)
            subMenu.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(subMenuRect);
            targetHeight = LayoutUtility.GetPreferredHeight(subMenuRect);

            // 서브메뉴의 내용이 튀어나오지 않게 마스크가 없다면 추가 (자연스러운 아코디언 필수)
            if (subMenu.GetComponent<RectMask2D>() == null && subMenu.GetComponent<Mask>() == null)
            {
                subMenu.AddComponent<RectMask2D>();
            }

            // 초기 상태: 닫힘 (높이 0, 투명도 0)
            subMenuCG.alpha = 0f;
            subMenuCG.interactable = false;
            subMenuCG.blocksRaycasts = false;
            
            subMenuLayout.preferredHeight = 0f;
            subMenuLayout.minHeight = 0f;
            subMenu.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (group != null)
        {
            group.SelectMenu(this);
        }
    }

    public void Open()
    {
        if (subMenu == null) return;
        isOpen = true;
        subMenu.SetActive(true);
        
        // 서브메뉴가 켜질 때 부모가 Active 상태이므로 여기서 높이를 다시 계산합니다.
        // LayoutElement 제한을 잠시 풀고 실제 컨텐츠의 높이를 구합니다.
        float startHeight = subMenuLayout.preferredHeight;
        subMenuLayout.preferredHeight = -1f;
        subMenuLayout.minHeight = -1f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(subMenuRect);
        float currentTargetHeight = LayoutUtility.GetPreferredHeight(subMenuRect);
        
        if (currentTargetHeight > 0)
        {
            targetHeight = currentTargetHeight;
        }
        
        // 애니메이션을 위해 시작 높이로 원복
        subMenuLayout.preferredHeight = startHeight;
        subMenuLayout.minHeight = startHeight;

        // 투명도 페이드인
        subMenuCG.DOKill();
        subMenuCG.DOFade(1f, animDuration).SetEase(Ease.OutQuart);
        subMenuCG.interactable = true;
        subMenuCG.blocksRaycasts = true;

        // 높이 스무스 애니메이션
        subMenuLayout.DOKill();
        DOTween.To(() => subMenuLayout.preferredHeight, x => { 
            subMenuLayout.preferredHeight = x; 
            subMenuLayout.minHeight = x; 
        }, targetHeight, animDuration).SetEase(Ease.OutQuart);
    }

    public void Close()
    {
        if (subMenu == null) return;
        isOpen = false;
        
        // 투명도 페이드아웃
        subMenuCG.DOKill();
        subMenuCG.DOFade(0f, animDuration).SetEase(Ease.OutQuart);
        subMenuCG.interactable = false;
        subMenuCG.blocksRaycasts = false;

        // 높이 스무스 애니메이션 (0으로)
        subMenuLayout.DOKill();
        DOTween.To(() => subMenuLayout.preferredHeight, x => { 
            subMenuLayout.preferredHeight = x; 
            subMenuLayout.minHeight = x; 
        }, 0f, animDuration).SetEase(Ease.OutQuart)
        .OnComplete(() => {
            subMenu.SetActive(false);
        });
    }
}