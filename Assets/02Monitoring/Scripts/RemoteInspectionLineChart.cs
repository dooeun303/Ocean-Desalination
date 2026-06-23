using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

public class RemoteInspectionLineChart : MonoBehaviour, IPageRefreshable
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

    private PostgresInspectionService _dbService;

    public void OnPageRefresh()
    {
        if (_dbService == null) _dbService = new PostgresInspectionService(host, port, database, user, password, schema);
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
            if (_dbService == null) _dbService = new PostgresInspectionService(host, port, database, user, password, schema);
            List<InspectionRecord> records = await _dbService.SearchInspectionsAsync("");
            DrawChart(records);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RemoteInspectionLineChart] DB 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<InspectionRecord> items)
    {
        if (chart == null) return;
        int[] monthlyCounts = new int[12];
        foreach (var item in items)
        {
            int monthIndex = item.InspectionDate.Month - 1;
            if (monthIndex >= 0 && monthIndex < 12) monthlyCounts[monthIndex]++;
        }
        chart.ClearData();
        string[] monthLabels = { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월" };
        for (int i = 0; i < 12; i++)
        {
            chart.AddXAxisData(monthLabels[i]);
            chart.AddData(0, monthlyCounts[i]);
        }
    }
}
