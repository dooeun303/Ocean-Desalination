using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

public class RemoteInspectionBarChart : MonoBehaviour, IPageRefreshable
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

    private PostgresInspectionService _dbService;

    public void OnPageRefresh()
    {
        if (_dbService == null) _dbService = new PostgresInspectionService(host, port, database, user, password, schema);
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
            if (_dbService == null) _dbService = new PostgresInspectionService(host, port, database, user, password, schema);
            List<InspectionRecord> records = await _dbService.SearchInspectionsAsync("");
            DrawChart(records);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RemoteInspectionBarChart] DB 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<InspectionRecord> items)
    {
        if (chart == null) return;
        Dictionary<string, int> inspectorCounts = new Dictionary<string, int>();
        foreach (var item in items)
        {
            string name = string.IsNullOrEmpty(item.InspectorName) ? "미지정" : item.InspectorName;
            if (!inspectorCounts.ContainsKey(name)) inspectorCounts[name] = 0;
            inspectorCounts[name]++;
        }
        chart.ClearData();
        foreach (var kvp in inspectorCounts)
        {
            chart.AddYAxisData(kvp.Key);
            chart.AddData(0, kvp.Value);
        }
    }
}
