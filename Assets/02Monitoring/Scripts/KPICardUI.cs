using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class KPICardUI
{
    public Transform cardRoot;
    public TMP_Text titleText;       // 1_TitleRow
    public TMP_Text valueText;       // 2_ValueUnitRow/Value
    public TMP_Text unitText;        // 2_ValueUnitRow/Unit
    public TMP_Text subText;         // 3_SubTextRow/SubText
    public Image indicatorBg;        // 3_SubTextRow/Indicator
    public TMP_Text indicatorText;   // 3_SubTextRow/Indicator/Text
    public Image progressFill;       // 4_ProgressBar/Fill

    public void Setup(Transform root)
    {
        cardRoot = root;
        if (root == null) return;
        
        Transform tRow = root.Find("1_TitleRow");
        if (tRow) titleText = tRow.GetComponent<TextMeshProUGUI>();
        
        Transform vuRow = root.Find("2_ValueUnitRow");
        if (vuRow)
        {
            Transform v = vuRow.Find("Value");
            if (v) valueText = v.GetComponent<TextMeshProUGUI>();
            Transform u = vuRow.Find("Unit");
            if (u) unitText = u.GetComponent<TextMeshProUGUI>();
        }
        
        Transform sRow = root.Find("3_SubTextRow");
        if (sRow)
        {
            Transform s = sRow.Find("SubText");
            if (s) subText = s.GetComponent<TextMeshProUGUI>();
            Transform ind = sRow.Find("Indicator");
            if (ind)
            {
                indicatorBg = ind.GetComponent<Image>();
                Transform t = ind.Find("Text");
                if (t) indicatorText = t.GetComponent<TextMeshProUGUI>();
            }
        }
        
        Transform pb = root.Find("4_ProgressBar");
        if (pb)
        {
            Transform f = pb.Find("Fill");
            if (f) progressFill = f.GetComponent<Image>();
        }
    }

    private static readonly Color ColorNormal = new Color32(0x50, 0xB8, 0xB8, 0xFF);      // #50B8B8 (정상 가동)
    private static readonly Color ColorStop = new Color32(0x5F, 0x6F, 0x85, 0xFF);        // #5F6F85 (정지)
    private static readonly Color ColorWarning = new Color32(0xF5, 0x9E, 0x0B, 0xFF);     // #F59E0B (경고)
    private static readonly Color ColorDanger = new Color32(0xEF, 0x44, 0x44, 0xFF);      // #EF4444 (위험)
    private static readonly Color ColorImportant = new Color32(0x1A, 0x20, 0x2D, 0xFF);   // #1A202D (중요 수치)

    private Color GetValueTextColor(string title)
    {
        title = (title ?? "").ToLowerInvariant();

        // 1. Danger / Alarms / Issues / Warning categories (Inverse / Alert)
        if (title.Contains("위험") || title.Contains("치명") || title.Contains("critical") || title.Contains("미해결") || title.Contains("활성화") || title.Contains("이상"))
        {
            return ColorDanger;
        }

        // 2. Warning / Attention / Pending / In-progress categories
        if (title.Contains("주의") || title.Contains("경고") || title.Contains("warning") || title.Contains("진행") || title.Contains("대기"))
        {
            return ColorWarning;
        }

        // 3. Normal / Safety / Uptime / Completed categories
        if (title.Contains("정상") || title.Contains("완료") || title.Contains("해제") || title.Contains("가동률") || title.Contains("건전성") || title.Contains("uptime") || title.Contains("안전"))
        {
            return ColorNormal;
        }

        // 4. Default for cumulative/general numerical counts (핵심 수치)
        return ColorImportant;
    }

    public void UpdateUI(string value, string unit, string sub, string indText, float ratio, Color? indColor = null, Color? indTextColor = null, Color? customValueColor = null)
    {
        if (valueText)
        {
            valueText.text = value;
            if (customValueColor.HasValue)
            {
                valueText.color = customValueColor.Value;
            }
            else
            {
                valueText.color = GetValueTextColor(titleText != null ? titleText.text : "");
            }
        }

        if (unitText) unitText.text = unit;
        if (subText) subText.text = sub;
        if (indicatorText)
        {
            indicatorText.text = indText;
            if (indTextColor.HasValue) indicatorText.color = indTextColor.Value;
        }
        
        if (indicatorBg)
        {
            if (string.IsNullOrEmpty(indText) || indText == "-")
            {
                indicatorBg.gameObject.SetActive(false);
            }
            else
            {
                indicatorBg.gameObject.SetActive(true);
                if (indColor.HasValue) indicatorBg.color = indColor.Value;
            }
        }
        
        if (progressFill)
        {
            ratio = Mathf.Clamp01(ratio);
            progressFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            
            // Set the progress fill color to match the value text color, 
            // except if it is the dark important color (which we map to ColorNormal for high readability)
            if (customValueColor.HasValue)
            {
                progressFill.color = customValueColor.Value;
            }
            else
            {
                Color valColor = valueText.color;
                progressFill.color = (valColor == ColorImportant) ? ColorNormal : valColor;
            }
        }
    }
}