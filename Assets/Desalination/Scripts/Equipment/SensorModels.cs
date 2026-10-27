using System;

// GET /api/equipment/:id/sensors 응답 DTO (EquipmentPanel에서 사용)

[Serializable]
public class SensorListResponse
{
    public bool success;
    public SensorItem[] data;
}

[Serializable]
public class SensorItem
{
    public string sensor_id;
    public string sensor_type;
    public string sensor_name;
    public string unit;
    public float? min_threshold;  // float → float?
    public float? max_threshold;  // float → float?
    public float value;
    public string measured_at;
    public string status;
}
