using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

// =============================================
// 정비 유지관리 월별 건수 Line Chart (PostgreSQL 직접 연동)
// =============================================
public class MaintenanceLineChart : MonoBehaviour, IPageRefreshable
{
    [Header("DB 접속 설정")]
    public string host = "127.0.0.1";
    public int port = 5433;
    public string database = "test";
    public string user = "postgres";
    public string password = "0000";
    public string schema = "aaa";

    [Header("차트")]
    public LineChart chart;

    private PostgresMaintenanceService _dbService;

    public void OnPageRefresh()
    {
        if (_dbService == null)
        {
            _dbService = new PostgresMaintenanceService(host, port, database, user, password, schema);
        }
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
            if (_dbService == null)
            {
                _dbService = new PostgresMaintenanceService(host, port, database, user, password, schema);
            }

            // 모든 유지보수 데이터 조회 (keyword = "")
            List<MaintenanceRecord> records = await _dbService.SearchMaintenancesAsync("");
            DrawChart(records);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MaintenanceLineChart] DB 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<MaintenanceRecord> items)
    {
        if (chart == null) return;

        // 월별 건수 배열 (0 = 1월 ... 11 = 12월)
        int[] monthlyCounts = new int[12];

        foreach (var item in items)
        {
            int monthIndex = item.Date.Month - 1; // 0~11
            if (monthIndex >= 0 && monthIndex < 12)
            {
                monthlyCounts[monthIndex]++;
            }
        }

        // 차트 초기화
        chart.ClearData();

        // X축 달 및 Y축 데이터 추가
        string[] monthLabels = { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월" };

        for (int i = 0; i < 12; i++)
        {
            chart.AddXAxisData(monthLabels[i]);
            chart.AddData(0, monthlyCounts[i]);
        }
    }
}