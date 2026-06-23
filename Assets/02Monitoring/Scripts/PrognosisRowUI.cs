using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrognosisRowUI : MonoBehaviour
{
    [Header("Columns")]
    public TMP_Text idText;
    public TMP_Text timestampText;
    public TMP_Text equipmentNameText;
    public TMP_Text locationText;
    public TMP_Text contentText;
    public TMP_Text levelText;
    public TMP_Text statusText;

    [Header("Visuals")]
    public Image levelIndicator;
    public Image statusBadge;
    public TMP_Text statusBadgeText;

    public void SetData(PrognosisData data)
    {
        if (idText) idText.text = data.id;
        if (timestampText) timestampText.text = data.timestamp;
        if (equipmentNameText) equipmentNameText.text = data.equipmentName;
        if (locationText) locationText.text = data.location;
        if (contentText) contentText.text = data.content;
        
        if (levelText) 
        {
            levelText.text = data.level;
            levelText.color = GetLevelColor(data.level);
        }

        if (levelIndicator) levelIndicator.color = GetLevelColor(data.level);

        if (statusBadgeText) statusBadgeText.text = data.status;
        if (statusBadge) statusBadge.color = GetStatusColor(data.status);
    }

    private Color GetLevelColor(string level) => level switch
    {
        "Critical" => new Color(0.9f, 0.2f, 0.2f),
        "Warning" => new Color(0.9f, 0.6f, 0.1f),
        "Normal" => new Color(0.2f, 0.7f, 0.3f),
        _ => Color.gray
    };

    private Color GetStatusColor(string status) => status switch
    {
        "Active" => new Color(0.9f, 0.2f, 0.2f, 0.2f),
        "Resolved" => new Color(0.2f, 0.7f, 0.3f, 0.2f),
        _ => new Color(0.5f, 0.5f, 0.5f, 0.2f)
    };
}

[System.Serializable]
public class PrognosisData
{
    public string id;
    public string timestamp;
    public string equipmentName;
    public string location;
    public string content;
    public string level;
    public string status;
}
