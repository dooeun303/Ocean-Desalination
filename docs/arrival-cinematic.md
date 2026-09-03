# 지원요청 수락 도착 연출 + 설비 정보/도크 패널

> 마지막 업데이트: 2026-09-03
> 커밋: `cfb4136` (Ocean-Desalination@`test/ar-realtime-drawing`)
> Unity 6.0.3 · URP 17

## 1. 목적

MR에서 유지보수 지원요청을 **수락**했을 때, 기존의 단순 텔레포트 대신 3D 연출을 준다.

```
지원요청 수락
  → 화면 페이드 아웃(검게) — 메인 공간 → 공정 공간은 별개 공간이라 필수
  → 설비 앞으로 순간이동 + 메뉴 숨김 + 이동잠금 + 설비 정보/도크 패널 배치
  → 화면 페이드 인
  → 카메라가 설비 앞에 "자리잡는" 짧은 이즈인 모션 (XR Rig 고정, 카메라만)
  → 설비 외곽선(Quick Outline) + 바닥 링 펄스
통화 종료 → 전부 원복
```

## 2. 파일

| 파일 | 상태 | 역할 |
|---|---|---|
| `CameraArrivalMotion.cs` | 신규 | 텔레포트 후 카메라가 설비 앞에 자리잡는 이즈인 모션 |
| `MrArrivalState.cs` | 신규 | 도착 연출 원복 + 도크 패널 포즈를 잇는 정적 브리지 |
| `EquipmentMarker.cs` | 대폭 수정 | 외곽선 + 바닥 링 + 설비 정보 패널 배치 + 도크 포즈 발행 |
| `SupportCallList.cs` | 신규 | 지원요청 큐. 수락 시 페이드+텔레포트+`ApplyArrival` |
| `SupportCallItemUI.cs` | 신규 | 지원요청 목록 항목 UI |
| `MrCallDockPanel.cs` | 수정 | 유지보수 도착 시 카메라 HUD 대신 월드 고정(`worldAnchored`) |
| `MrUiTheme.cs` / `MrIconFactory.cs` | 신규 | MR UI 공용 팔레트/아이콘 |
| `Assets/QuickOutline/` | 신규 | Chris Nolet Quick Outline 1.1 라이브러리 임포트 |
| `Prefab/3dmodelfbx.fbx.meta` | 수정 | 펌프 메시 **Read/Write Enabled** (Quick Outline 스무스 노멀 베이크에 필요) |

## 3. CameraArrivalMotion.cs

**붙이는 위치: `XR Origin (XR Rig) / Camera Offset`** — Main Camera 자체엔 TrackedPoseDriver가
있어서 매 프레임 로컬 포즈를 덮어써 모션이 안 보인다. Camera Offset은 트래킹이 안 걸려서 안전하고
헤드 트래킹과 자연스럽게 합성된다. `SupportCallList`는 `Camera.main.GetComponentInParent<CameraArrivalMotion>()`로 찾는다.

| 필드 | 기본 | 의미 |
|---|---|---|
| `duration` | 3.8 | 모션 길이(초) |
| `pullBack` | 0.9 | 시작 시 로컬 뒤로 물러난 거리 |
| `riseUp` | 0.3 | 시작 시 로컬 위로 |
| `startLookDownDeg` | 6 | 시작 시 추가로 내려다보는 각(모션 중에만, 끝나면 사라짐) |
| `endLookDownDeg` | **0** | 모션 종료 후 유지할 아래 각 — **0 권장** |

> ⚠ `endLookDownDeg != 0` 이면 Camera Offset에 상시 pitch가 남아 헤드트래킹과 합성될 때
> 수평선이 비스듬히 쓸려 **멀미를 유발**한다. "끝나면 설비를 내려다보는 느낌"은 텔레포트 회전
> (`targetPoint`)을 설비 쪽으로 살짝 숙여 잡는 걸로 해결.
> 코드 기본값을 8→0 으로 바꿔도 씬에 저장된 값은 안 바뀌므로 인스펙터에서 직접 0으로.

`Play()`가 처음 호출된 시점의 로컬 포즈를 "제자리"로 기억 → 이후 항상 그 자리로 ease-out cubic 복귀.

## 4. EquipmentMarker.cs

한 설비(디지털 트윈)에 붙는다. `static CurrentlyHighlighted` 로 어디서든 `StopHighlight()` 가능.

### 외곽선 (Quick Outline)

- `[DisallowMultipleComponent]` 이라 파괴/재생성하면 다음 회차 `AddComponent<Outline>()` 실패
  → **컴포넌트를 재사용하고 `enabled` 만 토글**. Quick Outline은 `OnDisable`에서 머티리얼 정리.
- `outlineWidth > 0` 이면 `highlightTarget`(없으면 이 마커)에 `Outline` 부착, `OutlineAll` + `rimColor`
- `Pulse()` 코루틴이 링 스케일/알파 + `OutlineWidth` 를 은은하게 맥동
- **전제**: 대상 메시 `Read/Write Enabled` (펌프 `3dmodelfbx.fbx` 임포트 설정). 안 켜면
  `Not allowed to access vertices ... isReadable is false` 로 외곽선 안 생김

### 바닥 링

