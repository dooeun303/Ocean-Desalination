using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Npgsql;

public class EquipmentHealthKPI : MonoBehaviour, IPageRefreshable
{
    [Header("DB 접속 설정")]
    public string host = "127.0.0.1";
    public int port = 5433;
    public string database = "test";
    public string user = "postgres";
    public string password = "0000";
    public string schema = "aaa";

    [Header("KPI 카드 UI")]
    public KPICardUI cardOverall = new KPICardUI();
    public KPICardUI cardNormal = new KPICardUI();
    public KPICardUI cardWarning = new KPICardUI();
    public KPICardUI cardCritical = new KPICardUI();

    // Palette Colors (Soft Pastel Backgrounds & Dark Readable Matching Text)
    private readonly Color ColorPointBg = new Color32(230, 244, 234, 255);    // #E6F4EA
    private readonly Color ColorPointText = new Color32(19, 115, 51, 255);     // #137333
    
    private readonly Color ColorActiveBg = new Color32(224, 247, 250, 255);    // #E0F7FA
    private readonly Color ColorActiveText = new Color32(0, 131, 143, 255);    // #00838F
    
    private readonly Color ColorWarningBg = new Color32(254, 243, 199, 255);   // #FEF3C7
    private readonly Color ColorWarningText = new Color32(183, 121, 31, 255);  // #B7791F
    
    private readonly Color ColorCriticalBg = new Color32(252, 232, 230, 255);  // #FCE8E6
    private readonly Color ColorCriticalText = new Color32(197, 34, 31, 255);  // #C5221F

    private void Awake()
    {
        AutoBindUI();
    }

    private void OnEnable()
    {
        OnPageRefresh();
    }

    public void OnPageRefresh()
    {
        StartCoroutine(FetchAndUpdate());
    }

    private void AutoBindUI()
    {
        Transform row = transform.Find("Row");
        if (row == null) row = transform.Find("Row_KPI");
        if (row == null && transform.childCount > 1) row = transform.GetChild(1);

        if (row != null && row.childCount >= 4)
        {
            cardOverall.Setup(row.GetChild(0));
            cardNormal.Setup(row.GetChild(1));
            cardWarning.Setup(row.GetChild(2));
            cardCritical.Setup(row.GetChild(3));
        }
    }

    private string GetConnectionString()
    {
        string h = (host ?? "").ToLowerInvariant() == "localhost" ? "127.0.0.1" : host;
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = h,
            Port = port,
            Database = database,
            Username = user,
            Password = password,
            SearchPath = schema,
            SslMode = SslMode.Disable,
            Timeout = 15,
            CommandTimeout = 15,
            ServerCompatibilityMode = ServerCompatibilityMode.NoTypeLoading
        };
        return builder.ConnectionString;
    }

    private IEnumerator FetchAndUpdate()
    {
        int totalAlarms = 0;
        int activeCriticalAlarms = 0;
        int normalCount = 0;
        int warningCount = 0;
        int criticalCount = 0;
        int totalCount = 0;

        bool success = false;
        string errorMsg = "";

        var thread = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // 1. Get Alarms metrics
                    string alarmSql = "SELECT COUNT(*), COUNT(CASE WHEN alarm_severity = 'critical' AND alarm_is_active = true THEN 1 END) FROM alarm;";
                    using (var cmd = new NpgsqlCommand(alarmSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalAlarms = reader.GetInt32(0);
                            activeCriticalAlarms = reader.GetInt32(1);
                        }
                    }

                    // 2. Get Equipment status counts
                    string equipSql = "SELECT equipment_status, COUNT(*) FROM equipment GROUP BY equipment_status;";
                    using (var cmd = new NpgsqlCommand(equipSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string status = reader.IsDBNull(0) ? "normal" : reader.GetString(0).ToLowerInvariant();
                            int count = reader.GetInt32(1);
                            if (status == "normal") normalCount = count;
                            else if (status == "warning") warningCount = count;
                            else if (status == "critical") criticalCount = count;
                        }
                    }
                    totalCount = normalCount + warningCount + criticalCount;
                    if (totalCount == 0) totalCount = 10; // fallback safety
                    success = true;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
        });

        while (!thread.IsCompleted)
        {
            yield return null;
        }

        if (!success)
        {
            Debug.LogError($"[EquipmentHealthKPI] DB Error: {errorMsg}");
            yield break;
        }

        // Calculate dynamic plant health percentage:
        float plantHealth = 100f;
        if (totalAlarms > 0)
        {
            plantHealth = 100f - (activeCriticalAlarms * 0.1f);
            plantHealth = Mathf.Clamp(plantHealth, 95.0f, 100f);
        }

        // Apply visual updates
        cardOverall.UpdateUI(plantHealth.ToString("F1"), "%", "설비 가동 및 상태 안전", "▲ 0.1", plantHealth / 100f, ColorPointBg, ColorPointText);
        cardNormal.UpdateUI(normalCount.ToString(), "대", "정상 운영 중", "-", (float)normalCount / totalCount, ColorActiveBg, ColorActiveText);
        cardWarning.UpdateUI(warningCount.ToString(), "대", "정밀 점검 필요", "-", (float)warningCount / totalCount, ColorWarningBg, ColorWarningText);
        cardCritical.UpdateUI(criticalCount.ToString(), "대", "즉시 조치 필요", "-", (float)criticalCount / totalCount, ColorCriticalBg, ColorCriticalText);
    }
}