using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput;
    public Vector2 LookInput;

    PlayerInput input;

    void Awake()
    {
        input = new PlayerInput();
    }

    void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        input.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
    }

    void OnDisable()
    {
        input.Disable();
    }
}