using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class testText : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.DOAnchorPosY(0, 5).SetDelay(1.5f).SetEase(Ease.InOutBounce);

        Text txt = GetComponent<Text>();
        txt.DOText("Dotween Example", 2, true, ScrambleMode.All).SetDelay(2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
