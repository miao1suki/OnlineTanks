using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float turnTorque = 180f;

    PlayerInputHandler input;

    [SyncVar] Vector3 syncPos;
    [SyncVar] Quaternion syncRot;

    float currentAngle;
    float turnVelocity;

    Rigidbody2D rb; 

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        rb = GetComponent<Rigidbody2D>(); 
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            rb.simulated = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        Vector2 move = input.MoveInput;
        Vector2 look = CalculateLookDirection();

        // 本地预测
        ClientPredict(move, look);

        // 发给服务器
        CmdSendInput(move, look);
    }

    Vector2 CalculateLookDirection()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        return (worldPos - (Vector2)transform.position).normalized;
    }

    // 客户端预测
    void ClientPredict(Vector2 move, Vector2 look)
    {
        Vector2 targetDir = look;

        if (targetDir.sqrMagnitude > 0.001f)
        {
            float targetAngle =
                Mathf.Atan2(targetDir.x, targetDir.y) *
                Mathf.Rad2Deg;

            currentAngle = Mathf.SmoothDampAngle(
                currentAngle,
                targetAngle,
                ref turnVelocity,
                0.08f
            );
        }

        currentAngle -= move.x * -turnTorque * Time.deltaTime;

        transform.rotation =
            Quaternion.Euler(0, 0, -currentAngle);

        Vector2 forward = new Vector2(
            Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        );

        Vector2 moveDir = forward * move.y;

        rb.linearVelocity = moveDir * moveSpeed;
    }

    // ===== 只负责上传输入 =====
    [Command]
    void CmdSendInput(Vector2 move, Vector2 look)
    {
        ServerSimulate(move, look);

        syncPos = transform.position;
        syncRot = transform.rotation;
    }

    // ===== 服务器唯一模拟 =====
    void ServerSimulate(Vector2 move, Vector2 look)
    {
        Vector2 targetDir = look;

        // 旋转（平滑朝向）
        if (targetDir.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(targetDir.x, targetDir.y) * Mathf.Rad2Deg;

            currentAngle = Mathf.SmoothDampAngle(
                currentAngle,
                targetAngle,
                ref turnVelocity,
                0.08f
            );
        }


        //A/D施加额外扭矩
        currentAngle -= move.x * -turnTorque * Time.deltaTime;


        transform.rotation = Quaternion.Euler(
            0,
            0,
            -currentAngle
        );


        Vector2 forward = new Vector2(
            Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        );

        Vector2 moveDir = forward * move.y;

        rb.linearVelocity = moveDir * moveSpeed;
    }

    void LateUpdate()
    {
        if (isLocalPlayer)
        {
            //此处留空做反作弊回滚
            return;
        }

        transform.position =
            Vector3.Lerp(
                transform.position,
                syncPos,
                10f * Time.deltaTime
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                syncRot,
                10f * Time.deltaTime
            );
    }
}