using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;
    public float turnTorque = 180f;

    public float fireCooldown = 0.5f;
    float nextFireTime;
    public Transform firePoint;

    [SyncVar]
    public bool isAlive = true;

    PlayerInputHandler input;

    [SyncVar] Vector3 syncPos;
    [SyncVar] Quaternion syncRot;


    float currentAngle;
    float turnVelocity;

    Rigidbody2D rb;
    SpriteRenderer[] sprites;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        rb = GetComponent<Rigidbody2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
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
        if (!isLocalPlayer || !isAlive) return;

        if (!isLocalPlayer) return;

        Vector2 move = input.MoveInput;
        Vector2 look = CalculateLookDirection();

        //检测右键
        bool isBoosting = Mouse.current.rightButton.isPressed;

        // 客户端预测
        ClientPredict(move, look, isBoosting);

        // 发给服务器
        CmdSendInput(move, look, isBoosting);

        //发射子弹
        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;

            CmdFire(
                firePoint.position,
                look
            );
        }
    }

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        rb.linearVelocity = Vector2.zero;

        rb.simulated = false;
        foreach (var s in sprites)
        {
            s.enabled = false;
        }

        if (isServer)
            MatchManager.Instance.OnPlayerDead(this);
    }

    public void Respawn(Vector3 pos)
    {
        isAlive = true;

        transform.position = pos;

        currentAngle = 0;

        rb.linearVelocity = Vector2.zero;

        rb.simulated = true;

        foreach (var s in sprites)
        {
            s.enabled = true;
        }
    }

    [Command]
    void CmdFire(Vector2 pos, Vector2 dir)
    {
        GameObject bullet =
            BulletPool.Instance.GetBullet(netId);

        bullet.transform.position = pos;

        bullet.transform.rotation =
            Quaternion.LookRotation(
                Vector3.forward,
                dir
            );

        bullet.SetActive(true);

        bullet.GetComponent<Bullet>()
            .Launch(dir);

        RpcSpawnBullet(
            pos,
            dir,
            netId
        );
    }

    [ClientRpc]
    void RpcSpawnBullet(
        Vector2 pos,
        Vector2 dir,
        uint ownerId
    )
    {
        if (isServer) return;

        GameObject bullet =
            BulletPool.Instance.GetBullet(ownerId);

        bullet.transform.position = pos;

        bullet.transform.rotation =
            Quaternion.LookRotation(
                Vector3.forward,
                dir
            );

        bullet.SetActive(true);

        bullet.GetComponent<Bullet>()
            .Launch(dir);
    }

    Vector2 CalculateLookDirection()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        return (worldPos - (Vector2)transform.position).normalized;
    }

    // 客户端预测
    void ClientPredict(Vector2 move, Vector2 look, bool boosting)
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

        float speed = boosting ? sprintSpeed : moveSpeed;
        rb.linearVelocity = moveDir * speed;
    }

    // ===== 只负责上传输入 =====
    [Command]
    void CmdSendInput(Vector2 move, Vector2 look, bool boosting)
    {
        ServerSimulate(move, look, boosting);

        syncPos = transform.position;
        syncRot = transform.rotation;
    }

    // ===== 服务器唯一模拟 =====
    void ServerSimulate(Vector2 move, Vector2 look, bool boosting)
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

        float speed = boosting ? sprintSpeed : moveSpeed;
        rb.linearVelocity = moveDir * speed;
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