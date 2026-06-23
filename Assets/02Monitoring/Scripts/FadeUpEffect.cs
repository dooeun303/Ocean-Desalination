using UnityEngine;
using DG.Tweening;

namespace Monitoring.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeUpEffect : MonoBehaviour
    {
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float distance = 12f;
        [SerializeField] private bool playOnEnable = true;

        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private Vector2 targetPos;
        private bool isInitialized = false;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        public void Play()
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (!isInitialized)
            {
                targetPos = rect.anchoredPosition;
                isInitialized = true;
            }

            rect.DOKill();
            canvasGroup.DOKill();

            rect.anchoredPosition = targetPos - Vector2.up * distance;
            canvasGroup.alpha = 0;

            rect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic);
            canvasGroup.DOFade(1f, duration).SetEase(Ease.InCubic);
        }
    }
}
