using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class EquipmentPanel : MonoBehaviour
{
    public string serverUrl => ServerConfig.BaseUrl + "/api/equipment";
    public string sensorServerUrl => ServerConfig.BaseUrl + "/api/equipment";

    // 장비 정보 텍스트
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI idText;
    private TextMeshProUGUI typeText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI modelText;
    private TextMeshProUGUI serialNoText;

    // 센서 값 텍스트 (순서: temperature / pressure / vibration / rpm)
    private TextMeshProUGUI temperatureText;
    private TextMeshProUGUI pressureText;
    private TextMeshProUGUI vibrationText;
    private TextMeshProUGUI rpmText;

    // 센서 상태 텍스트
    private TextMeshProUGUI temperatureStatusText;
    private TextMeshProUGUI pressureStatusText;
    private TextMeshProUGUI vibrationStatusText;
    private TextMeshProUGUI rpmStatusText;

    private readonly string[] _sensorOrder = { "temperature", "pressure", "vibration", "rpm" };

    void Awake()
    {
        var canvas = GetComponentInChildren<Canvas>(includeInactive: true);
        if (canvas == null)
        {
            Debug.LogWarning("[EquipmentPanel] Canvas를 찾을 수 없습니다.");
            return;
        }

        Transform root = canvas.transform;

        // 장비 정보
        nameText = root.Find("Image/헤더/Name")?.GetComponent<TextMeshProUGUI>();
        idText = FindValueText(root, "ID");
        typeText = FindValueText(root, "Type");
        modelText = FindValueText(root, "Model");
        serialNoText = FindValueText(root, "SerialNo");
        statusText = FindValueText(root, "Status");

        // 센서 값 (칸 안에 Temperature / Pressure / Vibration / RPM 오브젝트 필요)
        temperatureText = FindValueText(root, "Temperature");
        pressureText = FindValueText(root, "Pressure");
        vibrationText = FindValueText(root, "Vibration");
        rpmText = FindValueText(root, "RPM");

        // 센서 상태 (세 번째 텍스트 — texts[2])
        temperatureStatusText = FindStatusText(root, "Temperature");
        pressureStatusText = FindStatusText(root, "Pressure");
        vibrationStatusText = FindStatusText(root, "Vibration");
        rpmStatusText = FindStatusText(root, "RPM");

        Debug.Log("[EquipmentPanel] UI 자동 연결 완료");
    }

    // ─────────────────────────────────────────
    // 객체 선택 시
    // ─────────────────────────────────────────
    public void OnSelected()
    {
        Debug.Log("[EquipmentPanel] OnSelected 호출됨");

        var marker = GetComponent<EquipmentMarker>();
        if (marker == null)
        {
            Debug.LogWarning("[EquipmentPanel] EquipmentMarker를 찾을 수 없습니다.");
            return;
        }

        string uuid = marker.equipmentId;
        if (string.IsNullOrEmpty(uuid))
        {
            Debug.LogWarning("[EquipmentPanel] equipmentId가 비어있습니다.");
            return;
        }

        Debug.Log("[EquipmentPanel] UUID: " + uuid);
        StartCoroutine(FetchEquipment(uuid));
        StartCoroutine(FetchSensors(uuid));
    }

    // ─────────────────────────────────────────
    // 장비 데이터 조회
    // ─────────────────────────────────────────
    private IEnumerator FetchEquipment(string uuid)
    {
        string url = $"{serverUrl}/{uuid}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[EquipmentPanel] 요청 실패: {req.error}");
            yield break;
        }

        var response = JsonConvert.DeserializeObject<EquipmentResponse>(req.downloadHandler.text);
        if (!response.success)
        {
            Debug.LogWarning($"[EquipmentPanel] success: false — {response.message}");
            yield break;
        }

        var d = response.data;
        if (nameText) nameText.text = d.name;
        if (idText) idText.text = d.id;
        if (typeText) typeText.text = d.equipment_type;
        if (statusText) statusText.text = d.status;
        if (modelText) modelText.text = d.model ?? "-";
        if (serialNoText) serialNoText.text = d.serial_no ?? "-";

        Debug.Log("[EquipmentPanel] 장비 데이터 바인딩 완료: " + d.name);
    }

    // ─────────────────────────────────────────
    // 센서 데이터 조회
    // ─────────────────────────────────────────
    private IEnumerator FetchSensors(string uuid)
    {
        string url = $"{sensorServerUrl}/{uuid}/sensors";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[EquipmentPanel] 센서 요청 실패: {req.error}");
            yield break;
        }

        var response = JsonConvert.DeserializeObject<SensorListResponse>(req.downloadHandler.text);
        if (!response.success || response.data == null)
        {
            Debug.LogWarning("[EquipmentPanel] 센서 데이터 없음");
            yield break;
        }

        foreach (var sensor in response.data)
        {
            string value = $"{sensor.value:F1} {sensor.unit}";
            string status = sensor.status switch
            {
                "critical" => "초과",
                "warning" => "주의",
                _ => "정상"
            };

            switch (sensor.sensor_type)
            {
                case "temperature":
                    if (temperatureText) temperatureText.text = value;
                    if (temperatureStatusText) temperatureStatusText.text = status;
                    break;
                case "pressure":
                    if (pressureText) pressureText.text = value;
                    if (pressureStatusText) pressureStatusText.text = status;
                    break;
                case "vibration":
                    if (vibrationText) vibrationText.text = value;
                    if (vibrationStatusText) vibrationStatusText.text = status;
                    break;
                case "rpm":
                    if (rpmText) rpmText.text = value;
                    if (rpmStatusText) rpmStatusText.text = status;
                    break;
            }
        }

        Debug.Log("[EquipmentPanel] 센서 데이터 바인딩 완료");
    }

    // ─────────────────────────────────────────
    // 값 텍스트 자동 찾기 (texts[1] = 값)
    // ─────────────────────────────────────────
    private TextMeshProUGUI FindValueText(Transform root, string parentName)
    {
        var target = root.Find("Image/칸/" + parentName);
        if (target == null)
        {
            Debug.LogWarning($"[EquipmentPanel] '{parentName}' 찾기 실패");
            return null;
        }

        var texts = target.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length < 2)
        {
            Debug.LogWarning($"[EquipmentPanel] '{parentName}' 텍스트가 2개 미만입니다.");
            return null;
        }

        return texts[1];
    }

    // ─────────────────────────────────────────
    // 상태 텍스트 자동 찾기 (texts[2] = 상태)
    // ─────────────────────────────────────────
    private TextMeshProUGUI FindStatusText(Transform root, string parentName)
    {
        var target = root.Find("Image/칸/" + parentName);
        if (target == null) return null;

        var texts = target.GetComponentsInChildren<TextMeshProUGUI>();
        return texts.Length > 2 ? texts[2] : null;
    }
}