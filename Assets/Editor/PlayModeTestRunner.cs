using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            if (state == "WaitingForCompile")
            {
                EditorApplication.delayCall += () => {
                    SessionState.SetString(StateKey, "EnteringPlayMode");
                    EditorApplication.isPlaying = true;
                };
            }
            else if (state == "EnteringPlayMode" && EditorApplication.isPlaying)
            {
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += RunOnce;
            }
            else if (state == "InPlayMode" && EditorApplication.isPlaying)
            {
                EditorApplication.update += RunOnce;
            }
        }

        private static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            string res = RunTestLogic();
            SessionState.SetString(ResultKey, res);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static string RunTestLogic()
        {
            string connStr = "Host=127.0.0.1;Port=5433;Database=test;Username=postgres;Password=0000;SearchPath=ttt;SslMode=Disable;ServerCompatibilityMode=NoTypeLoading";
            var sb = new StringBuilder();
            try
            {
                using (var conn = new Npgsql.NpgsqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT column_name, is_nullable FROM information_schema.columns WHERE table_schema = 'ttt' AND table_name = 'remote_inspection';";
                    using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        sb.AppendLine("Columns in ttt.remote_inspection:");
                        while (reader.Read())
                        {
                            sb.AppendLine($"{reader.GetString(0)} (Nullable: {reader.GetString(1)})");
                        }
                    }
                }
                return sb.ToString();
            }
            catch (System.Exception e) { return "Error: " + e.Message; }
        }
    }
}