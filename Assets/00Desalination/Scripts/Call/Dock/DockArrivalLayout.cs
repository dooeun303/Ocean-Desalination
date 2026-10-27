using System.Collections.Generic;
using UnityEngine;

// 화상통화 도크와 함께 뜨는 패널들의 배치. 한 번 놓으면 월드에 고정(고개를 돌려도 안 따라옴), 손으로 잡아 옮길 수는 있다.
//  - 원격지원으로 설비 앞에 도착: 시선 기준 "부채꼴" - 가운데 도크, 왼쪽 공정정보, 오른쪽은 정보공유/지시하기 자리(버튼을 누르면 그 자리에)
//  - 유지보수 도착 연출이 도크 자리를 정해 둔 경우: 그 자리(시선 밖이면 눈앞)
//  - 그 외: 눈앞
public class DockArrivalLayout
{
    // 도크는 작은 HUD 버튼들과 같은 MrUiTheme.HudDepth(1.4)를 그대로 썼더니, 전체 크기(900x1084
    // 유닛)가 커서 얼굴 바로 앞을 가릴 만큼 크게 보였다("너무 가까워" 실기 확인). 버튼들은 크기가
    // 작아 그 거리에서도 괜찮았지만, 도크는 더 멀리 둬야 같은 체감 크기가 된다.
    public const float DockDepth = 2.2f;
    // 양옆 자리는 시선에서 ±SideYaw 돌린 방향 SideDistance에 두고 사용자 쪽을 바라보게 - 살짝 안쪽으로 기울어진 부채꼴.
    const float SideYaw = 38f;
    const float SideDistance = 3.0f; // 2026-10-21 "너무 앞에 나와 있어" 1.8 → 2.4 (가운데 도크 2.2보다 살짝 뒤)

    bool hasRightSlot;
    Vector3 rightSlotPos;
    Quaternion rightSlotRot;

    // 오른쪽 자리에 옮긴 씬 패널들의 원래 자리(부모 기준) - 통화가 끝나면 되돌린다.
    class MovedPanel { public Transform t; public Vector3 pos; public Quaternion rot; public bool wasActive; }
    readonly List<MovedPanel> movedPanels = new List<MovedPanel>();

    // 원격지원 도착 화면: 가운데 도크, 왼쪽 공정정보(도크와 같은 폭), 오른쪽은 자리만 기억.
    public void LayoutArrival(Transform dock, EquipmentMarker marker, float dockWorldWidth)
    {
        var cam = Camera.main.transform;
        Vector3 fwd = FlatForward(cam);
        Vector3 eye = cam.position;
        Quaternion Facing(Vector3 p) => Quaternion.LookRotation(Vector3.ProjectOnPlane(p - eye, Vector3.up).normalized, Vector3.up);
        Vector3 Side(float yaw) => eye + Quaternion.AngleAxis(yaw, Vector3.up) * fwd * SideDistance;

        Detach(dock);
        Vector3 center = eye + fwd * DockDepth;
        EquipmentMarker.PlaceCentered(dock, center, Facing(center));

        Vector3 left = Side(-SideYaw); // 통화 끝나면 EquipmentMarker.HideInfo가 원래 자리로
        marker.MoveInfoPanel(left, Facing(left), dockWorldWidth);

        rightSlotPos = Side(SideYaw);
        rightSlotRot = Facing(rightSlotPos);
        hasRightSlot = true;
        Debug.Log("[MrCallDockPanel] 도착 화면 배치: 공정정보(왼쪽) · 화상통화(가운데), 오른쪽은 버튼 패널 자리");
    }

    public static void PlaceAt(Transform dock, Vector3 pos, Quaternion rot)
    {
        Detach(dock);
        dock.SetPositionAndRotation(pos, rot);
    }

    // 지금 시선의 수평 방향 앞(DockDepth), 눈높이에 월드 고정
    public static void PlaceInFront(Transform dock)
    {
        Detach(dock);
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = FlatForward(cam.transform);
        dock.SetPositionAndRotation(cam.transform.position + fwd * DockDepth, Quaternion.LookRotation(fwd, Vector3.up));
    }

    // 정보공유/지시하기 패널을 오른쪽 자리에 놓는다. 원래 자리는 처음 한 번 기억해 두고 통화 끝에 되돌린다.
    // 도착 배치가 없던 통화(설비 없이 받은 통화)면 씬에 놓인 자리 그대로 둔다.
    public void PlaceInRightSlot(Transform t, bool wasActiveBeforeOpen)
    {
        if (!hasRightSlot || t == null) return;
        if (!movedPanels.Exists(m => m.t == t))
            movedPanels.Add(new MovedPanel { t = t, pos = t.localPosition, rot = t.localRotation, wasActive = wasActiveBeforeOpen });
        EquipmentMarker.PlaceCentered(t, rightSlotPos, rightSlotRot);
        MrUiOnTop.Apply(t.gameObject);
    }

    // 통화가 끝나면 오른쪽 자리에 옮겼던 패널을 원래 자리·켜짐 상태로 되돌린다.
    public void RestoreRightSlot()
    {
        foreach (var m in movedPanels)
        {
            if (m.t == null) continue;
            MrUiOnTop.Restore(m.t.gameObject);
            m.t.localPosition = m.pos;
            m.t.localRotation = m.rot;
            m.t.gameObject.SetActive(m.wasActive);
        }
        movedPanels.Clear();
        hasRightSlot = false;
    }

    // 수평 시선 기준 ±60도 안, 4m 이내면 "앞에 보이는 자리"로 본다.
    public static bool IsInFrontOfViewer(Vector3 pos)
    {
        var cam = Camera.main;
        if (cam == null) return true;
        Vector3 to = pos - cam.transform.position;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        Vector3 toFlat = Vector3.ProjectOnPlane(to, Vector3.up);
        float angle = fwd.sqrMagnitude < 0.0001f || toFlat.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(fwd, toFlat);
        bool ok = angle <= 60f && to.magnitude <= 4f;
        Debug.Log($"[MrCallDockPanel] 도크 지정 위치 각도={angle:F0}° 거리={to.magnitude:F2}m → {(ok ? "정보 패널 옆에 고정" : "시선 밖이라 눈앞에 고정")}");
        return ok;
    }

    // 도크는 부모 없이 월드에 두고 HUD와 같은 배율로
    static void Detach(Transform dock)
    {
        dock.SetParent(null, true);
        dock.localScale = Vector3.one * MrUiTheme.HudScale;
    }

    static Vector3 FlatForward(Transform cam)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(cam.up, Vector3.up);
        return fwd.normalized;
    }
}
