using UnityEngine;

// 화상통화_일반 패널에 붙는 스크립트
// 패널이 켜지면(SetActive(true)) 오버레이(배경+스피너)를 띄워두고,
// 실제로 AR 영상이 연결되면(JoinChannelVideoToken이 HideOverlay 호출) 사라진다.
public class PanelConnectingOverlay : MonoBehaviour
{
    public GameObject connectingSpinner; // 배경 + 스피너를 담은 오브젝트

    void OnEnable()
    {
        ShowOverlay();
    }

    // 패널이 열릴 때 오버레이(연결 대기 화면)를 보여줌
    public void ShowOverlay()
    {
        connectingSpinner?.SetActive(true);
    }

    // 실제로 상대방(AR) 영상이 연결되면 호출 → 오버레이 숨김
    public void HideOverlay()
    {
        connectingSpinner?.SetActive(false);
    }
}
