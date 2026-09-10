using UnityEngine;

// 회전하는 로딩 스피너. Show()/Hide()로 표시 제어.
public class LoadingSpinner : MonoBehaviour
{
    public float rotationSpeed = 180f; // 초당 회전 각도(도)

    void OnEnable()
    {
        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
