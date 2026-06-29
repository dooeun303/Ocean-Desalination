using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(MenuButtonGroup))]
public class MenuButtonGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        MenuButtonGroup group = (MenuButtonGroup)target;

        if (group.pages == null || group.pages.Length == 0)
        {
            EditorGUILayout.HelpBox("할당된 페이지가 없습니다. MenuButtonGroup에 페이지들을 추가해주세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🖥️ 에디터 모드 페이지 스위처 (Edit Mode View)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("플레이 모드로 들어가지 않고도, 아래 버튼을 눌러 각 페이지의 화면을 에디터상에서 즉시 확인하고 편집할 수 있습니다.", MessageType.Info);

        EditorGUILayout.BeginVertical("box");

        for (int i = 0; i < group.pages.Length; i++)
        {
            GameObject page = group.pages[i];
            if (page == null) continue;

            // Check if this page is currently active and fully visible (alpha = 1)
            CanvasGroup cg = page.GetComponent<CanvasGroup>();
            bool isCurrent = page.activeSelf && (cg == null || cg.alpha > 0.9f);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(10, 10, 8, 8);
            
            if (isCurrent)
            {
                // Highlight current active page button
                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.7f, 1f);
                buttonStyle.fontStyle = FontStyle.Bold;
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }

            string btnText = $"{i + 1}. {page.name}";
            if (isCurrent) btnText += " (표시 중)";

            if (GUILayout.Button(btnText, buttonStyle))
            {
                SwitchPageInEditor(group, i);
            }
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    private void SwitchPageInEditor(MenuButtonGroup group, int targetIndex)
    {
        // Register undo for all involved objects
        Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Editor Page Switch");

        for (int i = 0; i < group.pages.Length; i++)
        {
            GameObject page = group.pages[i];
            if (page == null) continue;

            Undo.RecordObject(page, "Toggle Page Active");
            CanvasGroup cg = page.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = page.AddComponent<CanvasGroup>();
                Undo.RegisterCreatedObjectUndo(cg, "Add CanvasGroup");
            }
            Undo.RecordObject(cg, "Modify CanvasGroup");

            if (i == targetIndex)
            {
                page.SetActive(true);
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                page.SetActive(false);
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            EditorUtility.SetDirty(page);
            EditorUtility.SetDirty(cg);
        }

        // Handle sidebar contents if available
        if (group.sidebarContents != null)
        {
            for (int i = 0; i < group.sidebarContents.Length; i++)
            {
                GameObject sidebar = group.sidebarContents[i];
                if (sidebar == null) continue;

                Undo.RecordObject(sidebar, "Toggle Sidebar Active");
                CanvasGroup cg = sidebar.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = sidebar.AddComponent<CanvasGroup>();
                    Undo.RegisterCreatedObjectUndo(cg, "Add CanvasGroup");
                }
                Undo.RecordObject(cg, "Modify CanvasGroup");

                if (i == targetIndex)
                {
                    sidebar.SetActive(true);
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                else
                {
                    sidebar.SetActive(false);
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }

                EditorUtility.SetDirty(sidebar);
                EditorUtility.SetDirty(cg);
            }
        }

        // Handle button highlights in Editor Mode
        if (group.buttons != null)
        {
            for (int i = 0; i < group.buttons.Length; i++)
            {
                MenuButton btn = group.buttons[i];
                if (btn == null) continue;

                Undo.RecordObject(btn, "Modify Button Highlight State");

                // We simulate SetSelected in edit mode safely
                if (btn.backgroundImage != null)
                {
                    Undo.RecordObject(btn.backgroundImage, "Modify Button Background");
                    if (i == targetIndex)
                    {
                        if (btn.selectedBg != null) btn.backgroundImage.sprite = btn.selectedBg;
                    }
                    else
                    {
                        if (btn.normalBg != null) btn.backgroundImage.sprite = btn.normalBg;
                    }
                    EditorUtility.SetDirty(btn.backgroundImage);
                }

                if (btn.iconImage != null)
                {
                    Undo.RecordObject(btn.iconImage, "Modify Button Icon Color");
                    btn.iconImage.color = (i == targetIndex) ? btn.selectedIconColor : btn.normalIconColor;
                    EditorUtility.SetDirty(btn.iconImage);
                }

                EditorUtility.SetDirty(btn);
            }
        }

        // Force a scene and hierarchy repaint
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}
