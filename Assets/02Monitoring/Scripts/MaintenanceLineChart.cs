using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

// =============================================
// 정비 유지관리 월별 건수 Line Chart (MockDataStore 버전)
// =============================================
public class MaintenanceLineChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트")]
    public LineChart chart;

    public void OnPageRefresh()
    {
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null)
            chart = GetComponentInChildren<LineChart>();

        OnPageRefresh();
    }

    private async Task FetchAndDraw()
    {
        try
        {
            List<MaintenanceRecord> records = await MonitoringMockDataStore.SearchMaintenances("");
            DrawChart(records);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MaintenanceLineChart] Mock 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<MaintenanceRecord> items)
    {
        if (chart == null) return;

        // 차트 초기화
        chart.ClearData();

        // 임의의 정적 월별 이력 수치 추가
        string[] monthLabels = { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월" };
        int[] dummyMonthlyCounts = { 5, 8, 12, 7, 15, 10, 6, 9, 11, 14, 8, 13 };

        for (int i = 0; i < 12; i++)
        {
            chart.AddXAxisData(monthLabels[i]);
            chart.AddData(0, dummyMonthlyCounts[i]);
        }
    }
}