using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

[InitializeOnLoad]
public class AutoSetupMaintenanceKPI
{
    static AutoSetupMaintenanceKPI()
    {
        EditorApplication.delayCall += RunSetup;
    }

    static void RunSetup()
    {
        if (SessionState.GetBool("MaintenanceKPISetupDone3", false)) return;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene == null || !scene.name.Contains("Monitoring2")) return;

        MaintenanceTableController tableCtrl = Object.FindObjectOfType<MaintenanceTableController>(true);
        if (tableCtrl == null) return;

        Transform tabTransform = tableCtrl.transform;
        
        Transform kpiManager = null;
        foreach (Transform child in tabTransform)
        {
            if (child.name.Contains("KPI") || child.name.Contains("Top") || child.childCount >= 3)
            {
                int cardCount = 0;
                foreach(Transform c in child) {
                    if (c.GetComponentsInChildren<TMP_Text>(true).Length > 0) cardCount++;
                }
                if (cardCount >= 3)
                {
                    kpiManager = child;
                    break;
                }
            }
        }

        if (kpiManager == null) 
        {
            foreach (Transform child in tabTransform.GetComponentsInChildren<Transform>(true))
            {
                if (child.childCount >= 3 && child.GetChild(0).GetComponentInChildren<TMP_Text>(true) != null)
                {
                    kpiManager = child;
                    break;
                }
            }
        }

        if (kpiManager == null) return;

        MaintenanceKPI kpi = kpiManager.gameObject.GetComponent<MaintenanceKPI>();
        if (kpi == null) 
        {
            kpi = kpiManager.gameObject.AddComponent<MaintenanceKPI>();
            Debug.Log("[AutoSetup] Added MaintenanceKPI to " + kpiManager.name);
        }

        EditorUtility.SetDirty(kpi);
        EditorSceneManager.MarkSceneDirty(scene);
        SessionState.SetBool("MaintenanceKPISetupDone3", true);
        Debug.Log("[AutoSetup] Maintenance KPI setup complete (Fixed Sibling Index).");
    }
}
