using DG.Tweening;
using System.Xml.Schema;
using UnityEngine;

public class PanelHandler : MonoBehaviour
{
    private void Start()
    {
        // 닷트윈 초기화
        DOTween.Init();

        // transform의 scale값을 모두 0.005로 변경 (원래 scale값이 0.05배 였기 때문)
        transform.localScale = Vector3.one * 0.005f;
        gameObject.SetActive(false);
    }

    
    // 메뉴 보이기
    public void ShowForMenu()
    {
        gameObject.SetActive(true);

        // DOTween 함수를 차례대로 수행
        var seq = DOTween.Sequence();

        // 목표 Scale값, 시간
        seq.Append(transform.DOScale(0.022f, 0.2f));
        seq.Append(transform.DOScale(0.02f, 0.1f));

        seq.Play();
    }

    // 메뉴 숨기기
    public void HideForMenu()
    {
        var seq = DOTween.Sequence();

        transform.localScale = Vector3.one * 0.01f;
        seq.Append(transform.DOScale(0.022f, 0.1f));
        seq.Append(transform.DOScale(0.001f, 0.2f));

        // 
        seq.Play().OnComplete(() =>
        {
            gameObject.SetActive(false);
        });

    }

    // 일반 패널 보이기
    public void Show()
    {
        gameObject.SetActive(true);

        // DOTween 함수를 차례대로 수행하게 해줍니다.
        var seq = DOTween.Sequence();

        // DOScale 의 첫 번째 파라미터는 목표 Scale 값, 두 번째는 시간입니다.
        seq.Append(transform.DOScale(1.1f, 0.2f));
        seq.Append(transform.DOScale(1f, 0.1f));

        seq.Play();
    }

    // 일반 패널 숨기기
    public void Hide()
    {
        var seq = DOTween.Sequence();

        transform.localScale = Vector3.one * 0.2f;

        seq.Append(transform.DOScale(1.1f, 0.1f));
        seq.Append(transform.DOScale(0.2f, 0.2f));

        // OnComplete 는 seq 에 설정한 애니메이션의 플레이가 완료되면
        // { } 안에 있는 코드가 수행된다는 의미입니다.
        // 여기서는 닫기 애니메이션이 완료된 후 객첼르 비활성화 합니다.
        seq.Play().OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}