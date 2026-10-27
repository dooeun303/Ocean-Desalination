using System.Collections;
using UnityEngine;
using TMPro;

public class EquipmentPanel : MonoBehaviour
{
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

    // 2026-10-27: "설비 정보 패널에 실시간 데이터 값이 바인딩 되어야 해" - 패널이 보이는 동안 이 주기로 센서
    // 최신값(sensor_reading의 마지막 값)을 다시 불러온다. 값을 넣는 쪽은 통합 DB 연동 전까지 서버의
    // 테스트용 생성기(POST /api/sensor-sim).
    const float SensorRefreshSeconds = 3f;
    Canvas _canvas;
    bool _sensorsLoggedOnce;

    void Awake()
    {
        var canvas = GetComponentInChildren<Canvas>(includeInactive: true);
        if (canvas == null)
        {
            Debug.LogWarning("[EquipmentPanel] Canvas를 찾을 수 없습니다.");
            return;
        }

        _canvas = canvas;
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

    // 패널이 켜져 있는 동안만 센서값을 주기적으로 갱신(꺼져 있으면 요청 안 함)
    IEnumerator Start()
    {
        var wait = new WaitForSeconds(SensorRefreshSeconds);
        var marker = GetComponent<EquipmentMarker>();
        while (true)
        {
            if (_canvas != null && _canvas.gameObject.activeInHierarchy && marker != null && !string.IsNullOrEmpty(marker.equipmentId))
                yield return FetchSensors(marker.equipmentId);
            yield return wait;
        }
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
        EquipmentData d = null;
        yield return EquipmentApi.Get(uuid, data => d = data,
            e => Debug.LogError($"[EquipmentPanel] 장비 조회 실패: {e}"));
        if (d == null) yield break;

        if (nameText) nameText.text = d.name;
        if (idText) idText.text = d.id;
        if (typeText) typeText.text = TypeKo(d.equipment_type);
        if (statusText) statusText.text = StatusKo(d.status);
        if (modelText) modelText.text = d.model ?? "-";
        if (serialNoText) serialNoText.text = d.serial_no ?? "-";

        Debug.Log("[EquipmentPanel] 장비 데이터 바인딩 완료: " + d.name);
    }

    // ─────────────────────────────────────────
    // 센서 데이터 조회
    // ─────────────────────────────────────────
    private IEnumerator FetchSensors(string uuid)
    {
        SensorItem[] sensors = null;
        yield return EquipmentApi.Sensors(uuid, data => sensors = data,
            e => Debug.LogWarning($"[EquipmentPanel] 센서 조회 실패: {e}"));
        if (sensors == null) yield break;

        foreach (var sensor in sensors)
        {
            // AR QR 패널과 같은 자릿수(온도 F1 / 압력·진동 F2 / RPM 정수) - 같은 값이 다르게 보이지 않게.
            string fmt = sensor.sensor_type switch
            {
                "pressure" => "F2",
                "vibration" => "F2",
                "rpm" => "F0",
                _ => "F1",
            };
            string value = $"{sensor.value.ToString(fmt)} {sensor.unit}";
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

        if (!_sensorsLoggedOnce) { _sensorsLoggedOnce = true; Debug.Log("[EquipmentPanel] 센서 데이터 바인딩 완료(이후 " + SensorRefreshSeconds + "초마다 갱신)"); }
    }

    static string TypeKo(string t) => (t ?? "").ToUpper() switch
    {
        "PUMP" => "펌프",
        "HIGH_PRESSURE_PUMP" => "고압펌프",
        "VALVE" => "밸브",
        "TANK" => "탱크",
        "LAPTOP" => "노트북",
        _ => string.IsNullOrEmpty(t) ? "-" : t,
    };

    static string StatusKo(string s) => (s ?? "").ToLower() switch
    {
        "normal" => "정상",
        "warning" => "주의",
        "fault" => "고장",
        "maintenance" => "정비중",
        _ => string.IsNullOrEmpty(s) ? "-" : s,
    };

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