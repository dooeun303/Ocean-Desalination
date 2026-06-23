using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;  // 네임스페이스 추가

public class TestPlayerInput : MonoBehaviour
{
    private Vector2 moveInput;  // 입력값 저장
    private PlayerInput playerInput;
    private bool isSprinting = false;
    public float speed = 5f;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerInput.actions.FindActionMap("Player").Enable();
    }

    // Input Actions에서 자동으로 호출되는 함수
    // 함수 이름 규칙: On + Action이름
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
            Debug.Log("점프!");
    }

    void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;

    }

    void Update()
    {
        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);
        float currentSpeed = isSprinting ? speed * 2 : speed;

        transform.position += dir * currentSpeed * Time.deltaTime;



    }
}