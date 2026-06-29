using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FaultPredictionKPIs : MonoBehaviour, IPageRefreshable
{
    [Header("KPI 카드 UI")]
    public KPICardUI cardActive = new KPICardUI();
    public KPICardUI cardReleased = new KPICardUI();
    public KPICardUI cardTotal = new KPICardUI();

    // Palette Colors (Soft Pastel Backgrounds & Dark Readable Matching Text)
    private readonly Color ColorPointBg = new Color32(230, 244, 234, 255);    // #E6F4EA
    private readonly Color ColorPointText = new Color32(19, 115, 51, 255);     // #137333

    private readonly Color ColorCriticalBg = new Color32(252, 232, 230, 255);  // #FCE8E6
    private readonly Color ColorCriticalText = new Color32(197, 34, 31, 255);  // #C5221F

    private readonly Color ColorInfoBg = new Color32(232, 240, 254, 255);      // #E8F0FE
    private readonly Color ColorInfoText = new Color32(26, 115, 232, 255);     // #1A73E8

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

        if (row != null && row.childCount >= 3)
        {
            cardActive.Setup(row.GetChild(0));
            cardReleased.Setup(row.GetChild(1));
            cardTotal.Setup(row.GetChild(2));
        }
    }

    private IEnumerator FetchAndUpdate()
    {
        int activeCount = 0;
        int criticalCount = 0;
        int releasedCount = 0;
        int totalCount = 0;

        foreach (var r in MonitoringMockDataStore.Alarms)
        {
            totalCount++;
            if (r.IsActive)
            {
                activeCount++;
                if ((r.Severity ?? "").ToLowerInvariant() == "critical")
                {
                    criticalCount++;
                }
            }
            else
            {
                releasedCount++;
            }
        }

        yield return null;

        float activeRatio = totalCount > 0 ? (float)activeCount / totalCount : 0f;
        float releasedRatio = totalCount > 0 ? (float)releasedCount / totalCount : 0f;

        cardActive.UpdateUI(activeCount.ToString(), "건", "미해결 고장예지 알람", $"▲ {criticalCount}", activeRatio, ColorCriticalBg, ColorCriticalText);
        cardReleased.UpdateUI(releasedCount.ToString(), "건", "조치 완료 및 자동 해제", "-", releasedRatio, ColorPointBg, ColorPointText);
        cardTotal.UpdateUI(totalCount.ToString(), "건", "누적 고장예지 알람", "-", 1.0f, ColorInfoBg, ColorInfoText);
    }
}
