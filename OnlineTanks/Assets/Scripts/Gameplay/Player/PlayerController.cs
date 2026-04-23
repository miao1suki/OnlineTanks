using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    private PlayerInputHandler input;
    private PlayerStateMachine stateMachine;

    [SyncVar] Vector3 syncPos;
    [SyncVar] Quaternion syncRot;

    public PlayerInputHandler Input => input;

    void Start()
    {
        input = GetComponent<PlayerInputHandler>();

        if (isServer)
        {
            stateMachine = new PlayerStateMachine(this);
            stateMachine.ChangeState(new MoveState(this));
        }
    }

    void Update()
    {
        // 本地只负责上传输入
        if (isLocalPlayer)
        {
            CmdSendInput(input.MoveInput, input.LookInput);
        }

        // 服务器执行状态机
        if (isServer)
        {
            stateMachine?.Update();
            syncPos = transform.position;
            syncRot = transform.rotation;
        }

        // 客户端插值
        if (!isServer)
        {
            transform.position = Vector3.Lerp(transform.position, syncPos, Time.deltaTime * 10);
            transform.rotation = Quaternion.Slerp(transform.rotation, syncRot, Time.deltaTime * 10);
        }
    }

    [Command]
    void CmdSendInput(Vector2 move, Vector2 look)
    {
        input.MoveInput = move;
        input.LookInput = look;
    }

    // 给状态机调用（服务器执行）
    public void ServerMove(Vector2 move, Vector2 look)
    {
        // 鼠标朝向
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hit = ray.GetPoint(distance);
            Vector3 dir = hit - transform.position;
            dir.y = 0;

            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 10f
                );
            }
        }

        // 移动
        Vector3 forward = transform.forward * move.y;
        Vector3 right = transform.right * move.x;

        Vector3 velocity = (forward + right) * moveSpeed * Time.deltaTime;
        transform.position += velocity;
    }
}