- `ringAnchor` 지정 시 그 위치, 아니면 대상 렌더러 바운즈에서 자동
- 반지름: `ringRadius > 0` 이면 그 값, 아니면 바운즈에서 0.4~1.5m
- 링 GameObject는 **부모 없이 월드에** 생성 (마커의 큰 스케일(예: 111x)을 안 물려받게).
  좌표에 반지름을 직접 구워넣음(`Cos(a)*baseRadius`)

### 설비 정보 패널 (`ShowInfo(Transform viewer)`)

`viewer`(XR Rig)의 현재 포즈를 **한 번만 참고**하고 패널을 부모 없이 월드 좌표에 고정 —
스틱 회전으로 Rig가 돌아도 패널은 안 따라온다.

| 필드 | 기본 | 의미 |
|---|---|---|
| `infoPanel` | (비움) | 표시할 패널 루트. 비우면 `EquipmentPanel` 하위 Canvas |
| `infoPanelJustActivate` | false | **★가장 확실★** 씬에서 위치 잡아두고 켜기만 함(이동/회전/스케일 안 건드림) |
| `infoPanelAnchor` | (비움) | 특정 Transform 에 스냅 |
| `infoPanelLocalOffset` | (-0.6, 1.2, 1.2) | XR Rig(바닥 원점) 로컬 기준 (x-:왼쪽, y:높이, z+:앞) |
| `infoPanelYaw` | 20 | 시야 중앙 쪽으로 돌리는 각 |
| `infoPanelScale` | 0.05 | 월드 스케일. 0이면 원래 크기 유지 |
| `infoPanelRecenter` | true | RectTransform 중심을 목표 지점에 맞춤(피벗이 구석이어도 안 튐) |
| `infoPanelFaceViewer` / `infoPanelFaceFlip` | true / false | 사용자를 바라보게 / 읽는 면이 -Z면 뒤집기 |

- `_resolvedPanel` 캐시: 부모를 뗀 뒤에도 재조회/`HideInfo` 되도록
- 디버그 로그: `[EquipmentMarker] 정보 패널 표시: <name> 목표=… 실제=… scale=…`

### 도크 패널 포즈 발행 (`PublishDockPose`)

`placeDockBesideInfo` (기본 ON) 이면 정보 패널 최종 포즈 + `dockPanelOffset`(기본 `(1.6,0,0)`, 오른쪽)
을 계산해 `MrArrivalState.SetDockPose()` 로 남긴다.

## 5. MrArrivalState.cs

```
static Action OnRestore;                 // 숨긴 쪽(SupportCallList)이 등록, 끝난 쪽이 호출
static bool HasDockPose;                  // 도크를 정보 패널 옆에 둘지
static Vector3 DockPos; static Quaternion DockRot;
static void SetDockPose(pos, rot);
static void RestoreArrival();             // HasDockPose=false + OnRestore 1회 실행
```

## 6. SupportCallList.cs

- `TeleportThenShowDock(data)`: 마커의 `teleportPoint` 로 `FadeController.FadeAndTeleport` →
  검은 화면 동안 `xrRig` 이동 + `ApplyArrival(marker)` + `RaiseCallAccepted` → 페이드 인 후
  `arrivalCam.Play()` + `marker.StartCoroutine(marker.Highlight())`
- `ApplyArrival(marker)`:
  - `hideOnArrival[]` GameObject 숨김, `disableDuringCall[]` Behaviour 비활성(이동잠금)
  - `marker.ShowInfo(xrRig)`
  - `MrArrivalState.OnRestore` 에 원복 람다 등록
- 인스펙터: `arrivalCamera`, `hideOnArrival`, `disableDuringCall`, `testTargetMarker`
- `[ContextMenu("▶ 도착 연출 테스트")]` — 서버/AR 체인 없이 연출만 확인 (통화는 안 띄움 → 도크 안 나옴)

> **이동잠금**: `disableDuringCall` 에 locomotion move / teleport provider 를 인스펙터에서 연결.
> 시선(헤드 트래킹)과 컨트롤러 스틱 회전은 유지, 위치 이동만 차단. 통화 종료 시 자동 복구.

## 7. MrCallDockPanel.cs

- `HandleCallAccepted`: `MrArrivalState.HasDockPose` 면 `worldAnchored=true` — 캔버스를
  카메라에서 떼어 `DockPos/DockRot` 에 월드 고정, `Update()`의 카메라 앞 HUD 추적을 건너뜀
  (손으로 잡아 옮기는 건 유지). 일반 통화(포즈 없음)는 기존대로 카메라 HUD.
- `HandleCallEnded/Rejected/MaintenanceComplete`: `EquipmentMarker.CurrentlyHighlighted?.StopHighlight()`
  + `MrArrivalState.RestoreArrival()`

## 8. 남은 것 / 인스펙터 배선 필요

- [ ] `SupportCallList.disableDuringCall` 에 locomotion provider 연결 (이동잠금)
- [ ] `EquipmentMarker` 마다 `teleportPoint` / `highlightTarget` / (필요시) `infoPanel`·`ringAnchor` 지정
- [ ] `hideOnArrival` 에 메뉴 등 도착 시 숨길 오브젝트 연결
- [ ] `Shaders/EquipmentRim.shader`·`EquipmentOutline.shader` 는 폐기된 시도 — 삭제 가능
- [ ] 서버 `support-call` 데이터에 `equipment_id` 가 실려야 실제 수락 경로에서 마커 매칭됨
      (지금 테스트 콜은 `maintenance_log_id: null` → `TestArrival` 컨텍스트 메뉴로 확인)
