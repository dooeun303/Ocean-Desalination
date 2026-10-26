using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// 실기기 테스트 보조 (에디터 전용 - 빌드에 안 들어감).
// MR을 에디터 Play로 돌리면서 VR 패널을 손으로 누르지 않고도, 바깥(스크립트/Claude)에서 같은 버튼 동작을
// 실행하게 해준다. Play 중에 프로젝트의 Temp/mr_test_cmd.txt 에 명령 한 줄을 쓰면 0.5초 안에 읽어서 실행하고
// 파일을 지운다. 결과는 Console([MrTestBridge])과 Temp/mr_test_result.txt 에 남는다.
//
// 명령:
//   accept_latest      - 가장 최근 대기(ringing) 지원요청을 수락 (원격지원 목록의 "수락" 버튼과 같은 경로)
//   accept <id>        - 지정한 지원요청 수락
//   reject_latest      - 가장 최근 대기 지원요청 거절
//   close              - 화상통화 도크의 닫기(✕) 버튼 (MR이 통화 종료)
//   status             - 통화 중 여부/현재 통화 id
//
// 실제 버튼과 같은 private 메서드(SupportCallList.OnAcceptClicked 등)를 리플렉션으로 부른다 - 그래서 버튼을
// 눌렀을 때와 같은 코드 경로를 탄다(통화 중 수락 차단, 텔레포트, 도크 표시 등 포함).
[InitializeOnLoad]
public static class MrTestBridge
{
    // 커맨드라인에서 에디터를 열자마자 MR 2 씬으로 Play에 들어간다 - 사람이 Play를 누르지 않아도 브리지 명령을 쓸 수 있게.
    //   Unity.exe -projectPath "<MR 프로젝트>" -executeMethod MrTestBridge.PlayMr2
    public static void PlayMr2()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01Scenes/MR 2.unity");
        EditorApplication.isPlaying = true;
    }

    static readonly string CmdPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "mr_test_cmd.txt");
    static readonly string ResultPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "mr_test_result.txt");
    static double _nextPoll;

    static MrTestBridge()
    {
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (!Application.isPlaying || EditorApplication.timeSinceStartup < _nextPoll) return;
        _nextPoll = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(CmdPath)) return;

        string cmd;
        try { cmd = File.ReadAllText(CmdPath).Trim(); File.Delete(CmdPath); }
        catch (IOException) { return; } // 쓰는 중 - 다음 폴링에

        try { Report(cmd, Run(cmd)); }
        catch (Exception e) { Report(cmd, "실패: " + e.Message); Debug.LogException(e); }
    }

    static string Run(string cmd)
    {
        var parts = cmd.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        string verb = parts.Length > 0 ? parts[0] : "";
        string arg = parts.Length > 1 ? parts[1].Trim() : null;

        switch (verb)
        {
            case "accept_latest": return InvokeOnList("OnAcceptClicked", FindRinging(null));
            case "accept": return InvokeOnList("OnAcceptClicked", FindRinging(arg));
            case "reject_latest": return InvokeOnList("OnRejectClicked", FindRinging(null));
            case "close":
            {
                var dock = UnityEngine.Object.FindFirstObjectByType<MrCallDockPanel>(FindObjectsInactive.Include);
                if (dock == null) return "MrCallDockPanel 없음";
                Call(dock, "OnCloseClicked");
                return "도크 닫기 실행";
            }
            case "status":
                return $"IsInCall={MrCallDockPanel.IsInCall} CurrentSupportCallId={VideoCallSignalingMR.CurrentSupportCallId ?? "-"}";
            default:
                return "알 수 없는 명령";
        }
    }

    // 서버에서 대기(ringing) 요청을 조회해 최신 것(또는 id 일치) 하나를 고른다.
    static SupportCallData FindRinging(string id)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        string json = http.GetStringAsync(ServerConfig.BaseUrl + "/api/support-calls?status=ringing").Result;
        var list = JsonConvert.DeserializeObject<ApiResponse<List<SupportCallData>>>(json)?.data;
        if (list == null || list.Count == 0) throw new Exception("대기 중인 지원요청이 없음");
        var pick = id == null ? list.First() : list.FirstOrDefault(d => d.id == id);
        if (pick == null) throw new Exception("해당 id의 대기 요청 없음: " + id);
        return pick;
    }

    static string InvokeOnList(string method, SupportCallData data)
    {
        var list = UnityEngine.Object.FindFirstObjectByType<SupportCallList>(FindObjectsInactive.Include);
        if (list == null) return "SupportCallList 없음";
        // 수락/거절은 이 컴포넌트에서 코루틴을 돌려서, 목록 패널이 꺼져 있으면 켠다(= 메뉴에서 원격지원 목록을 연 것과 같음).
        if (!list.gameObject.activeInHierarchy)
        {
            for (var t = list.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
        }
        Call(list, method, data);
        return $"{method} 실행: {data.id} ({data.equipment_name ?? "설비 미지정"})";
    }

    static void Call(object target, string method, params object[] args)
    {
        var m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m == null) throw new Exception(target.GetType().Name + "." + method + " 없음");
        m.Invoke(target, args);
    }

    static void Report(string cmd, string result)
    {
        string line = $"{DateTime.Now:HH:mm:ss} {cmd} → {result}";
        Debug.Log("[MrTestBridge] " + line);
        try { File.AppendAllText(ResultPath, line + Environment.NewLine); } catch (IOException) { }
    }
}
