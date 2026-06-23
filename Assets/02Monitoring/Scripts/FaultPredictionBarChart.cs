using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class FaultPredictionBarChart : MonoBehaviour, IPageRefreshable
{
    [Header("DB 연동 설정")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5432;
    [SerializeField] private string database = "postgres";
    [SerializeField] private string user = "postgres";
    [SerializeField] private string password = "password";
    [SerializeField] private string schema = "aaa";

    [Header("차트 및 테마 색상")]
    [SerializeField] private BarChart chart;
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
            chart = GetComponentInChildren<BarChart>();

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
            Debug.LogError($"[FaultPredictionBarChart] DB 조회 실패: {e.Message}");
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

        var equipCounts = new Dictionary<string, int>();
        foreach (var r in records)
        {
            string eq = string.IsNullOrEmpty(r.EquipmentName) ? "알 수 없음" : r.EquipmentName;
            if (!equipCounts.ContainsKey(eq)) equipCounts[eq] = 0;
            equipCounts[eq]++;
        }

        var sortedEquip = equipCounts.OrderByDescending(kv => kv.Value).Take(5).ToList();

        // 메인 테마색 지정
        chart.theme.colorPalette.Clear();
        chart.theme.colorPalette.Add(normalColor);

        chart.ClearData();
        foreach (var kv in sortedEquip)
        {
            chart.AddYAxisData(kv.Key);
            chart.AddData(0, kv.Value);
        }
    }
}
