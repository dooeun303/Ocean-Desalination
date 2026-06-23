using Newtonsoft.Json;

[System.Serializable]
public class AlarmData
{
    [JsonProperty("type")] public string type;
    [JsonProperty("alarm_id")] public string alarm_id;
    [JsonProperty("alarm_code")] public string alarm_code;
    [JsonProperty("severity")] public string severity;
    [JsonProperty("description")] public string description;
    [JsonProperty("triggered_at")] public string triggered_at;
    [JsonProperty("equipment")] public EquipmentData equipment;  // ← 이걸로 교체
    [JsonProperty("mr_space_id")] public string mr_space_id;
}
