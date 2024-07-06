using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2)]
[RequireComponent(typeof(PlayerInput))]
public class InputReader : MonoBehaviour
{
    public static InputReader Instance;

    public Vector2 moveInput;

    public bool _runHeld;
    public bool _jumpPressed;
    public bool _jumpHeld;
    public bool _dashPressed;
    public bool _dashHeld;

    // References
    private PlayerInput _playerInputs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        _jumpPressed = false;
        _dashPressed = false;
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _jumpPressed = true;
        }
        else if (context.performed)
        {
            _jumpHeld = true;
        }
        else if (context.canceled)
        {
            _jumpHeld = false;
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _runHeld = true;
        }
        else if (context.canceled)
        {
            _runHeld = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _dashPressed = true;
        }
        else if (context.performed)
        {
            _dashHeld = true;
        }
        else if (context.canceled)
        {
            _dashHeld = false;
        }
    }
}
