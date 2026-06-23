using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Components")]
    public Image backgroundImage;
    public Image iconImage;
    public GameObject tooltip;

    [Header("Settings")]
    public Sprite normalBg;
    public Sprite selectedBg;
    public Color normalIconColor = new Color(0x3D / 255f, 0x5A / 255f, 0x6A / 255f, 1f);
    public Color selectedIconColor = Color.white;
    public float hoverOffsetY = -10f;   // 음수 = 아래로
    public float animDuration = 0.15f;

    [Header("Group")]
    public MenuButtonGroup group;

    private bool isSelected = false;
    private CanvasGroup tooltipCG;
    private Vector3 bgOriginalPos;
    private Vector3 tooltipOriginalPos;

    void Start()
    {
        // 한 프레임 기다렸다가 위치 저장
        StartCoroutine(InitAfterLayout());
    }

    IEnumerator InitAfterLayout()
    {
        yield return null;

        if (tooltip != null)
        {
            tooltipOriginalPos = tooltip.transform.localPosition;

            tooltipCG = tooltip.GetComponent<CanvasGroup>();
            if (tooltipCG == null)
                tooltipCG = tooltip.AddComponent<CanvasGroup>();

            tooltipCG.alpha = 0f;
            tooltip.SetActive(false);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipCG != null)
        {
            tooltip.SetActive(true);
            tooltipCG.DOKill();
            tooltipCG.DOFade(1f, animDuration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCG != null)
        {
            tooltipCG.DOKill();
            tooltipCG.DOFade(0f, animDuration)
                .OnComplete(() => tooltip.SetActive(false));
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        group.SelectButton(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selected)
        {
            if (backgroundImage != null)
            {
                if (selectedBg != null) backgroundImage.sprite = selectedBg;
            }
            if (iconImage != null) iconImage.color = selectedIconColor;

            if (tooltipCG != null)
            {
                tooltipCG.DOKill();
                tooltipCG.DOFade(0f, animDuration)
                    .OnComplete(() => tooltip.SetActive(false));
            }
        }
        else
        {
            if (backgroundImage != null && normalBg != null) backgroundImage.sprite = normalBg;
            if (iconImage != null) iconImage.color = normalIconColor;
        }
    }
}