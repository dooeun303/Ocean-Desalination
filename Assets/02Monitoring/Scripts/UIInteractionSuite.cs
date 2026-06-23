using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace Monitoring.UI
{
    public enum InteractionType
    {
        KPICard,      // Move up 2px, Teal shadow (12% #50B8B8)
        Panel,        // Black shadow only (6% #000000)
        MenuTile,     // Move down 9px
        ButtonAction, // Scale 0.98 on press
        TableRow,     // Background highlight (3% #50B8B8) + Shadow
        SidebarItem   // Background highlight (6% #50B8B8) + Scale 0.98
    }

    public class UIInteractionSuite : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private InteractionType type;
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private Image shadowImage;
        [SerializeField] private Image background;
        
        [SerializeField] private string tooltipMessage;
        
        private Vector2 originalPos;
        private Color originalBgColor;
        private bool isInitialized = false;

        private void Awake()
        {
            if (targetRect == null) targetRect = GetComponent<RectTransform>();
            if (background == null) background = GetComponent<Image>();
            
            if (background != null) originalBgColor = background.color;
            
            if (shadowImage != null)
            {
                Color sc = shadowImage.color;
                shadowImage.color = new Color(sc.r, sc.g, sc.b, 0);
            }
        }

        private void Start()
        {
            originalPos = targetRect.anchoredPosition;
            isInitialized = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInitialized) return;

            if (!string.IsNullOrEmpty(tooltipMessage) && TooltipController.Instance != null)
            {
                TooltipController.Instance.Show(tooltipMessage, GetComponent<RectTransform>());
            }
            
            switch (type)
            {
                case InteractionType.KPICard:
                    targetRect.DOAnchorPos(originalPos + Vector2.up * 2f, 0.2f).SetEase(Ease.OutCubic);
                    if (shadowImage != null) shadowImage.DOFade(0.12f, 0.2f);
                    break;

                case InteractionType.Panel:
                    if (shadowImage != null) shadowImage.DOFade(0.06f, 0.2f);
                    break;

                case InteractionType.MenuTile:
                    targetRect.DOAnchorPos(originalPos + Vector2.down * 9f, 0.12f).SetEase(Ease.OutCubic);
                    break;

                case InteractionType.TableRow:
                    if (background != null) background.DOColor(new Color(0.314f, 0.722f, 0.722f, 0.03f), 0.1f);
                    if (shadowImage != null) shadowImage.DOFade(0.06f, 0.1f);
                    break;

                case InteractionType.SidebarItem:
                    if (background != null) background.DOColor(new Color(0.314f, 0.722f, 0.722f, 0.06f), 0.12f);
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInitialized) return;

            if (!string.IsNullOrEmpty(tooltipMessage) && TooltipController.Instance != null)
            {
                TooltipController.Instance.Hide();
            }

            switch (type)
            {
                case InteractionType.KPICard:
                    targetRect.DOAnchorPos(originalPos, 0.2f).SetEase(Ease.OutCubic);
                    if (shadowImage != null) shadowImage.DOFade(0, 0.2f);
                    break;

                case InteractionType.Panel:
                    if (shadowImage != null) shadowImage.DOFade(0, 0.2f);
                    break;

                case InteractionType.MenuTile:
                    targetRect.DOAnchorPos(originalPos, 0.12f).SetEase(Ease.OutCubic);
                    break;

                case InteractionType.TableRow:
                    if (background != null) background.DOColor(originalBgColor, 0.1f);
                    if (shadowImage != null) shadowImage.DOFade(0, 0.1f);
                    break;

                case InteractionType.SidebarItem:
                    if (background != null) background.DOColor(originalBgColor, 0.12f);
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (type == InteractionType.ButtonAction || type == InteractionType.MenuTile || type == InteractionType.SidebarItem)
            {
                targetRect.DOScale(0.98f, 0.1f);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (type == InteractionType.ButtonAction || type == InteractionType.MenuTile || type == InteractionType.SidebarItem)
            {
                targetRect.DOScale(1f, 0.1f);
            }
        }
    }
}
