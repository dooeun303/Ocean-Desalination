using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaintenanceKPI : MonoBehaviour, IPageRefreshable
{
    [Header("KPI 카드 UI")]
    public KPICardUI cardTotal = new KPICardUI();
    public KPICardUI cardInProgress = new KPICardUI();
    public KPICardUI cardCompleted = new KPICardUI();
    public KPICardUI cardUptime = new KPICardUI();

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
            cardTotal.Setup(row.GetChild(0));
            cardInProgress.Setup(row.GetChild(1));
            cardCompleted.Setup(row.GetChild(2));
            cardUptime.Setup(row.GetChild(3));
        }
    }

    private IEnumerator FetchAndUpdate()
    {
        int activeYear = 2026;
        int activeMonth = 5;
        
        int totalThisMonth = 0;
        int totalLastMonth = 0;
        int inProgressThisMonth = 0;
        int completedThisMonth = 0;
        int activeCriticalAlarms = 0;

        // 1. Get the month with maximum maintenance logs
        var counts = new Dictionary<string, int>();
        foreach (var r in MonitoringMockDataStore.Maintenances)
        {
            string key = $"{r.ScheduledAt.Year}-{r.ScheduledAt.Month}";
            if (!counts.ContainsKey(key)) counts[key] = 0;
            counts[key]++;
        }

        string maxKey = null;
        int maxCnt = -1;
        foreach (var kvp in counts)
        {
            if (kvp.Value > maxCnt)
            {
                maxCnt = kvp.Value;
                maxKey = kvp.Key;
            }
        }

        if (maxKey != null)
        {
            var parts = maxKey.Split('-');
            activeYear = int.Parse(parts[0]);
            activeMonth = int.Parse(parts[1]);
        }

        // Calculate previous month dynamically
        DateTime targetDate = new DateTime(activeYear, activeMonth, 1);
        DateTime prevDate = targetDate.AddMonths(-1);
        int lastYear = prevDate.Year;
        int lastMonth = prevDate.Month;

        foreach (var r in MonitoringMockDataStore.Maintenances)
        {
            if (r.ScheduledAt.Year == activeYear && r.ScheduledAt.Month == activeMonth)
            {
                totalThisMonth++;
                string status = (r.Status ?? "").ToLowerInvariant();
                if (status == "in_progress")
                {
                    inProgressThisMonth++;
                }
                else if (status == "completed")
                {
                    completedThisMonth++;
                }
            }
            else if (r.ScheduledAt.Year == lastYear && r.ScheduledAt.Month == lastMonth)
            {
                totalLastMonth++;
            }
        }

        // 3. Get active critical alarms to calculate uptime
        foreach (var r in MonitoringMockDataStore.Alarms)
        {
            if (r.IsActive && (r.Severity ?? "").ToLowerInvariant() == "critical")
            {
                activeCriticalAlarms++;
            }
        }

        yield return null;

        int diff = totalThisMonth - totalLastMonth;
        string diffSign = diff >= 0 ? "+" : "";
        string compareText = $"전월 대비 {diffSign}{diff}건";

        float uptime = 100f - (activeCriticalAlarms * 0.1f);
        uptime = Mathf.Clamp(uptime, 95.0f, 100f);

        float totalRatio = totalLastMonth > 0 ? (float)totalThisMonth / totalLastMonth : (totalThisMonth > 0 ? 1f : 0f);
        float progressRatio = totalThisMonth > 0 ? (float)inProgressThisMonth / totalThisMonth : 0f;
        float completedRatio = totalThisMonth > 0 ? (float)completedThisMonth / totalThisMonth : 0f;

        cardTotal.UpdateUI(totalThisMonth.ToString(), "건", $"{activeMonth}월 전체 작업", compareText, totalRatio, ColorPointBg, ColorPointText);
        cardInProgress.UpdateUI(inProgressThisMonth.ToString(), "건", "처리 중인 작업", "-", progressRatio, ColorWarningBg, ColorWarningText);
        cardCompleted.UpdateUI(completedThisMonth.ToString(), "건", "완료된 작업", "-", completedRatio, ColorActiveBg, ColorActiveText);
        cardUptime.UpdateUI(uptime.ToString("F1"), "%", "설비 평균 가동률", "▲ 0.1", uptime / 100f, ColorPointBg, ColorPointText);
    }
}