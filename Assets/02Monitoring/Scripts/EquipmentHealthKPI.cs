using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentHealthKPI : MonoBehaviour, IPageRefreshable
{
    [Header("KPI 카드 UI")]
    public KPICardUI cardOverall = new KPICardUI();
    public KPICardUI cardNormal = new KPICardUI();
    public KPICardUI cardWarning = new KPICardUI();
    public KPICardUI cardCritical = new KPICardUI();

    // Palette Colors (Soft Pastel Backgrounds & Dark Readable Matching Text)
    private readonly Color ColorPointBg = new Color32(230, 244, 234, 255);    // #E6F4EA
    private readonly Color ColorPointText = new Color32(19, 115, 51, 255);     // #137333
    
    private readonly Color ColorActiveBg = new Color32(224, 247, 250, 255);    // #E0F7FA
    private readonly Color ColorActiveText = new Color32(0, 131, 143, 255);    // #00838F
    
    private readonly Color ColorWarningBg = new Color32(254, 243, 199, 255);   // #FEF3C7
    private readonly Color ColorWarningText = new Color32(183, 121, 31, 255);  // #B7791F
    
    private readonly Color ColorCriticalBg = new Color32(252, 232, 230, 255);  // #FCE8E6
    private readonly Color ColorCriticalText = new Color32(197, 34, 31, 255);  // #C5221F

    private void Awake()
    {
        AutoBindUI();
    }

    private void OnEnable()
    {
        OnPageRefresh();
    }

    public void OnPageRefresh()
    {
        StartCoroutine(FetchAndUpdate());
    }

    private void AutoBindUI()
    {
        Transform row = transform.Find("Row");
        if (row == null) row = transform.Find("Row_KPI");
        if (row == null && transform.childCount > 1) row = transform.GetChild(1);

        if (row != null && row.childCount >= 4)
        {
            cardOverall.Setup(row.GetChild(0));
            cardNormal.Setup(row.GetChild(1));
            cardWarning.Setup(row.GetChild(2));
            cardCritical.Setup(row.GetChild(3));
        }
    }

    private IEnumerator FetchAndUpdate()
    {
        int totalAlarms = MonitoringMockDataStore.Alarms.Count;
        int activeCriticalAlarms = 0;
        int normalCount = 0;
        int warningCount = 0;
        int criticalCount = 0;

        // Group active alarms by equipment name
        var activeAlarmsByEquip = new Dictionary<string, List<AlarmRecord>>();
        foreach (var alarm in MonitoringMockDataStore.Alarms)
        {
            if (alarm.IsActive)
            {
                if (!activeAlarmsByEquip.ContainsKey(alarm.EquipmentName))
                {
                    activeAlarmsByEquip[alarm.EquipmentName] = new List<AlarmRecord>();
                }
                activeAlarmsByEquip[alarm.EquipmentName].Add(alarm);

                if ((alarm.Severity ?? "").ToLowerInvariant() == "critical")
                {
                    activeCriticalAlarms++;
                }
            }
        }

        foreach (var equip in MonitoringMockDataStore.Equipments)
        {
            if (activeAlarmsByEquip.TryGetValue(equip.name, out var list))
            {
                bool hasCritical = list.Exists(a => (a.Severity ?? "").ToLowerInvariant() == "critical");
                bool hasWarning = list.Exists(a => (a.Severity ?? "").ToLowerInvariant() == "warning");
                if (hasCritical) criticalCount++;
                else if (hasWarning) warningCount++;
                else normalCount++;
            }
            else
            {
                normalCount++;
            }
        }

        int totalCount = normalCount + warningCount + criticalCount;
        if (totalCount == 0) totalCount = 10;

        yield return null;

        // Calculate dynamic plant health percentage:
        float plantHealth = 100f;
        if (totalAlarms > 0)
        {
            plantHealth = 100f - (activeCriticalAlarms * 0.1f);
            plantHealth = Mathf.Clamp(plantHealth, 95.0f, 100f);
        }

        // Apply visual updates
        cardOverall.UpdateUI(plantHealth.ToString("F1"), "%", "설비 가동 및 상태 안전", "▲ 0.1", plantHealth / 100f, ColorPointBg, ColorPointText);
        cardNormal.UpdateUI(normalCount.ToString(), "대", "정상 운영 중", "-", (float)normalCount / totalCount, ColorActiveBg, ColorActiveText);
        cardWarning.UpdateUI(warningCount.ToString(), "대", "정밀 점검 필요", "-", (float)warningCount / totalCount, ColorWarningBg, ColorWarningText);
        cardCritical.UpdateUI(criticalCount.ToString(), "대", "즉시 조치 필요", "-", (float)criticalCount / totalCount, ColorCriticalBg, ColorCriticalText);
    }
}