using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

// =============================================
// 정비 유지관리 설비별 발생 건수 Bar Chart (PostgreSQL 직접 연동)
// =============================================
public class MaintenanceBarChart : MonoBehaviour, IPageRefreshable
{
    [Header("DB 접속 설정")]
    public string host = "127.0.0.1";
    public int port = 5433;
    public string database = "test";
    public string user = "postgres";
    public string password = "0000";
    public string schema = "aaa";

    [Header("차트")]
    public BarChart chart;

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
            chart = GetComponentInChildren<BarChart>();

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
            Debug.LogError($"[MaintenanceBarChart] DB 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<MaintenanceRecord> items)
    {
        if (chart == null) return;

        // 설비별 발생 건수 집계
        Dictionary<string, int> equipmentCounts = new Dictionary<string, int>();

        foreach (var item in items)
        {
            string name = string.IsNullOrEmpty(item.EquipmentName) ? "미분류" : item.EquipmentName;
            if (!equipmentCounts.ContainsKey(name))
                equipmentCounts[name] = 0;
            equipmentCounts[name]++;
        }

        // 차트 데이터 초기화
        chart.ClearData();

        // X축 및 Y축 데이터 추가
        foreach (var kvp in equipmentCounts)
        {
            chart.AddYAxisData(kvp.Key);   // 설비명 (Y축 카테고리 데이터)
            chart.AddData(0, kvp.Value);   // 건수
        }
    }
}