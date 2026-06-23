using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadeUp : MonoBehaviour
{
    public float duration = 0.3f;
    public float offset = 50f;

    private CanvasGroup _cg;
    private RectTransform _rt;
    private Vector2 _originalPos;
    private bool _initialized = false;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_initialized) return;
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        
        _rt = GetComponent<RectTransform>();
        _originalPos = _rt.anchoredPosition;
        _initialized = true;
    }

    private void OnEnable()
    {
        Init();
        StopAllCoroutines();
        StartCoroutine(FadeUpRoutine());
    }

    private IEnumerator FadeUpRoutine()
    {
        _cg.alpha = 0f;
        _rt.anchoredPosition = _originalPos - new Vector2(0, offset);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // cubic ease out
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            _cg.alpha = easeT;
            _rt.anchoredPosition = Vector2.Lerp(_originalPos - new Vector2(0, offset), _originalPos, easeT);
            
            yield return null;
        }

        _cg.alpha = 1f;
        _rt.anchoredPosition = _originalPos;
    }

    /// <summary>
    /// 루트 오브젝트(팝업)를 받아서, 풀스크린 배경(Scrim)이 아닌 실제 패널 본체를 찾아 애니메이션을 부착합니다.
    /// </summary>
    public static void ApplyTo(GameObject root)
    {
        if (root == null) return;
        
        Transform target = null;
        
        // 1. 이름이 Panel인 자식 찾기
        Transform p = root.transform.Find("Panel");
        if (p != null) target = p;
        else 
        {
            // 2. 풀스크린이 아닌 자식 찾기
            foreach(Transform child in root.transform)
            {
                var rt = child.GetComponent<RectTransform>();
                if (rt != null && (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one))
                {
                    target = child;
                    break;
                }
            }
        }
        
        // 못 찾으면 루트 자체에 부착
        if (target == null) target = root.transform;

        if (target.GetComponent<UIFadeUp>() == null)
        {
            target.gameObject.AddComponent<UIFadeUp>();
        }
    }
}
