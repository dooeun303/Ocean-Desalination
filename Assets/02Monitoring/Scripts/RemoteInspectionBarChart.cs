using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

public class RemoteInspectionBarChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트")]
    public BarChart chart;

    public void OnPageRefresh()
    {
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null) chart = GetComponentInChildren<BarChart>();
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
            Debug.LogError($"[RemoteInspectionBarChart] Mock 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<InspectionRecord> items)
    {
        if (chart == null) return;
        
        chart.ClearData();
        
        // 임의의 점검자별 담당 점검 수치 추가
        string[] inspectors = { "김철수", "이영희", "박민수", "최다혜" };
        int[] counts = { 8, 11, 6, 5 };

        for (int i = 0; i < inspectors.Length; i++)
        {
            chart.AddYAxisData(inspectors[i]);
            chart.AddData(0, counts[i]);
        }
    }
}
