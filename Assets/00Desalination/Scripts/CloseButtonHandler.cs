using DG.Tweening; // 닷 트윈 사용을 위한 using 문
using UnityEngine;

public class CloseButtonHandler : MonoBehaviour
{
    // 팝업 윈도우
    public PanelHandler popupWindow;

    // 메뉴 닫기 클릭시 동작
    public void OnButtonClickForMenu()
    {
        var seq = DOTween.Sequence();

        seq.Append(transform.DOScale(0.095f, 0.1f));
        seq.Append(transform.DOScale(0.105f, 0.1f));
        seq.Append(transform.DOScale(0.1f, 0.1f));

        // 연결된 팝업 윈도우에서 닫기 동작 실행
        seq.Play().OnComplete(() =>
        {
            popupWindow.HideForMenu();
        }); 
    }

    // 보통 닫기 클릭시 동작
    public void OnButtonClickForGeneral()
    {
        var seq = DOTween.Sequence();

        seq.Append(transform.DOScale(0.095f, 0.1f));
        seq.Append(transform.DOScale(0.105f, 0.1f));
        seq.Append(transform.DOScale(0.1f, 0.1f));

        // 연결된 팝업 윈도우에서 닫기 동작 실행
        seq.Play().OnComplete(() =>
        {
            popupWindow.Hide();
        });
    }


}
