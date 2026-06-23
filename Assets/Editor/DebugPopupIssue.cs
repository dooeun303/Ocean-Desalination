using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class DebugPopupIssue
{
    public static void RunDebug()
    {
        Debug.Log("--- DEBUGGING POPUP ISSUE ---");
        var scene = EditorSceneManager.OpenScene("Assets/01Scenes/Monitoring2.unity");

        var controllers = Object.FindObjectsOfType<MaintenanceTableController>(true);
        if (controllers.Length == 0)
        {
            Debug.LogError("No MaintenanceTableController found in scene!");
            return;
        }

        foreach (var c in controllers)
        {
            Debug.Log($"Found MaintenanceTableController on '{c.name}' (Active: {c.gameObject.activeInHierarchy})");
            
            var type = c.GetType();
            
            var registerOpenBtnField = type.GetField("registerOpenButton", BindingFlags.NonPublic | BindingFlags.Instance);
            var registerOpenBtn = registerOpenBtnField?.GetValue(c) as Button;
            Debug.Log($"registerOpenButton is: {(registerOpenBtn != null ? registerOpenBtn.name : "NULL")}");

            var registerPopupField = type.GetField("registerPopup", BindingFlags.NonPublic | BindingFlags.Instance);
            var registerPopup = registerPopupField?.GetValue(c) as MaintenanceRegisterPopup;
            Debug.Log($"registerPopup is: {(registerPopup != null ? registerPopup.name : "NULL")}");

            if (registerPopup != null)
            {
                Debug.Log($"MaintenanceRegisterPopup GameObject activeInHierarchy: {registerPopup.gameObject.activeInHierarchy}");
                Debug.Log($"MaintenanceRegisterPopup GameObject activeSelf: {registerPopup.gameObject.activeSelf}");
                
                var popupPanelField = registerPopup.GetType().GetField("popupPanel", BindingFlags.Public | BindingFlags.Instance);
                var popupPanel = popupPanelField?.GetValue(registerPopup) as GameObject;
                Debug.Log($"MaintenanceRegisterPopup.popupPanel is: {(popupPanel != null ? popupPanel.name : "NULL")}");
                if (popupPanel != null)
                {
                    Debug.Log($"popupPanel activeSelf: {popupPanel.activeSelf}");
                }
            }
        }
        Debug.Log("--- DEBUG COMPLETE ---");
        EditorApplication.Exit(0);
    }
}
