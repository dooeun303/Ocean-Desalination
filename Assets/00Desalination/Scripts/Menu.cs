using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class Menu : MonoBehaviour
{
    // 메뉴캔버스
    public GameObject menuCanvas;
    public PanelHandler popupWindow;

    // 메뉴 인풋 액션 (우컨트롤러 B키)
    public InputActionReference menuAction; 

    // 객체 활성화시 실행
    private void OnEnable()
    {
        // B 키가 눌리면 OnMenuPressed 이벤트 실행
        menuAction.action.performed += OnMenuPressed;
        menuAction.action.Enable();
    }

    // 객체 비활성화시 실행
    private void OnDisable()
    {
        menuAction.action.performed -= OnMenuPressed;
        menuAction.action.Disable();
    }

    // B키 눌렸을 때 ToggleMenu 함수 실행
    void OnMenuPressed(InputAction.CallbackContext ctx)
    {
        ToggleMenu(); // 아래 ToggleMenu 함수 실행
    }

    // 토글 메뉴
    void ToggleMenu()
    {
        // 메뉴 캔버스가 켜져있는지 확인
        bool isActive = menuCanvas.activeSelf;

        // 메뉴캔버스 활성화 시키기
         //menuCanvas.SetActive(!isActive);

        OnButtonClick();

    }

    public void OnButtonClick()
    {
        var seq = DOTween.Sequence();

        seq.Append(transform.DOScale(0.095f, 0.1f));
        seq.Append(transform.DOScale(0.105f, 0.1f));
        seq.Append(transform.DOScale(0.1f, 0.1f));

        seq.Play().OnComplete(() => {
            popupWindow.ShowForMenu();
        });
    }
}
