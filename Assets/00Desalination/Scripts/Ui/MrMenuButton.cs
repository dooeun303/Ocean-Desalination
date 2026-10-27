using UnityEngine;
using UnityEngine.InputSystem;

// 2026-10-27 Quest: "우측 컨트롤러의 B 키를 누르면 메뉴가 나타나야 한다".
// 씬의 XR Origin/메뉴(잡아서 옮길 수 있는 캔버스)를 B로 연다/닫는다.
//  - 꺼져 있거나 시야 밖이면 → 눈앞(처음 메뉴와 같은 거리, 같은 크기로 보이게)에 띄운다
//  - 이미 눈앞에 떠 있으면 → 닫는다
// 씬을 건드리지 않도록 시작할 때 스스로 붙는다.
public class MrMenuButton : MonoBehaviour
{
    const string MenuPath = "시스템/XR Interaction Setup/XR Origin (XR Rig)/메뉴";
    const float InViewAngle = 35f; // 시선과 이 각도 안이면 "눈앞에 떠 있다"

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (GameObject.Find(MenuPath) == null) return; // 메뉴가 없는 씬
        var go = new GameObject("[MrMenuButton]");
        DontDestroyOnLoad(go);
        go.AddComponent<MrMenuButton>();
    }

    InputAction bButton;
    Transform menu;
    float distance; // 처음 메뉴가 놓인 수평 거리 - 같은 크기로 보이게 그대로 쓴다

    void Awake()
    {
        var go = GameObject.Find(MenuPath);
        menu = go.transform;
        var rig = menu.parent;
        distance = rig != null ? Vector3.ProjectOnPlane(menu.localPosition, Vector3.up).magnitude * rig.lossyScale.x : 2.5f;
        if (distance < 1f) distance = 2.5f;

        bButton = new InputAction("MenuToggle", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");
        bButton.performed += _ => Toggle();
    }

    void OnEnable() => bButton?.Enable();
    void OnDisable() => bButton?.Disable();
    void OnDestroy() => bButton?.Dispose();

    void Toggle()
    {
        if (menu == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        if (menu.gameObject.activeInHierarchy && InView(cam))
        {
            menu.gameObject.SetActive(false);
            Debug.Log("[MrMenuButton] B - 메뉴 닫기");
            return;
        }

        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
        fwd.Normalize();
        menu.gameObject.SetActive(true);
        EquipmentMarker.PlaceCentered(menu, cam.transform.position + fwd * distance, Quaternion.LookRotation(fwd, Vector3.up));
        Debug.Log("[MrMenuButton] B - 메뉴 눈앞에 열기");
    }

    bool InView(Camera cam)
    {
        Vector3 to = Vector3.ProjectOnPlane(menu.position - cam.transform.position, Vector3.up);
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        return to.sqrMagnitude > 0.0001f && fwd.sqrMagnitude > 0.0001f && Vector3.Angle(fwd, to) <= InViewAngle;
    }
}
