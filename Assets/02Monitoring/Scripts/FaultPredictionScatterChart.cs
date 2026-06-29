using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class FaultPredictionScatterChart : MonoBehaviour, IPageRefreshable
{
    [Header("차트 및 테마 색상")]
    [SerializeField] private ScatterChart chart;
    [SerializeField] private Color normalColor = new Color32(0x50, 0xB8, 0xB8, 0xFF); // 청록색

    public void OnPageRefresh()
    {
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
            List<AlarmRecord> records = await MonitoringMockDataStore.SearchAlarms("");
            DrawChart(records);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FaultPredictionScatterChart] Mock 조회 실패: {e.Message}");
        }
    }

    private void DrawChart(List<AlarmRecord> records)
    {
        if (chart == null) return;

        chart.theme.colorPalette.Clear();
        chart.theme.colorPalette.Add(normalColor);

        chart.ClearData();
        
        // 임의의 산점도 데이터 채우기 (X축: 시간, Y축: 심각도)
        float[,] dummyPoints = {
            { 2.5f, 1f }, { 4.2f, 2f }, { 6.8f, 1f }, { 8.5f, 3f }, { 10.1f, 1f },
            { 11.5f, 2f }, { 13.0f, 1f }, { 14.5f, 3f }, { 16.2f, 2f }, { 18.0f, 1f },
            { 19.8f, 1f }, { 21.5f, 2f }, { 23.0f, 1f }
        };
        
        for (int i = 0; i < dummyPoints.GetLength(0); i++)
        {
            chart.AddData(0, dummyPoints[i, 0], dummyPoints[i, 1]);
        }
    }
}
