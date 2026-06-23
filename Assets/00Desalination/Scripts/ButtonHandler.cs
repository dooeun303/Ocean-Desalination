using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ButtonHandler : MonoBehaviour
{
    // 팝업 윈도우
    public PanelHandler popupWindow;

    // 메뉴 열기 버튼 클릭시 동작
    public void OnButtonClickForMenu()
    {
        var seq = DOTween.Sequence();

        seq.Append(transform.DOScale(0.095f, 0.1f));
        seq.Append(transform.DOScale(0.105f, 0.1f));
        seq.Append(transform.DOScale(0.1f, 0.1f));

        // 연결된 팝업 윈도우에서 메뉴 열기 동작 실행
        seq.Play().OnComplete(() => {
            popupWindow.ShowForMenu();
        });
    }


    // 보통 열기 버튼 클릭시 동작
    public void OnButtonClickForGeneral()
    {
        var seq = DOTween.Sequence();

        seq.Append(transform.DOScale(0.095f, 0.1f));
        seq.Append(transform.DOScale(0.105f, 0.1f));
        seq.Append(transform.DOScale(0.1f, 0.1f));

        // 연결된 팝업 윈도우에서 메뉴 열기 동작 실행
        seq.Play().OnComplete(() => {
            popupWindow.Show();
        });
    }
}