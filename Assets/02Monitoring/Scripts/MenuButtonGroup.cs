using UnityEngine;
using DG.Tweening;

public class MenuButtonGroup : MonoBehaviour
{
    public MenuButton[] buttons;
    public GameObject[] pages;
    public GameObject[] sidebarContents;
    public float fadeDuration = 0.2f;

    private MenuButton currentSelected;
    private int currentPageIndex = -1;

    void Start()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            CanvasGroup cg = GetOrAddCanvasGroup(pages[i]);
            if (i == 0)
            {
                pages[i].SetActive(true);
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
                currentPageIndex = 0;
            }
            else
            {
                pages[i].SetActive(false);
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }
    }

    public void SelectButton(MenuButton target)
    {
        if (currentSelected != null)
            currentSelected.SetSelected(false);
        currentSelected = target;
        currentSelected.SetSelected(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == target)
            {
                SwitchPage(i);
                break;
            }
        }
    }

    void SwitchPage(int index)
    {
        if (currentPageIndex == index) return;

        // 이전 페이지 끄기
        if (currentPageIndex >= 0)
        {
            int prevIndex = currentPageIndex;
            if (prevIndex < pages.Length && pages[prevIndex] != null)
            {
                CanvasGroup prevCG = GetOrAddCanvasGroup(pages[prevIndex]);
                prevCG.DOKill();
                prevCG.interactable = false;
                prevCG.blocksRaycasts = false;
                prevCG.DOFade(0f, fadeDuration).OnComplete(() =>
                {
                    pages[prevIndex].SetActive(false);
                });
            }

            // 이전 사이드바 끄기
            if (prevIndex < sidebarContents.Length && sidebarContents[prevIndex] != null)
            {
                CanvasGroup prevSidebar = GetOrAddCanvasGroup(sidebarContents[prevIndex]);
                prevSidebar.DOKill();
                prevSidebar.interactable = false;
                prevSidebar.blocksRaycasts = false;
                prevSidebar.DOFade(0f, fadeDuration).OnComplete(() =>
                {
                    sidebarContents[prevIndex].SetActive(false);
                });
            }
        }

        currentPageIndex = index;

        // 새 페이지 켜기
        if (index < pages.Length && pages[index] != null)
        {
            pages[index].SetActive(true);
            CanvasGroup nextCG = GetOrAddCanvasGroup(pages[index]);
            nextCG.DOKill();
            nextCG.alpha = 0f;
            nextCG.DOFade(1f, fadeDuration);
            nextCG.interactable = true;
            nextCG.blocksRaycasts = true;

            // 페이지 새로고침 (모든 IPageRefreshable 실행)
            var refreshables = pages[index].GetComponentsInChildren<IPageRefreshable>(true);
            foreach (var r in refreshables)
            {
                r.OnPageRefresh();
            }

            // PageFadeUp 실행
            PageFadeUp fadeUp = pages[index].GetComponent<PageFadeUp>();
            if (fadeUp != null) fadeUp.PlayFadeUp();
        }

        // 새 사이드바 켜기
        if (index < sidebarContents.Length && sidebarContents[index] != null)
        {
            sidebarContents[index].SetActive(true);
            CanvasGroup nextSidebar = GetOrAddCanvasGroup(sidebarContents[index]);
            nextSidebar.DOKill();
            nextSidebar.alpha = 0f;
            nextSidebar.DOFade(1f, fadeDuration);
            nextSidebar.interactable = true;
            nextSidebar.blocksRaycasts = true;
        }
    }

    public void ShowPage(GameObject targetPage)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == targetPage)
            {
                SwitchPage(i);
                break;
            }
        }
    }

    CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }
}