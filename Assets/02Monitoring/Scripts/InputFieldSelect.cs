using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InputFieldSelect : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectSprite;

    public void OnSelect(BaseEventData eventData)
    {
        targetImage.sprite = selectSprite;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetImage.sprite = normalSprite;
    }
}