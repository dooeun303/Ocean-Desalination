using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class FaultPredictionPieChart : MonoBehaviour, IPageRefreshable
{
    [Header("DB 연동 설정")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5432;
    [SerializeField] private string database = "postgres";
    [SerializeField] private string user = "postgres";
    [SerializeField] private string password = "password";
    [SerializeField] private string schema = "aaa";

    [Header("차트 및 테마 색상")]
    [SerializeField] private PieChart chart;
    [SerializeField] private Color criticalColor = new Color32(0xEF, 0x44, 0x44, 0xFF); // 위험
    [SerializeField] private Color warningColor = new Color32(0xF5, 0x9E, 0x0B, 0xFF);  // 경고
    [SerializeField] private Color normalColor = new Color32(0x50, 0xB8, 0xB8, 0xFF);   // 정상

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
            chart = GetComponentInChildren<PieChart>();

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
            Debug.LogError($"[FaultPredictionPieChart] DB 조회 실패: {e.Message}");
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

        int critical = 0, warning = 0, normal = 0;
        foreach (var r in records)
        {
            string sev = (r.Severity ?? "").ToLowerInvariant();
            if (sev == "critical") critical++;
            else if (sev == "warning") warning++;
            else normal++;
        }

        chart.theme.colorPalette.Clear();
        chart.ClearData();
        
        if (critical > 0) { chart.AddData(0, critical, "위험"); chart.theme.colorPalette.Add(criticalColor); }
        if (warning > 0)  { chart.AddData(0, warning, "경고"); chart.theme.colorPalette.Add(warningColor); }
        if (normal > 0)   { chart.AddData(0, normal, "정상"); chart.theme.colorPalette.Add(normalColor); }
    }
}
