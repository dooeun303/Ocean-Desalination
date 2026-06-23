using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

// =============================================
// 정비 유지관리 작업 상태 Pie Chart (PostgreSQL 직접 연동)
// =============================================
public class MaintenanceStatusChart : MonoBehaviour, IPageRefreshable
{
    [Header("DB 접속 설정")]
    public string host = "127.0.0.1";
    public int port = 5433;
    public string database = "test";
    public string user = "postgres";
    public string password = "0000";
    public string schema = "aaa";

    [Header("차트")]
    public PieChart chart;

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
            chart = GetComponentInChildren<PieChart>();

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
            Debug.LogError($"[MaintenanceStatusChart] DB 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<MaintenanceRecord> items)
    {
        if (chart == null) return;

        int scheduled = 0;
        int inProgress = 0;
        int completed = 0;

        foreach (var item in items)
        {
            string status = (item.Status ?? "").ToLowerInvariant();
            if (status == "scheduled") scheduled++;
            else if (status == "in_progress") inProgress++;
            else if (status == "completed") completed++;
        }

        chart.ClearData();

        // 한글 라벨 대입
        chart.AddData(0, scheduled, "대기");
        chart.AddData(0, inProgress, "진행중");
        chart.AddData(0, completed, "완료");
    }
}