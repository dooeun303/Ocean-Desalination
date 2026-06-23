using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DumpUI
{
    public static void Run()
    {
        var popups = Object.FindObjectsOfType<MaintenanceRegisterPopup>(true);
        foreach (var popup in popups)
        {
            Debug.Log($"--- POPUP: {popup.gameObject.name} ---");
            DumpTrans(popup.transform, "");
        }
        EditorApplication.Exit(0);
    }
    
    static void DumpTrans(Transform t, string indent)
    {
        string comps = "";
        if (t.GetComponent<Image>()) comps += "Image ";
        if (t.GetComponent<TMP_Text>()) comps += "TMP_Text ";
        if (t.GetComponent<Button>()) comps += "Button ";
        if (t.GetComponent<TMP_InputField>()) comps += "TMP_InputField ";
        if (t.GetComponent<TMP_Dropdown>()) comps += "TMP_Dropdown ";
        
        Debug.Log($"{indent}- {t.name} [{comps}]");
        foreach (Transform child in t) DumpTrans(child, indent + "  ");
    }
}
