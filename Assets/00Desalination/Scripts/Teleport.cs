using NUnit.Framework;
using TMPro;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint; // 이동 위치
    [SerializeField] private Transform xrRig; // XR RIG


    public void OnButtonPressed()
    {
        StartCoroutine(FadeController.Instance.FadeAndTeleport(() =>
        {
            Debug.Log("[Maintenance] 텔레포트 실행!");
            xrRig.position = spawnPoint.position;
            xrRig.rotation = spawnPoint.rotation;
        }));
    }

}
