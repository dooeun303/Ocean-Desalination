using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class FaultPredictionScatterChart : MonoBehaviour, IPageRefreshable
{
    [Header("DB 연동 설정")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5432;
    [SerializeField] private string database = "postgres";
    [SerializeField] private string user = "postgres";
    [SerializeField] private string password = "password";
    [SerializeField] private string schema = "aaa";

    [Header("차트 및 테마 색상")]
    [SerializeField] private ScatterChart chart;
    [SerializeField] private Color normalColor = new Color32(0x50, 0xB8, 0xB8, 0xFF); // 청록색

    private PostgresAlarmService _service;

    public void OnPageRefresh()
    {
        if (_service == null)
        {
            _service = new PostgresAlarmService(host, port, database, user, password, schema);
        }
        _ = FetchAndDraw();
    }

    void Start()
    {
        if (chart == null)
            chart = GetComponentInChildren<ScatterChart>();

        OnPageRefresh();
    }

    private async System.Threading.Tasks.Task FetchAndDraw()
    {
        try
        {
            List<AlarmRecord> records = await _service.SearchAlarmsAsync("");
            DrawChart(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FaultPredictionScatterChart] DB 조회 실패: {e.Message}");
        }
    }

    private void DrawChart(List<AlarmRecord> records)
    {
        if (chart == null) return;
        if (records == null || records.Count == 0)
        {
            chart.ClearData();
            return;
        }

        chart.theme.colorPalette.Clear();
        chart.theme.colorPalette.Add(normalColor);

        chart.ClearData();
        foreach (var r in records)
        {
            float xHour = r.TriggeredAt.Hour + (r.TriggeredAt.Minute / 60f);
            float ySeverity = 1f; // 정상
            
            string sev = (r.Severity ?? "").ToLowerInvariant();
            if (sev == "critical") ySeverity = 3f;
            else if (sev == "warning") ySeverity = 2f;

            chart.AddData(0, xHour, ySeverity);
        }
    }
}
