using System.Collections;
using UnityEngine;
using UnityEngine.Video;

// 화상통화_일반 패널에 붙는 스크립트
// 패널이 켜질 때마다(SetActive(true)) 일정 시간 로딩 스피너(+ 연결중 영상)를 먼저 보여줌
public class PanelConnectingOverlay : MonoBehaviour
{
    public GameObject connectingSpinner;       // 배경 + 스피너 + 영상을 담은 오브젝트
    public VideoPlayer connectingVideoPlayer;  // 연결중 영상 재생용 (선택)
    public float delaySeconds = 3f;

    void OnEnable()
    {
        StartCoroutine(ShowSpinner());
    }

    void OnDisable()
    {
        connectingVideoPlayer?.Stop();
    }

    private IEnumerator ShowSpinner()
    {
        connectingSpinner?.SetActive(true);

        yield return new WaitForSeconds(delaySeconds);

        // 스피너가 사라진 뒤 영상 재생 시작
        connectingSpinner?.SetActive(false);
        connectingVideoPlayer?.Play();
    }
}
