using UnityEngine;
using UnityEditor;
using XCharts.Runtime;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class AutoSetupFaultPrediction
{
    private static void CopyDBSettings(Object source, Object target)
    {
        var srcSO = new SerializedObject(source);
        var tgtSO = new SerializedObject(target);

        string[] props = { "host", "port", "database", "user", "password", "schema" };
        foreach (var p in props)
        {
            var srcProp = srcSO.FindProperty(p);
            var tgtProp = tgtSO.FindProperty(p);
            if (srcProp != null && tgtProp != null)
            {
                switch (srcProp.propertyType)
                {
                    case SerializedPropertyType.String:
                        tgtProp.stringValue = srcProp.stringValue;
                        break;
                    case SerializedPropertyType.Integer:
                        tgtProp.intValue = srcProp.intValue;
                        break;
                }
            }
        }
        tgtSO.ApplyModifiedProperties();
    }

    [MenuItem("Tools/고장예지 탭 자동 연결하기")]
    public static void Setup()
    {
        var controller = Object.FindObjectOfType<FaultPredictionTableController>(true);
        if (controller == null)
        {
            Debug.LogError("FaultPredictionTableController를 씬에서 찾을 수 없습니다.");
            return;
        }

        Transform root = controller.transform;

        // 1. BarChart
        var barChart = root.GetComponentInChildren<BarChart>(true);
        if (barChart != null)
        {
            var bc = barChart.GetComponent<FaultPredictionBarChart>();
            if (bc == null) bc = barChart.gameObject.AddComponent<FaultPredictionBarChart>();
            CopyDBSettings(controller, bc);
            EditorUtility.SetDirty(barChart.gameObject);
        }

        // 2. ScatterChart
        var scatterChart = root.GetComponentInChildren<ScatterChart>(true);
        if (scatterChart != null)
        {
            var sc = scatterChart.GetComponent<FaultPredictionScatterChart>();
            if (sc == null) sc = scatterChart.gameObject.AddComponent<FaultPredictionScatterChart>();
            CopyDBSettings(controller, sc);
            EditorUtility.SetDirty(scatterChart.gameObject);
        }

        // 3. PieChart
        var pieChart = root.GetComponentInChildren<PieChart>(true);
        if (pieChart != null)
        {
            var pc = pieChart.GetComponent<FaultPredictionPieChart>();
            if (pc == null) pc = pieChart.gameObject.AddComponent<FaultPredictionPieChart>();
            CopyDBSettings(controller, pc);
            EditorUtility.SetDirty(pieChart.gameObject);
        }

        // 4. KPIs
        var kpiScript = root.GetComponent<FaultPredictionKPIs>();
        if (kpiScript == null) 
            kpiScript = root.gameObject.AddComponent<FaultPredictionKPIs>();
        CopyDBSettings(controller, kpiScript);

        var so = new SerializedObject(kpiScript);
        var kpisProp = so.FindProperty("kpis");

        // 6-1 탭의 최상위 부모 찾기
        Transform tabRoot = controller.transform;
        while (tabRoot.parent != null && !tabRoot.name.Contains("6-1") && !tabRoot.name.Contains("Monitoring"))
        {
            tabRoot = tabRoot.parent;
        }

        // "KPI" 컨테이너 찾기
        var kpiRoots = tabRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "KPI" || t.name == "KPI (1)" || t.name == "KPI (2)" || t.name == "KPI (3)")
            .OrderBy(t => t.name)
            .ToList();

        if (kpiRoots.Count >= 4)
        {
            kpisProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var viewProp = kpisProp.GetArrayElementAtIndex(i);
                
                var texts = kpiRoots[i].GetComponentsInChildren<TMP_Text>(true);
                var images = kpiRoots[i].GetComponentsInChildren<Image>(true);

                // 값(Value): "0", "66.7", "130", "15" 거나 폰트 크기가 가장 큰 것
                var valText = texts.FirstOrDefault(t => t.text.Trim() == "0" || t.text.Trim() == "66.7" || t.text.Trim() == "130" || t.text.Trim() == "15") ?? texts.OrderByDescending(t => t.fontSize).FirstOrDefault();
                
                // 보조 텍스트(Sub): 보통 회색이거나 "대비", "누적", "전체" 등을 포함. 여기선 세 번째 텍스트로 추정
                var subText = texts.FirstOrDefault(t => t.text.Contains("대비") || t.text.Contains("누적") || t.text.Contains("전체")) ?? (texts.Length >= 4 ? texts[3] : null);
                
                // 상태바 텍스트(StatusBar): "상태", "안전", "주의", "발생" 포함되거나 가장 마지막 텍스트
                var statusText = texts.FirstOrDefault(t => t.text.Contains("안전") || t.text.Contains("주의") || t.text.Contains("발생")) ?? texts.LastOrDefault();
                
                // 상태바 배경(StatusBg): 상태바 텍스트의 부모에 있는 Image 컴포넌트
                Image statusBg = null;
                if (statusText != null && statusText.transform.parent != null)
                {
                    statusBg = statusText.transform.parent.GetComponent<Image>();
                }

                if (valText != null) viewProp.FindPropertyRelative("valueText").objectReferenceValue = valText;
                if (subText != null) viewProp.FindPropertyRelative("subText").objectReferenceValue = subText;
                if (statusText != null) viewProp.FindPropertyRelative("statusBarText").objectReferenceValue = statusText;
                if (statusBg != null) viewProp.FindPropertyRelative("statusBarBg").objectReferenceValue = statusBg;
            }
            so.ApplyModifiedProperties();
            Debug.Log("씬 계층구조를 분석하여 KPI 디테일(값, 보조텍스트, 상태바)을 모두 찾아 연결했습니다! (개별 그룹 방식)");
        }
        else if (kpiRoots.Count == 1)
        {
            // 단일 컨테이너 방식일 경우
            var allTexts = kpiRoots[0].GetComponentsInChildren<TMP_Text>(true);
            var allImages = kpiRoots[0].GetComponentsInChildren<Image>(true);
            
            // Value 텍스트 찾기 ("0", "66.7", "130", "15" 등)
            var valTexts = allTexts.Where(t => t.text.Trim() == "0" || t.text.Trim() == "66.7" || t.text.Trim() == "130" || t.text.Trim() == "15").ToList();
            if (valTexts.Count < 4) valTexts = allTexts.Where(t => t.fontSize >= 20).ToList();

            // Sub 텍스트 찾기 ("대비", "누적" 등)
            var subTexts = allTexts.Where(t => t.text.Contains("대비") || t.text.Contains("누적") || t.text.Contains("전체") || t.text.Contains("위험")).ToList();

            // Status 텍스트 찾기 ("안전", "주의" 등)
            var statusTexts = allTexts.Where(t => t.text.Contains("안전") || t.text.Contains("주의") || t.text.Contains("발생")).ToList();

            kpisProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var viewProp = kpisProp.GetArrayElementAtIndex(i);
                
                TMP_Text vText = i < valTexts.Count ? valTexts[i] : null;
                TMP_Text sText = i < subTexts.Count ? subTexts[i] : null;
                TMP_Text stText = i < statusTexts.Count ? statusTexts[i] : null;
                Image stBg = (stText != null && stText.transform.parent != null) ? stText.transform.parent.GetComponent<Image>() : null;

                if (vText != null) viewProp.FindPropertyRelative("valueText").objectReferenceValue = vText;
                if (sText != null) viewProp.FindPropertyRelative("subText").objectReferenceValue = sText;
                if (stText != null) viewProp.FindPropertyRelative("statusBarText").objectReferenceValue = stText;
                if (stBg != null) viewProp.FindPropertyRelative("statusBarBg").objectReferenceValue = stBg;
            }
            so.ApplyModifiedProperties();
            Debug.Log($"단일 KPI 컨테이너에서 세부 텍스트들을 찾아 연결했습니다!");
        }
        else
        {
            Debug.LogWarning($"KPI UI 그룹을 충분히 찾지 못했습니다 (찾은 개수: {kpiRoots.Count}). 수동 할당이 필요합니다.");
        }

        EditorUtility.SetDirty(root.gameObject);
        Debug.Log("=== 고장예지 탭 차트 및 KPI 자동 할당 완료! ===");
    }
}
