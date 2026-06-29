using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class FaultPredictionPieChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트 및 테마 색상")]
    [SerializeField] private PieChart chart;
    [SerializeField] private Color criticalColor = new Color32(0xEF, 0x44, 0x44, 0xFF); // 위험
    [SerializeField] private Color warningColor = new Color32(0xF5, 0x9E, 0x0B, 0xFF);  // 경고
    [SerializeField] private Color normalColor = new Color32(0x50, 0xB8, 0xB8, 0xFF);   // 정상

    public void OnPageRefresh()
    {
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
            List<AlarmRecord> records = await MonitoringMockDataStore.SearchAlarms("");
            DrawChart(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FaultPredictionPieChart] Mock 조회 실패: {e.Message}");
        }
    }

    private void DrawChart(List<AlarmRecord> records)
    {
        if (chart == null) return;

        chart.theme.colorPalette.Clear();
        chart.ClearData();
        
        // 임의의 상태 데이터 채우기
        chart.AddData(0, 3, "위험"); chart.theme.colorPalette.Add(criticalColor);
        chart.AddData(0, 7, "경고"); chart.theme.colorPalette.Add(warningColor);
        chart.AddData(0, 18, "정상"); chart.theme.colorPalette.Add(normalColor);
    }
}
