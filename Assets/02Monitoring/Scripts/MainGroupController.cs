using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Monitoring.UI
{
    /// <summary>
    /// 상단바 대메뉴 선택에 따라 사이드바와 콘텐츠 영역 전체를 전환하는 컨트롤러.
    /// </summary>
    public class MainGroupController : MonoBehaviour
    {
        [Serializable]
        public class GroupItem
        {
            public string groupName;
            public Button topBarButton;
            public GameObject sidebar;
            public GameObject tabArea;
            public GameObject commonHeader;
            public Image tileBackground;
            public TMP_Text tileText;
            public Sprite activeSprite;
            public Sprite inactiveSprite;
        }

        [SerializeField] private List<GroupItem> groups = new List<GroupItem>();
        
        [Header("Colors")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.239f, 0.353f, 0.416f, 1f); // #3D5A6A

        private int currentGroupIndex = -1;

        private void Awake()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                int index = i;
                if (groups[i].topBarButton != null)
                    groups[i].topBarButton.onClick.AddListener(() => SelectGroup(index));
            }
        }

        private void Start()
        {
            // Default to Asset Management (assumed index 0)
            if (groups.Count > 0) SelectGroup(0);
        }

        public void SelectGroup(int index)
        {
            if (index < 0 || index >= groups.Count) return;
            if (index == currentGroupIndex) return;

            currentGroupIndex = index;

            for (int i = 0; i < groups.Count; i++)
            {
                bool isActive = (i == index);
                var g = groups[i];

                if (g.sidebar != null) g.sidebar.SetActive(isActive);
                if (g.tabArea != null) g.tabArea.SetActive(isActive);
                if (g.commonHeader != null) g.commonHeader.SetActive(isActive);

                if (g.tileBackground != null)
                {
                    g.tileBackground.sprite = isActive ? g.activeSprite : g.inactiveSprite;
                    g.tileBackground.type = isActive ? Image.Type.Simple : Image.Type.Sliced;
                    // For inactive, we use the DCE2E6 color logic
                    if (!isActive) g.tileBackground.color = new Color(0.863f, 0.886f, 0.902f, 0.90f);
                    else g.tileBackground.color = Color.white;
                }

                if (g.tileText != null)
                {
                    g.tileText.color = isActive ? activeColor : inactiveColor;
                }
            }
        }
    }
}
