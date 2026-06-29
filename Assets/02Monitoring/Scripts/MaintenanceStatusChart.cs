using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

// =============================================
// 정비 유지관리 작업 상태 Pie Chart (MockDataStore 버전)
// =============================================
public class MaintenanceStatusChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트")]
    public PieChart chart;

    public void OnPageRefresh()
    {
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null)
            chart = GetComponentInChildren<PieChart>();

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
            Debug.LogError($"[MaintenanceStatusChart] Mock 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<MaintenanceRecord> items)
    {
        if (chart == null) return;

        chart.ClearData();

        // 임의의 상태 데이터 채우기
        chart.AddData(0, 5, "대기");
        chart.AddData(0, 3, "진행중");
        chart.AddData(0, 12, "완료");
    }
}