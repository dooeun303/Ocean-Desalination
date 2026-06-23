using UnityEngine;

public class ProximityUI : MonoBehaviour
{

    public GameObject uiCanvas;

    // 장비의 콜라이더에 닿으면 실행되는 함수
    private void OnTriggerEnter(Collider other)
    {
        // XR Rig의 콜라이더가 닿으면 UI Canvas가 켜짐
        if (other.CompareTag("Player"))
        {
            uiCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            uiCanvas.SetActive(false);
        }
    }
}
