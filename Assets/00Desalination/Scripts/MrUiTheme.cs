using UnityEngine;
using UnityEngine.UI;

// MR 쪽 HUD 패널들(WorkGuidePanel/LiveDrawOverlay/ModelRotateTestPanel)이 공통으로 쓰는 색/폰트
// 팔레트. AR쪽 InmoUiTheme.cs와 같은 값 - 원래 AR 색 자체가 "MR 쪽에 먼저 목업으로 만들어서
// 승인받은 색을 그대로 옮긴 것"(InmoUiTheme.cs 주석 참고)이라, 그 원본 색을 MR에도 명시적인
// 유틸리티로 만들어서 세 패널이 각자 다른 색을 하드코딩하던 것("너무 중구난박이야")을 통일한다.
// VR(MR)은 완전 몰입형이라 AR의 라이트/다크·시스루 배경 개념은 필요 없다 - 항상 다크 하나만 쓴다.
public static class MrUiTheme
{
    public static readonly Color Panel = new Color32(0x00, 0x00, 0x00, 0xEB);      // 패널 배경 (거의 불투명 검정)
    public static readonly Color Accent = new Color32(0x4F, 0xC3, 0xF7, 0xFF);     // 포인트색 (밝은 스틸블루) - AR과 동일
    public static readonly Color AccentSoft = new Color32(0x1E, 0x2C, 0x31, 0xFF); // 버튼 기본 배경
    public static readonly Color Good = new Color32(0x3D, 0xDC, 0x84, 0xFF);       // 진행/완료 등 긍정 상태
    public static readonly Color Warn = new Color32(0xE0, 0x9A, 0x2C, 0xFF);       // 분해/토글 켜짐 등 강조
    public static readonly Color Danger = new Color32(0xFF, 0x66, 0x59, 0xFF);     // 끄기/거절/종료 등 부정 상태
    public static readonly Color Ink = new Color32(0xFF, 0xFF, 0xFF, 0xFF);        // 기본 텍스트
    public static readonly Color InkDim = new Color32(0xB0, 0xBE, 0xC5, 0xFF);     // 보조 텍스트

    // 세 패널 모두 이 값으로 카메라 앞 HUD에 매단다 - Z(거리)와 스케일을 통일해서 같은 "층"에
    // 떠 있는 것처럼 보이게 한다. Y 오프셋만 패널마다 달라서 서로 겹치지 않고 세로로 쌓인다.
    public const float HudDepth = 1.4f;
    public const float HudScale = 0.0015f;

    public static Font CreateFont(int size = 36) =>
        Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, size);

    // AR쪽 InmoUiTheme.Round와 동일한 용도 - 9-slice 둥근 테두리 스프라이트.
    public static void Round(Image img, int radius)
    {
        img.sprite = MrIconFactory.RoundedRect(radius);
        img.type = Image.Type.Sliced;
    }
}
