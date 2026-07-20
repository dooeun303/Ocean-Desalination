using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────
// 메뉴의 "화상통화" 버튼 전용 스크립트.
// ButtonHandler / PanelHandler는 여러 버튼이 공유하는 범용 스크립트라서
// 여기에 통화 관련 로직을 넣지 않고, 이 오브젝트에만 별도로 붙여서
// 버튼을 누르면 "화상통화_일반" 패널을 열면서 AR에 통화 요청도 같이 보낸다.
//
// 붙이는 곳: "화상통화" 메뉴 버튼 오브젝트 (ButtonHandler가 이미 붙어있는 오브젝트)
// Inspector에서 연결할 것:
//   videoCallPanel : "화상통화_일반" 오브젝트의 PanelHandler
// ─────────────────────────────────────────────────────────────
public class GeneralVideoCallTrigger : MonoBehaviour
{
    [Header("고정 채널 이름 (MR-AR 1:1 가정)")]
    public string channelName = "general-call";

    [Header("화상통화_일반 패널 (거절되면 자동으로 닫기 위해 필요)")]
    public PanelHandler videoCallPanel;

    void Start()
    {
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnVideoCallButtonClicked);
        }
        else
        {
            Debug.LogWarning("[GeneralVideoCallTrigger] 같은 오브젝트에 Button 컴포넌트가 없습니다.");
        }

        VideoCallSignalingMR.OnCallRejected += OnCallRejected;
    }

    void OnDestroy()
    {
        VideoCallSignalingMR.OnCallRejected -= OnCallRejected;
    }

    // 버튼 클릭 시 (ButtonHandler의 애니메이션/패널 열기와 별개로 같이 실행됨)
    private void OnVideoCallButtonClicked()
    {
        if (VideoCallSignalingMR.Instance == null)
        {
            Debug.LogError("[GeneralVideoCallTrigger] VideoCallSignalingMR.Instance가 null입니다. " +
                "씬에 VideoCallSignalingMR 컴포넌트가 붙은 오브젝트가 있는지 확인하세요.");
            return;
        }

        Debug.Log("[GeneralVideoCallTrigger] 화상통화 요청 전송: " + channelName);
        VideoCallSignalingMR.Instance.RequestCall(channelName);
    }

    // 거절되면 열려있던 패널 닫기
    private void OnCallRejected(string rejectedChannelName)
    {
        if (rejectedChannelName == channelName)
        {
            Debug.Log("[GeneralVideoCallTrigger] 통화 거절됨 → 패널 닫기");
            videoCallPanel?.Hide();
        }
    }
}
