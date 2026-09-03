// 지원요청 수락 시 "도착 연출"(메뉴 숨김 등)을 걸어둔 쪽(SupportCallList)과, 통화가 끝났을 때
// 그걸 원복해야 하는 쪽(MrCallDockPanel)을 느슨하게 잇는 다리. 씬 참조를 여기저기 중복으로
// 물릴 필요 없이, 숨긴 쪽이 복구 동작을 등록하고 끝난 쪽이 호출만 한다.
public static class MrArrivalState
{
    // SupportCallList가 도착 연출을 걸면서 "이걸 하면 원복됨" 을 등록한다. 없으면 아무 일도 안 함.
    public static System.Action OnRestore;

    // 도착 시 EquipmentMarker가 "도크(화상통화) 패널을 정보 패널 옆 여기에 두라"고 남기는 포즈.
    // MrCallDockPanel.HandleCallAccepted 가 읽어서 카메라-추적 HUD 대신 이 자리에 월드 고정으로 띄운다.
    public static bool HasDockPose;
    public static UnityEngine.Vector3 DockPos;
    public static UnityEngine.Quaternion DockRot = UnityEngine.Quaternion.identity;

    public static void SetDockPose(UnityEngine.Vector3 pos, UnityEngine.Quaternion rot)
    {
        HasDockPose = true; DockPos = pos; DockRot = rot;
    }

    public static void RestoreArrival()
    {
        HasDockPose = false; // 통화 끝 - 다음 도착 때 다시 세팅됨
        var cb = OnRestore;
        OnRestore = null; // 1회성 - 다음 수락 때 다시 등록됨
        cb?.Invoke();
    }
}
