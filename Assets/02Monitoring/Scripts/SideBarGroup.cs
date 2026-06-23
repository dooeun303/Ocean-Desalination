using UnityEngine;

public class SidebarGroup : MonoBehaviour
{
    public SidebarMenu[] menus;
    private SidebarMenu currentOpen;

    public void SelectMenu(SidebarMenu target)
    {
        // 현재 열린 메뉴 닫기
        if (currentOpen != null && currentOpen != target)
            currentOpen.Close();

        // 같은 메뉴 누르면 토글
        if (currentOpen == target)
        {
            target.Close();
            currentOpen = null;
        }
        else
        {
            target.Open();
            currentOpen = target;
        }
    }
}