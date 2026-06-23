# Project Overview
- **Game Title:** 해수담수화 플랜트 디지털 트윈 통합 관제 시스템
- **High-Level Concept:** 플랜트 운영 데이터를 시각화하고 제어하는 통합 대시보드 시스템. 태성에스앤아이 스타일 가이드 v1.1.1 준수.
- **Players:** Single player (Operator)
- **Target Platform:** PC (1920x1080 / 32:9 UltraWide)
- **Render Pipeline:** URP (PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
- 데이터 모니터링 -> 이상 징후 감지 -> 상세 리포트 확인 -> 시스템 제어 및 설정 변경.
## Controls and Input Methods
- 마우스 클릭 및 휠 스크롤 (uGUI 기반), 텍스트 입력, 드롭다운 선택.

# UI Layout (Style Guide v1.1.1)
- **Top Bar:** 84px height, Background #FFFFFF 25% (Blur 22px).
- **Left Menu:** 230px width, Background #FFFFFF 92% (Blur 14px).
- **Content Area:** Padding (Top 24px, Side 32px, Bottom 40px), Background #EAF3F5.
- **Panel:** Radius 14px, Internal Padding (V 18px, H 20px), Gap 16px.

# Key Asset & Context
- **Scripts:** 
  - `Assets/Scripts/UI/DesignSystem.cs`: TextMeshPro 지원 및 타이틀 생성 기능 추가.
  - `Assets/Scripts/UI/Interactions/UIFadeUp.cs`: 진입 애니메이션 (12px up, 0.5s).
  - `Assets/Scripts/UI/Interactions/UIHoverDepth.cs`: 호버 피드백 (KPI 2px 이동 및 그림자).
- **Fonts:** `Noto Sans KR` (TMP SDF)
- **Colors:** Point(#73CF79), Link(#5CC196), IconActive(#50B8B8), Background(#EAF3F5), TextCritical(#1A202D).

# Implementation Steps

## 1. DesignSystem 고도화 (TextMeshPro 및 타이틀)
- **Description**: `DS.cs`에 TMP 지원 함수 추가 및 타이틀 영역 생성 로직 구현.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes
- **Details**:
  - `MakeTextTMP` 함수 추가 (TextMeshProUGUI 기반).
  - `MakePageHeader` 함수 추가 (타이틀 그라데이션 및 부제 배치).
  - 가이드라인의 pt 단위를 TMP 폰트 사이즈로 매핑.

## 2. 인터랙션 스크립트 구현
- **Description**: Fade-up 및 Hover 피드백 스크립트 작성.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes
- **Details**:
  - `UIFadeUp.cs`: DOTween 또는 단순 보간을 이용한 0.5초 애니메이션.
  - `UIHoverDepth.cs`: KPI 카드와 일반 카드의 호버 상태 분기 처리.

## 3. 콘텐츠 패널 프리팹 최신화
- **Description**: 가이드라인 v1.1.1 수치에 맞춘 기본/복합 패널 프리팹 업데이트.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No
- **Details**:
  - 패널 배경(#FFFFFF), 테두리(#50B8B8 10%), 모서리(14px).
  - 헤더와 본문 사이 구분선(1px, Alpha 4%) 추가.

## 4. 테이블 및 상태 인디케이터 구현
- **Description**: 가이드라인의 테이블 규격 및 상태 점(6x6px) 구현.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

# Verification & Testing
- **Visual Alignment**: 씬의 UI 요소들이 가이드라인의 px 수치와 일치하는지 Inspecter에서 확인.
- **Motion Test**: 실행 시 모든 패널이 Fade-up 되며 나타나는지 확인.
- **Color Contrast**: 텍스트 시인성이 가이드라인 규정(#1A202D, #3D5A6A)대로 확보되었는지 확인.
