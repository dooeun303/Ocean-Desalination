using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Quest용 APK 커맨드라인 빌드 (에디터 전용). 빌드 설정에서 켜진 씬(MR 2)만 Builds/mr.apk로 만든다.
//   Unity.exe -batchmode -quit -projectPath "<MR 프로젝트>" -buildTarget Android -executeMethod MrCommandLineBuild.BuildAndroid -logFile <로그>
public static class MrCommandLineBuild
{
    public static void BuildAndroid()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/mr.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log($"[MrCommandLineBuild] {report.summary.result} - {string.Join(", ", scenes)} → Builds/mr.apk ({report.summary.totalSize / (1024 * 1024)}MB, 오류 {report.summary.totalErrors})");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
