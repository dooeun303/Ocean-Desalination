using UnityEditor;
using UnityEngine;
using Npgsql;
using System.Text;
using System.Collections.Generic;

public class CheckDBSchema
{
    public static void RunCheck()
    {
        Debug.Log("--- DB SCHEMA CHECK ---");
        string connString = "Host=127.0.0.1;Port=5433;Database=test;Username=postgres;Password=0000;SslMode=Disable;";
        
        string[] tables = new string[] { "equipment", "technician", "manual" };
        
        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                foreach (var tbl in tables)
                {
                    string sql = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = 'aaa' AND table_name = '{tbl}' ORDER BY ordinal_position;";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"TABLE: aaa.{tbl}");
                        while (reader.Read())
                        {
                            sb.AppendLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
                        }
                        Debug.Log(sb.ToString());
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("DB Error: " + e.Message);
        }
        
        Debug.Log("--- DB SCHEMA CHECK COMPLETE ---");
        EditorApplication.Exit(0);
    }
}
