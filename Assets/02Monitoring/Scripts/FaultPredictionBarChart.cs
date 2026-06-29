using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class FaultPredictionBarChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트 및 테마 색상")]
    [SerializeField] private BarChart chart;
    [SerializeField] private Color normalColor = new Color32(0x50, 0xB8, 0xB8, 0xFF); // 청록색

    public void OnPageRefresh()
    {
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
            List<AlarmRecord> records = await MonitoringMockDataStore.SearchAlarms("");
            DrawChart(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FaultPredictionBarChart] Mock 조회 실패: {e.Message}");
        }
    }

    private void DrawChart(List<AlarmRecord> records)
    {
        if (chart == null) return;

        // 메인 테마색 지정
        chart.theme.colorPalette.Clear();
        chart.theme.colorPalette.Add(normalColor);

        chart.ClearData();
        
        // 임의의 정적 데이터 채우기
        chart.AddYAxisData("스마트 크레인 A");
        chart.AddData(0, 12);
        chart.AddYAxisData("스마트 컨베이어 B");
        chart.AddData(0, 8);
        chart.AddYAxisData("고속 모터 C");
        chart.AddData(0, 15);
        chart.AddYAxisData("스마트 펌프 D");
        chart.AddData(0, 5);
        chart.AddYAxisData("자동화 로봇 E");
        chart.AddData(0, 9);
    }
}
