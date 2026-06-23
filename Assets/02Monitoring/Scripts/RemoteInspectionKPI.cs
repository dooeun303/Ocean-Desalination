using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Npgsql;

public class RemoteInspectionKPI : MonoBehaviour, IPageRefreshable
{
    [Header("DB 접속 설정")]
    public string host = "127.0.0.1";
    public int port = 5433;
    public string database = "test";
    public string user = "postgres";
    public string password = "0000";
    public string schema = "aaa";

    [Header("KPI 카드 UI")]
    public KPICardUI cardTotal = new KPICardUI();
    public KPICardUI cardInProgress = new KPICardUI();
    public KPICardUI cardCompleted = new KPICardUI();
    public KPICardUI cardAnomalyRate = new KPICardUI();

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
            cardTotal.Setup(row.GetChild(0));
            cardInProgress.Setup(row.GetChild(1));
            cardCompleted.Setup(row.GetChild(2));
            cardAnomalyRate.Setup(row.GetChild(3));
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
        int activeYear = 2026;
        int activeMonth = 4;
        
        int totalThisMonth = 0;
        int totalLastMonth = 0;
        int pendingThisMonth = 0;
        int completedThisMonth = 0;
        int issueFoundThisMonth = 0;

        bool success = false;
        string errorMsg = "";

        var thread = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    // 1. Get the month with maximum inspections
                    string activeMonthSql = @"
SELECT EXTRACT(YEAR FROM inspection_scheduled_at) as yr, EXTRACT(MONTH FROM inspection_scheduled_at) as mon, COUNT(*) as cnt
FROM inspection
GROUP BY yr, mon
ORDER BY cnt DESC
LIMIT 1;";
                    using (var cmd = new NpgsqlCommand(activeMonthSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            activeYear = (int)reader.GetDouble(0);
                            activeMonth = (int)reader.GetDouble(1);
                        }
                    }

                    // Calculate previous month dynamically
                    DateTime targetDate = new DateTime(activeYear, activeMonth, 1);
                    DateTime prevDate = targetDate.AddMonths(-1);
                    int lastYear = prevDate.Year;
                    int lastMonth = prevDate.Month;

                    // 2. Get counts for active month and last month
                    string countsSql = @"
SELECT COUNT(CASE WHEN EXTRACT(YEAR FROM inspection_scheduled_at) = @activeYear AND EXTRACT(MONTH FROM inspection_scheduled_at) = @activeMonth THEN 1 END) as total_this,
       COUNT(CASE WHEN EXTRACT(YEAR FROM inspection_scheduled_at) = @lastYear AND EXTRACT(MONTH FROM inspection_scheduled_at) = @lastMonth THEN 1 END) as total_last,
       COUNT(CASE WHEN EXTRACT(YEAR FROM inspection_scheduled_at) = @activeYear AND EXTRACT(MONTH FROM inspection_scheduled_at) = @activeMonth AND inspection_result = 'pending' THEN 1 END) as pending_this,
       COUNT(CASE WHEN EXTRACT(YEAR FROM inspection_scheduled_at) = @activeYear AND EXTRACT(MONTH FROM inspection_scheduled_at) = @activeMonth AND (inspection_result = 'normal' OR inspection_result = 'issue_found') THEN 1 END) as completed_this,
       COUNT(CASE WHEN EXTRACT(YEAR FROM inspection_scheduled_at) = @activeYear AND EXTRACT(MONTH FROM inspection_scheduled_at) = @activeMonth AND inspection_result = 'issue_found' THEN 1 END) as issue_this
FROM inspection;";

                    using (var cmd = new NpgsqlCommand(countsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("activeYear", activeYear);
                        cmd.Parameters.AddWithValue("activeMonth", activeMonth);
                        cmd.Parameters.AddWithValue("lastYear", lastYear);
                        cmd.Parameters.AddWithValue("lastMonth", lastMonth);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                totalThisMonth = reader.GetInt32(0);
                                totalLastMonth = reader.GetInt32(1);
                                pendingThisMonth = reader.GetInt32(2);
                                completedThisMonth = reader.GetInt32(3);
                                issueFoundThisMonth = reader.GetInt32(4);
                            }
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
            Debug.LogError($"[RemoteInspectionKPI] DB Error: {errorMsg}");
            yield break;
        }

        int diff = totalThisMonth - totalLastMonth;
        string diffSign = diff >= 0 ? "+" : "";
        string compareText = $"전월 대비 {diffSign}{diff}건";

        float anomalyRate = completedThisMonth > 0 ? ((float)issueFoundThisMonth / completedThisMonth) * 100f : 0f;

        float totalRatio = totalLastMonth > 0 ? (float)totalThisMonth / totalLastMonth : (totalThisMonth > 0 ? 1f : 0f);
        float progressRatio = totalThisMonth > 0 ? (float)pendingThisMonth / totalThisMonth : 0f;
        float completedRatio = totalThisMonth > 0 ? (float)completedThisMonth / totalThisMonth : 0f;

        cardTotal.UpdateUI(totalThisMonth.ToString(), "건", $"{activeMonth}월 전체 점검", compareText, totalRatio, ColorPointBg, ColorPointText);
        cardInProgress.UpdateUI(pendingThisMonth.ToString(), "건", "대기/진행 중", "-", progressRatio, ColorWarningBg, ColorWarningText);
        cardCompleted.UpdateUI(completedThisMonth.ToString(), "건", "완료된 점검", "-", completedRatio, ColorActiveBg, ColorActiveText);
        cardAnomalyRate.UpdateUI(anomalyRate.ToString("F1"), "%", "이상 발견율", "▲ 12.5", anomalyRate / 100f, ColorCriticalBg, ColorCriticalText);
    }
}