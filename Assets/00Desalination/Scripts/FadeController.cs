using System.Collections;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    
    private void Awake()
    {
        Instance = this;
        fadeImage.color = new Color(0, 0, 0, 0); // image 컬러 초기화 (검정색)
    }

    void Update()
    {
        // 페이드 캔버스의 이미지의 위치가 카메라의 앞에 위치하도록 함
        transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.1f;
        transform.rotation = Camera.main.transform.rotation;
    }

    // FadeAndTeleport 함수 (페이드 함수에 텔레포트까지 합침)
    public IEnumerator FadeAndTeleport(System.Action onMidpoint)
    {
        yield return StartCoroutine(Fade(0f, 1f)); // 페이드인

        onMidpoint?.Invoke(); // 텔레포트 실행

        yield return new WaitForSeconds(0.5f); // 0.1초 대기

        yield return StartCoroutine(Fade(1f, 0f)); // 페이드아웃

    }
    
    // Fade 함수 (이미지 색상변경)
    private IEnumerator Fade(float from, float to) 
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, to);
    }

    float elapsed = 0f; 
}