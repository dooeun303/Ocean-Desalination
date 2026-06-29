using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

public class RemoteInspectionLineChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트")]
    public LineChart chart;

    public void OnPageRefresh()
    {
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null) chart = GetComponentInChildren<LineChart>();
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
            Debug.LogError($"[RemoteInspectionLineChart] Mock 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<InspectionRecord> items)
    {
        if (chart == null) return;
        
        chart.ClearData();
        
        // 임의의 정적 월별 원격 점검 수치 추가
        string[] monthLabels = { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월" };
        int[] dummyMonthlyCounts = { 3, 6, 9, 5, 11, 8, 4, 7, 10, 12, 6, 9 };

        for (int i = 0; i < 12; i++)
        {
            chart.AddXAxisData(monthLabels[i]);
            chart.AddData(0, dummyMonthlyCounts[i]);
        }
    }
}
