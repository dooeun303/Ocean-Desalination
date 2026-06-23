using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
///
/// <summary>
/// Express 서버의 GET /api/equipment/:id 를 호출하여
/// equipment 데이터를 가져오는 서비스
///
/// [사용법]
/// EquipmentApiService.Instance.GetEquipmentById(
///     equipmentId,
///     onSuccess: (data) => { ... },
///     onError:   (msg)  => { ... }
/// );
/// </summary>
/// 
public class EquipmentApiService : MonoBehaviour
{

    public static EquipmentApiService Instance { get; private set; }

    [Header("서버설정")]
    [SerializeField] private string baseUrl = "http://localhost:3000";
    [SerializeField] private float timeoutSeconds = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GetEquipmentById(int equipmentId, Action<EquipmentData> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchEquipment(equipmentId, onSuccess, onError));
        
    }

    private IEnumerator FetchEquipment(int equipmentId, Action<EquipmentData> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/api/equipment/{equipmentId}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = (int)timeoutSeconds;
        request.SetRequestHeader( "Content-Type", "application/json");

        yield return request.SendWebRequest(); // 요청 보내기

        // 요청 실패시
        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"요청 실패 ({request.responseCode}): {request.error}");
            yield break;
        }

        // 응답올때 json을 EquipmentResponse 엔티티로 변환
        EquipmentResponse response;

        try
        {
            response = JsonUtility.FromJson<EquipmentResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke($"JSON 파싱 오류: {e.Message}");
            yield break;
        }

        if (response.success && response.data != null)
        {
            onSuccess?.Invoke(response.data);
            
        } else
        {
            onError?.Invoke(response.message ?? "데이터를 찾을 수 없습니다.");
        }


    }
}
