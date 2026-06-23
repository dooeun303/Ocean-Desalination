using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace Monitoring.UI
{
    public class TooltipController : MonoBehaviour
    {
        public static TooltipController Instance { get; private set; }

        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image background;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private Vector2 offset = new Vector2(0, -8f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (canvasGroup != null) canvasGroup.alpha = 0;
            if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
        }

        public void Show(string message, RectTransform invoker)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            tooltipText.text = message;
            tooltipPanel.gameObject.SetActive(true);

            // Rebuild layout to get correct size
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

            // Get the camera from the canvas
            Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            // Position below the invoker
            Vector3[] corners = new Vector3[4];
            invoker.GetWorldCorners(corners);
            
            // Bottom center of the invoker in world space
            Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;
            
            // Convert to screen space
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, bottomCenterWorld);

            // Convert screen space to local position of tooltip's parent
            RectTransform parent = tooltipPanel.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, cam, out Vector2 localPos);

            tooltipPanel.anchoredPosition = localPos + offset;

            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
        }

        public void Hide()
        {
            if (tooltipPanel == null) return;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).OnComplete(() => tooltipPanel.gameObject.SetActive(false));
        }
    }
}
