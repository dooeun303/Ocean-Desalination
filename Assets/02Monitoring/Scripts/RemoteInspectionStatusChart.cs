using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using XCharts.Runtime;

public class RemoteInspectionStatusChart : MonoBehaviour, IPageRefreshable
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

    private PostgresInspectionService _dbService;

    public void OnPageRefresh()
    {
        if (_dbService == null) _dbService = new PostgresInspectionService(host, port, database, user, password, schema);
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null) chart = GetComponentInChildren<PieChart>();
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
            Debug.LogError($"[RemoteInspectionStatusChart] DB 조회 실패: {e.Message}");
        }
    }

    void DrawChart(List<InspectionRecord> items)
    {
        if (chart == null) return;
        int pending = 0;
        int normal = 0;
        int issue = 0;

        foreach (var item in items)
        {
            string res = (item.Result ?? "").ToLowerInvariant();
            if (res == "pending" || res == "in_progress") pending++;
            else if (res == "normal") normal++;
            else if (res == "issue_found") issue++;
        }

        chart.ClearData();
        chart.AddData(0, normal, "정상");
        chart.AddData(0, issue, "이상발견");
        chart.AddData(0, pending, "대기/진행");
    }
}
