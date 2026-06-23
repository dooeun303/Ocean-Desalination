using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Monitoring
{
    /// <summary>
    /// 설계 정보·자산 관리 대시보드의 사이드바 네비게이션(탭) 전환 컨트롤러.
    /// 선택된 항목을 강조(배경/텍스트 색상)하고, 매핑된 콘텐츠 패널을 전환한다.
    /// </summary>
    public class AssetNavController : MonoBehaviour
    {
        [Serializable]
        public class NavItem
        {
            public string id;                 // 메뉴 식별자 (예: "설비 대장")
            public Button button;             // 클릭 대상
            public Image background;          // 강조 배경 이미지
            public TMP_Text label;            // 메뉴 라벨
            public Image icon;                // 메뉴 아이콘 (선택)
            public GameObject indicator;      // 활성 표시 바 (선택)
            public GameObject contentPanel;   // 이 탭에서 보여줄 콘텐츠 (없으면 null)
        }

        [Header("네비게이션 항목")]
        [SerializeField] private List<NavItem> items = new List<NavItem>();

        [Header("색상 - 활성")]
        [SerializeField] private Color activeBackground = new Color(0.314f, 0.722f, 0.722f, 0.10f);
        [SerializeField] private Color activeText = new Color(0.314f, 0.722f, 0.722f, 1f);

        [Header("색상 - 비활성")]
        [SerializeField] private Color inactiveBackground = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color inactiveText = new Color(0.45f, 0.52f, 0.58f, 1f);

        [Header("초기 선택 인덱스")]
        [SerializeField] private int defaultIndex = 0;

        /// <summary>탭이 전환될 때 발생 (선택된 인덱스, id 전달).</summary>
        public event Action<int, string> OnTabChanged;

        private int currentIndex = -1;

        private void Awake()
        {
            for (int i = 0; i < items.Count; i++)
            {
                int index = i; // 클로저 캡처 방지
                if (items[i] != null && items[i].button != null)
                    items[i].button.onClick.AddListener(() => SelectTab(index));
            }
        }

        private void Start()
        {
            SelectTab(Mathf.Clamp(defaultIndex, 0, items.Count - 1));
        }

        /// <summary>지정한 인덱스의 탭을 선택한다.</summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= items.Count) return;
            if (index == currentIndex) return;

            currentIndex = index;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                bool active = (i == index);

                if (item.background != null)
                    item.background.color = active ? activeBackground : inactiveBackground;

                if (item.label != null)
                {
                    item.label.color = active ? activeText : inactiveText;
                    if (active) item.label.fontStyle |= FontStyles.Bold;
                    else item.label.fontStyle &= ~FontStyles.Bold;
                }

                if (item.icon != null)
                {
                    var c = item.icon.color;
                    item.icon.color = active
                        ? new Color(activeText.r, activeText.g, activeText.b, 1f)
                        : new Color(c.r, c.g, c.b, 0.55f);
                }

                if (item.indicator != null)
                    item.indicator.SetActive(active);

                if (item.contentPanel != null)
                    item.contentPanel.SetActive(active);
            }

            OnTabChanged?.Invoke(index, items[index]?.id);
            
            // 페이지 로드 시 인터랙션 및 데이터 갱신 실행
            TriggerPageRefresh(index);
        }

        /// <summary>
        /// 페이지 로드/새로고침을 통합 수행하는 함수.
        /// 인터랙션(PageFadeUp) + 데이터 로드(DB 테이블)를 모두 포함한다.
        /// </summary>
        public void LoadPage(int index)
        {
            SelectTab(index);
        }

        /// <summary>현재 활성화된 페이지를 새로고침한다.</summary>
        public void RefreshCurrentPage()
        {
            if (currentIndex >= 0 && currentIndex < items.Count)
            {
                TriggerPageRefresh(currentIndex);
            }
        }

        private void TriggerPageRefresh(int index)
        {
            if (index < 0 || index >= items.Count) return;
            var panel = items[index].contentPanel;
            if (panel == null) return;

            // 해당 패널 내의 모든 IPageRefreshable 구성 요소를 찾아 실행
            // (PageFadeUp, TableController, KPI 등)
            var refreshables = panel.GetComponentsInChildren<IPageRefreshable>(true);
            foreach (var r in refreshables)
            {
                r.OnPageRefresh();
            }
        }

        /// <summary>현재 선택된 인덱스.</summary>
        public int CurrentIndex => currentIndex;
    }
}
