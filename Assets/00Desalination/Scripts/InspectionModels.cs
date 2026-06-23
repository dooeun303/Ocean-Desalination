using System;

[Serializable]
public class InspectionListResponse
{
    public bool success;
    public InspectionItem[] data;
}

[Serializable]
public class InspectionItem
{
    public string equipment_id;
    public string equipment_name;
    public string equipment_type;
    public string location;
    public string inspection_id;
    public string inspection_result;    // normal / issue_found
    public string inspection_note;     // 점검 메모
    public string scheduled_at;
    public string completed_at;
    public string inspection_status;      // pending / in_progress / completed
    public string latest_alarm_severity;  // critical / warning / null
    public string latest_alarm_at;
}

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

[Serializable]
public class SaveInspectionRequest
{
    public string inspection_id;   // 기존 remote_inspection 레코드 id
    public string equipment_id;
    public string inspector_id;
    public string result;          // normal / issue_found
    public string note;
}

[Serializable]
public class SaveInspectionResponse
{
    public bool success;
    public string message;
}