using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public static PlayerInputHandler Local;

    public Vector2 MoveInput;
    public Vector2 LookInput;

    public bool BoostHeld;

    public bool FirePressed;
    public bool PausePressed;
    public bool SurrenderPressed;

    public bool IsMobileLook;

    PlayerInput input;

    void Awake()
    {
        input = new PlayerInput();
    }

    void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMoveCancel;

        input.Player.Look.performed += OnLook;

        input.Player.Boost.performed += OnBoostOn;
        input.Player.Boost.canceled += OnBoostOff;

        input.Player.Attack.performed += OnAttack;

        input.Player.Pause.performed += OnPause;

        input.Player.Surrender.performed += OnSurrender;
    }

    void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMoveCancel;

        input.Player.Look.performed -= OnLook;

        input.Player.Boost.performed -= OnBoostOn;
        input.Player.Boost.canceled -= OnBoostOff;

        input.Player.Attack.performed -= OnAttack;

        input.Player.Pause.performed -= OnPause;

        input.Player.Surrender.performed -= OnSurrender;

        input.Disable();
    }

    void LateUpdate()
    {
        // µ•÷° ‰»Î«Â¡„
        FirePressed = false;
        PausePressed = false;
        SurrenderPressed = false;
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    void OnMoveCancel(InputAction.CallbackContext ctx)
    {
        MoveInput = Vector2.zero;
    }

    void OnLook(InputAction.CallbackContext ctx)
    {
        LookInput = ctx.ReadValue<Vector2>();
    }

    void OnBoostOn(InputAction.CallbackContext ctx)
    {
        BoostHeld = true;
    }

    void OnBoostOff(InputAction.CallbackContext ctx)
    {
        BoostHeld = false;
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        FirePressed = true;
    }

    void OnPause(InputAction.CallbackContext ctx)
    {
        PausePressed = true;
    }

    void OnSurrender(InputAction.CallbackContext ctx)
    {
        SurrenderPressed = true;
    }

    public void UI_Fire()
    {
        FirePressed = true;
    }
}