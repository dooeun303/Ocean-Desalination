using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Npgsql;

public class FaultPredictionKPIs : MonoBehaviour, IPageRefreshable
{
    [Header("DB 접속 설정")]
    public string host = "127.0.0.1";
    public int port = 5433;
    public string database = "test";
    public string user = "postgres";
    public string password = "0000";
    public string schema = "aaa";

    [Header("KPI 카드 UI")]
    public KPICardUI cardActive = new KPICardUI();
    public KPICardUI cardReleased = new KPICardUI();
    public KPICardUI cardTotal = new KPICardUI();

    // Palette Colors (Soft Pastel Backgrounds & Dark Readable Matching Text)
    private readonly Color ColorPointBg = new Color32(230, 244, 234, 255);    // #E6F4EA
    private readonly Color ColorPointText = new Color32(19, 115, 51, 255);     // #137333

    private readonly Color ColorCriticalBg = new Color32(252, 232, 230, 255);  // #FCE8E6
    private readonly Color ColorCriticalText = new Color32(197, 34, 31, 255);  // #C5221F

    private readonly Color ColorInfoBg = new Color32(232, 240, 254, 255);      // #E8F0FE
    private readonly Color ColorInfoText = new Color32(26, 115, 232, 255);     // #1A73E8

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

        if (row != null && row.childCount >= 3)
        {
            cardActive.Setup(row.GetChild(0));
            cardReleased.Setup(row.GetChild(1));
            cardTotal.Setup(row.GetChild(2));
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
        int activeCount = 0;
        int criticalCount = 0;
        int releasedCount = 0;
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

                    string sql = @"
SELECT COUNT(CASE WHEN alarm_is_active = true THEN 1 END),
       COUNT(CASE WHEN alarm_is_active = true AND alarm_severity = 'critical' THEN 1 END),
       COUNT(CASE WHEN alarm_is_active = false THEN 1 END),
       COUNT(*)
FROM alarm;";
                    
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            activeCount = reader.GetInt32(0);
                            criticalCount = reader.GetInt32(1);
                            releasedCount = reader.GetInt32(2);
                            totalCount = reader.GetInt32(3);
                        }
                    }
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
            Debug.LogError($"[FaultPredictionKPIs] DB Error: {errorMsg}");
            yield break;
        }

        float activeRatio = totalCount > 0 ? (float)activeCount / totalCount : 0f;
        float releasedRatio = totalCount > 0 ? (float)releasedCount / totalCount : 0f;

        cardActive.UpdateUI(activeCount.ToString(), "건", "미해결 고장예지 알람", $"▲ {criticalCount}", activeRatio, ColorCriticalBg, ColorCriticalText);
        cardReleased.UpdateUI(releasedCount.ToString(), "건", "조치 완료 및 자동 해제", "-", releasedRatio, ColorPointBg, ColorPointText);
        cardTotal.UpdateUI(totalCount.ToString(), "건", "누적 고장예지 알람", "-", 1.0f, ColorInfoBg, ColorInfoText);
    }
}
