using UnityEngine;
using Monitoring;

public class PageRefresh : MonoBehaviour
{
    [Header("네비게이션 컨트롤러 참조")]
    public AssetNavController navController;

    // 새로고침 버튼에서 호출
    public void OnRefreshButton()
    {
        if (navController != null)
        {
            navController.RefreshCurrentPage();
        }
    }

    // 특정 탭 로드
    public void LoadPage(int index)
    {
        if (navController != null)
        {
            navController.LoadPage(index);
        }
    }

    public void OnAlertTab() => LoadPage(0);
    public void OnMaintenanceTab() => LoadPage(1);
    public void OnRemoteTab() => LoadPage(2);
}
