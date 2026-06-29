using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

public class RemoteInspectionStatusChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트")]
    public PieChart chart;

    public void OnPageRefresh()
    {
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null) chart = GetComponentInChildren<PieChart>();
        OnPageRefresh();
    }

    private async Task FetchAndDraw()
    {
        try
        {
            List<InspectionRecord> records = await MonitoringMockDataStore.SearchInspections("");
            DrawChart(records);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RemoteInspectionStatusChart] Mock 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<InspectionRecord> items)
    {
        if (chart == null) return;

        chart.ClearData();
        chart.AddData(0, 24, "정상");
        chart.AddData(0, 4, "이상발견");
        chart.AddData(0, 7, "대기/진행");
    }
}
