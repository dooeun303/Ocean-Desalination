using UnityEngine;
using UnityEngine.InputSystem;

// 컨트롤러 B버튼(오른손 하단 버튼)을 누르면 메뉴 패널을 켜고 끈다.
// 인스펙터에서 menuPanel 에 토글할 패널(예: 햄버거 메뉴 Canvas/오브젝트)을 연결해서 사용.
//
// UnityEngine.XR.InputDevices(레거시 XR SDK 경로)가 아니라 새 Input System 의
// <XRController> 레이아웃을 직접 바인딩해서 쓴다. 에디터의 XR Device Simulator 도,
// 실기기(OpenXR/Oculus)도 컨트롤러를 이 Input System 레이어에 <XRController> 로
// 노출하기 때문에 이 방식이 양쪽 다 동작한다. (레거시 InputDevices 는 시뮬레이터가
// 값을 안 흘려줘서 에디터 테스트가 불가능했음)
public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;

    private InputAction _secondaryButtonAction;

    private void Awake()
    {
        _secondaryButtonAction = new InputAction(
            name: "MenuToggle",
            type: InputActionType.Button,
            binding: "<XRController>{RightHand}/secondaryButton");

        _secondaryButtonAction.performed += OnSecondaryButtonPerformed;
    }

    private void OnEnable() => _secondaryButtonAction.Enable();

    private void OnDisable() => _secondaryButtonAction.Disable();

    private void OnDestroy() => _secondaryButtonAction.Dispose();

    private void OnSecondaryButtonPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log(nameof(Menu) + ": Secondary Button(B) 눌림 감지됨");
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning(nameof(Menu) + ": menuPanel 이 비어있습니다. 인스펙터에서 연결하세요.");
            return;
        }

        bool newState = !menuPanel.activeSelf;
        menuPanel.SetActive(newState);
        Debug.Log(nameof(Menu) + $": menuPanel({menuPanel.name}) SetActive({newState}) 호출. 결과 activeInHierarchy={menuPanel.activeInHierarchy}");
    }
}
