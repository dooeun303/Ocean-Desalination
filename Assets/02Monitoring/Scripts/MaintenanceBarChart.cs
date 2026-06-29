using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

// =============================================
// 정비 유지관리 설비별 발생 건수 Bar Chart (MockDataStore 버전)
// =============================================
public class MaintenanceBarChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트")]
    public BarChart chart;

    public void OnPageRefresh()
    {
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null)
            chart = GetComponentInChildren<BarChart>();

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
            Debug.LogError($"[MaintenanceBarChart] Mock 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<MaintenanceRecord> items)
    {
        if (chart == null) return;

        // 차트 데이터 초기화
        chart.ClearData();

        // 임의의 정적 설비별 정비 이력 건수 추가
        string[] equips = { "스마트 크레인 A", "스마트 컨베이어 B", "고속 모터 C", "스마트 펌프 D", "자동화 로봇 E" };
        int[] counts = { 6, 4, 9, 3, 5 };

        for (int i = 0; i < equips.Length; i++)
        {
            chart.AddYAxisData(equips[i]);
            chart.AddData(0, counts[i]);
        }
    }
}