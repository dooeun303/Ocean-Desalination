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
    }

    void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
        ResetColors();
    }

    void ResetColors()
    {
        if (tmpText != null) { tmpText.DOKill(); tmpText.color = originalTextColor; }
        if (outline != null) { outline.DOKill(); outline.effectColor = originalOutlineColor; }
        if (bgImage != null) { bgImage.DOKill(); bgImage.color = originalBgColor; }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        
        // 스케일 업 제거 (가이드라인 준수)
        
        // 호버 색상 변경
        if (style == ModernButtonStyle.Basic)
        {
            if (tmpText != null) { tmpText.DOKill(); tmpText.DOColor(hoverPrimaryColor, 0.2f); }
            if (outline != null) { outline.DOKill(); outline.DOColor(hoverPrimaryColor, 0.2f); }
        }
        else if (style == ModernButtonStyle.IconOnly)
        {
            if (bgImage != null) { bgImage.DOKill(); bgImage.DOColor(hoverIconBgColor, 0.2f); }
        }
        // Primary 버튼의 그림자(Shadow) 호버 처리는 별도의 하위 오브젝트나 Material이 필요하여 생략 또는 추후 연결
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;

        // 색상 원상복구
        if (style == ModernButtonStyle.Basic)
        {
            if (tmpText != null) { tmpText.DOKill(); tmpText.DOColor(originalTextColor, 0.2f); }
            if (outline != null) { outline.DOKill(); outline.DOColor(originalOutlineColor, 0.2f); }
        }
        else if (style == ModernButtonStyle.IconOnly)
        {
            if (bgImage != null) { bgImage.DOKill(); bgImage.DOColor(originalBgColor, 0.2f); }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        
        // 버튼 누를 때만 스케일 다운 (0.98배)
        transform.DOKill();
        transform.DOScale(originalScale * 0.98f, 0.1f).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;
        
        // 뗄 때 원래 크기로 복구 (1.0배)
        transform.DOKill();
        transform.DOScale(originalScale, 0.25f).SetEase(Ease.OutBack);
    }
}
