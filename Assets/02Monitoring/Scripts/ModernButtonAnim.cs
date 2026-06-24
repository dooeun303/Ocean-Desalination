using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public enum ModernButtonStyle
{
    Auto,       // 자동 감지
    Basic,      // 기본 버튼 (텍스트/테두리 색상 변경)
    Primary,    // 주요 버튼 (그림자 등 - 필요 시 추가 구현)
    IconOnly    // 아이콘 버튼 (배경 투명도 변경)
}

[RequireComponent(typeof(Button))]
public class ModernButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("스타일 가이드 설정")]
    public ModernButtonStyle style = ModernButtonStyle.Auto;
    
    private Button btn;
    private Vector3 originalScale;

    // 컴포넌트 캐싱
    private TMP_Text tmpText;
    private Image bgImage;
    private Outline outline;

    // 기본 색상 저장
    private Color originalTextColor;
    private Color originalOutlineColor;
    private Color originalBgColor;

    private bool isHovered = false;
    private bool isPressed = false;

    // 호버 목표 색상 (#50B8B8)
    private Color hoverPrimaryColor = new Color32(80, 184, 184, 255); 
    // 아이콘 전용 호버 배경 (검정 6%)
    private Color hoverIconBgColor = new Color(0f, 0f, 0f, 0.06f);

    void Awake()
    {
        btn = GetComponent<Button>();
        originalScale = transform.localScale;

        tmpText = GetComponentInChildren<TMP_Text>(true);
        bgImage = GetComponent<Image>();
        outline = GetComponent<Outline>();

        // 기존 색상 저장
        if (tmpText != null) originalTextColor = tmpText.color;
        if (outline != null) originalOutlineColor = outline.effectColor;
        if (bgImage != null) originalBgColor = bgImage.color;

        if (style == ModernButtonStyle.Auto)
        {
            DetectStyle();
        }
    }

    void DetectStyle()
    {
        if (tmpText == null) 
        {
            style = ModernButtonStyle.IconOnly;
        }
        else if (tmpText.color.r > 0.9f && tmpText.color.g > 0.9f && tmpText.color.b > 0.9f)
        {
            // 글자색이 흰색(#FFFFFF) 계열이면 주요 버튼 (Primary)
            style = ModernButtonStyle.Primary;
        }
        else
        {
            // 글자색이 어두운 계열(#3D5A6A)이면 기본 버튼 (Basic)
            style = ModernButtonStyle.Basic;
        }
    }

    void OnEnable()
    {
        transform.localScale = originalScale;
        isHovered = false;
        isPressed = false;
    }

    void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
        ResetColors();
    }

    void ResetColors()
    {
        isHovered = false;
        isPressed = false;
        if (tmpText != null) { tmpText.DOKill(); tmpText.color = originalTextColor; }
        if (outline != null) { outline.DOKill(); outline.effectColor = originalOutlineColor; }
        if (bgImage != null) { bgImage.DOKill(); bgImage.color = originalBgColor; }
    }

    private void UpdateVisuals(bool immediate = false)
    {
        float duration = immediate ? 0f : 0.2f;
        bool active = (isHovered || isPressed) && (btn == null || btn.interactable);

        if (style == ModernButtonStyle.Basic)
        {
            Color targetColor = active ? hoverPrimaryColor : originalTextColor;
            Color targetOutline = active ? hoverPrimaryColor : originalOutlineColor;

            if (tmpText != null)
            {
                tmpText.DOKill();
                if (immediate) tmpText.color = targetColor;
                else tmpText.DOColor(targetColor, duration);
            }
            if (outline != null)
            {
                outline.DOKill();
                if (immediate) outline.effectColor = targetOutline;
                else outline.DOColor(targetOutline, duration);
            }
        }
        else if (style == ModernButtonStyle.IconOnly)
        {
            Color targetBg = active ? hoverIconBgColor : originalBgColor;
            if (bgImage != null)
            {
                bgImage.DOKill();
                if (immediate) bgImage.color = targetBg;
                else bgImage.DOColor(targetBg, duration);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        
        isHovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;

        isHovered = false;
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        
        isPressed = true;
        UpdateVisuals();

        // 버튼 누를 때만 스케일 다운 (0.98배)
        transform.DOKill();
        transform.DOScale(originalScale * 0.98f, 0.1f).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        
        isPressed = false;
        UpdateVisuals();

        // 뗄 때 원래 크기로 복구 (1.0배)
        transform.DOKill();
        transform.DOScale(originalScale, 0.25f).SetEase(Ease.OutBack);
    }
}
