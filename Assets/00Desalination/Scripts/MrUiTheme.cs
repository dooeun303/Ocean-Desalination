using UnityEngine;
using UnityEngine.UI;

// MR 쪽 HUD 패널들(WorkGuidePanel/LiveDrawOverlay/ModelRotateTestPanel/MrCallDockPanel)이
// 공통으로 쓰는 색/폰트 팔레트.
// 2026-09-09: 씬의 "메뉴" 패널(밝은 카드형)과 톤을 맞추기 위해 다크 → 라이트로 재조정.
// 값(색)만 바꿨고 사용처·구조는 그대로다. 밝은 배경 + 남색 잉크 + 파란 포인트.
public static class MrUiTheme
{
    public static readonly Color Panel = new Color32(0xFF, 0xFF, 0xFF, 0xFA);      // 패널 배경 (거의 불투명 흰색)
    public static readonly Color Accent = new Color32(0x2E, 0x6F, 0xE8, 0xFF);     // 포인트색 (파랑) - 메뉴판 "닫기"/햄버거색
    public static readonly Color AccentSoft = new Color32(0xEC, 0xF2, 0xFC, 0xFF); // 버튼 기본 배경 (아주 옅은 파랑)
    public static readonly Color Good = new Color32(0x22, 0xA1, 0x66, 0xFF);       // 진행/완료 등 긍정 상태 (초록)
    public static readonly Color Warn = new Color32(0xC9, 0x82, 0x1A, 0xFF);       // 분해/토글 켜짐 등 강조 (호박색)
    public static readonly Color Danger = new Color32(0xE5, 0x46, 0x2E, 0xFF);     // 끄기/거절/종료 등 부정 상태 (빨강)
    public static readonly Color Ink = new Color32(0x2C, 0x38, 0x4A, 0xFF);        // 기본 텍스트 (짙은 남색)
    public static readonly Color InkDim = new Color32(0x6B, 0x7A, 0x8A, 0xFF);     // 보조 텍스트 (회청색)

    // 세 패널 모두 이 값으로 카메라 앞 HUD에 매단다 - Z(거리)와 스케일을 통일해서 같은 "층"에
    // 떠 있는 것처럼 보이게 한다. Y 오프셋만 패널마다 달라서 서로 겹치지 않고 세로로 쌓인다.
    public const float HudDepth = 1.4f;
    public const float HudScale = 0.0015f;

    public static Font CreateFont(int size = 36) =>
        Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, size);

    // AR쪽 InmoIconFactory.Round와 동일한 용도 - 9-slice 둥근 테두리 스프라이트.
    public static void Round(Image img, int radius)
    {
        img.sprite = MrIconFactory.RoundedRect(radius);
        img.type = Image.Type.Sliced;
    }
}