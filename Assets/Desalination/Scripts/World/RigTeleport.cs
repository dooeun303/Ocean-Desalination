using UnityEngine;

// XR 리그 순간이동 공용. 리그는 CharacterController로 움직이는데 프로젝트 설정의 Physics.autoSyncTransforms가
// 꺼져 있어서, transform.position만 바꾸면 CharacterController 내부 위치가 예전 자리에 남아 이동이 꼬였다
// (2026-10-27 Quest: 알람 "설비로 이동" 뒤 조이스틱으로 안 움직임). 컨트롤러를 잠깐 끄고 옮긴 뒤 물리에 반영한다.
public static class RigTeleport
{
    public static void MoveTo(Transform rig, Vector3 position, Quaternion rotation)
    {
        var cc = rig.GetComponent<CharacterController>();
        bool wasEnabled = cc != null && cc.enabled;
        if (wasEnabled) cc.enabled = false;
        rig.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
        if (wasEnabled) cc.enabled = true;
    }

    // 설비 텔레포트 지점으로 - 바라보는 수평 방향만 쓴다(EquipmentMarker.UprightRotation).
    public static void MoveTo(Transform rig, Transform point) =>
        MoveTo(rig, point.position, EquipmentMarker.UprightRotation(point));
}
