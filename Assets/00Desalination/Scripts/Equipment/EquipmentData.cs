using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class EquipmentData
{
    [JsonProperty("id")] public string id;
    [JsonProperty("name")] public string name;
    [JsonProperty("equipment_type")] public string equipment_type;
    [JsonProperty("model")] public string model;
    [JsonProperty("serial_no")] public string serial_no;
    [JsonProperty("status")] public string status;
}

[System.Serializable]
public class EquipmentResponse
{
    public bool success;
    public EquipmentData data;
    public string message;
}
