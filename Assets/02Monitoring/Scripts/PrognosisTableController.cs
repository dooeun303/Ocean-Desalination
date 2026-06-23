using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using TMPro;

public class PrognosisTableController : MonoBehaviour, IPageRefreshable
{
    public void OnPageRefresh()
    {
        RefreshUI();
    }

    [Header("Table")]
    public Transform rowContainer;
    public GameObject rowPrefab;

    [Header("Charts")]
    public BarChart barChart;
    public RadarChart radarChart;
    public PieChart donutChart;

    [Header("KPIs")]
    public TMP_Text[] kpiValues; // 0: Active, 1: Released, 2: Total, 3: Avg Prob
    public TMP_Text[] kpiTrends;
    public TMP_Text[] kpiSubTexts;

    private List<PrognosisData> _mockData = new List<PrognosisData>();

    void Start()
    {
        InitializeMockData();
        RefreshUI();
    }

    void InitializeMockData()
    {
        _mockData.Add(new PrognosisData { 
            id = "A-101", 
            timestamp = "2024-05-15 10:30", 
            equipmentName = "Belt Conveyor", 
            location = "Line 1", 
            content = "Bearing vibration abnormal", 
            level = "Warning", 
            status = "Active" 
        });
        _mockData.Add(new PrognosisData { 
            id = "A-102", 
            timestamp = "2024-05-15 11:20", 
            equipmentName = "Motor A", 
            location = "Line 2", 
            content = "Overheating detected", 
            level = "Critical", 
            status = "Active" 
        });
        _mockData.Add(new PrognosisData { 
            id = "A-103", 
            timestamp = "2024-05-15 09:15", 
            equipmentName = "Gearbox", 
            location = "Line 1", 
            content = "Oil level low", 
            level = "Normal", 
            status = "Resolved" 
        });
    }

    void RefreshUI()
    {
        // 1. Refresh Table
        foreach (Transform child in rowContainer)
        {
            if (child.gameObject.activeSelf) Destroy(child.gameObject);
        }

        foreach (var data in _mockData)
        {
            GameObject row = Instantiate(rowPrefab, rowContainer);
            row.SetActive(true);
            row.GetComponent<PrognosisRowUI>().SetData(data);
        }

        // 2. Setup Bar Chart
        if (barChart != null)
        {
            barChart.ClearData();
            barChart.AddXAxisData("Equip 1");
            barChart.AddXAxisData("Equip 2");
            barChart.AddXAxisData("Equip 3");
            barChart.AddXAxisData("Equip 4");
            barChart.AddXAxisData("Equip 5");
            barChart.AddData(0, 15);
            barChart.AddData(0, 25);
            barChart.AddData(0, 10);
            barChart.AddData(0, 30);
            barChart.AddData(0, 12);
        }

        // 3. Setup Radar Chart
        if (radarChart != null)
        {
            radarChart.ClearData();
            // Radar charts in XCharts require radar components to be set up in inspector usually, 
            // but we can try to add data if it has indices.
            radarChart.AddData(0, 80, 60, 40, 50, 70);
        }

        // 4. Setup Donut Chart
        if (donutChart != null)
        {
            donutChart.ClearData();
            donutChart.AddData(0, 40, "Type A");
            donutChart.AddData(0, 30, "Type B");
            donutChart.AddData(0, 20, "Type C");
            donutChart.AddData(0, 10, "Type D");
        }

        // 5. Update KPIs
        if (kpiValues.Length >= 4)
        {
            kpiValues[0].text = "2건";
            kpiValues[1].text = "128건";
            kpiValues[2].text = "130건";
            kpiValues[3].text = "15%";
        }
    }
}
