#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AddModernAnimToAllButtons : EditorWindow
{
    [MenuItem("Tools/모든 버튼에 쫀득한 애니메이션 일괄 적용")]
    public static void ApplyToAll()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        int addedCount = 0;
        int removedCount = 0;

        foreach (var btn in allButtons)
        {
            // 1. 제외 조건: 서브탭 메뉴
            if (btn.GetComponent<SubMenuItem>() != null) continue;
            
            // 2. 제외 조건: 상단바 버튼 (부모 이름에 Top, 상단, Header 등이 포함된 경우)
            bool isTopBar = false;
            Transform p = btn.transform.parent;
            while (p != null)
            {
                string pName = p.name.ToLower();
                if (pName.Contains("top") || pName.Contains("상단") || pName.Contains("header"))
                {
                    isTopBar = true;
                    break;
                }
                p = p.parent;
            }

            if (isTopBar)
            {
                // 상단바에 이미 스크립트가 들어갔다면 제거해 줍니다.
                var anim = btn.GetComponent<ModernButtonAnim>();
                if (anim != null)
                {
                    DestroyImmediate(anim);
                    EditorUtility.SetDirty(btn.gameObject);
                    removedCount++;
                }
                continue;
            }
            
            // 적용 대상: 아직 스크립트가 없는 경우에만 추가
            if (btn.GetComponent<ModernButtonAnim>() == null)
            {
                btn.gameObject.AddComponent<ModernButtonAnim>();
                EditorUtility.SetDirty(btn.gameObject);
                addedCount++;
            }
        }
        
        // 씬 저장 플래그 활성화
        if ((addedCount > 0 || removedCount > 0) && !Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        EditorUtility.DisplayDialog("완료!", $"총 {addedCount}개의 버튼에 모던 클릭 애니메이션을 추가했고, 상단바 버튼 {removedCount}개에서 제거했습니다.", "확인");
    }
}
#endif
