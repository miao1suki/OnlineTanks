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
    public PlayerHitBox hitBox;

    [SyncVar(hook = nameof(OnAliveChanged))]
    public bool isAlive = true;

    PlayerInputHandler input;
    // 统一入口锁
    bool inputLocked = true;

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
        // ESC菜单锁
        if (EscMenuController.Instance != null &&
        EscMenuController.Instance.IsOpen())
            return;

        if (!isLocalPlayer || !isAlive || inputLocked) return;


        Vector2 move = input.MoveInput;
        Vector2 look = CalculateLookDirection();

        //按B放弃
        if (input.SurrenderPressed)
        {
            if (RoomCanvasController.Instance != null &&
                RoomCanvasController.Instance.CanSurrender)
            {
                CmdSurrender();
            }
        }

        //检测右键
        bool isBoosting = input.BoostHeld;

        // 客户端预测
        ClientPredict(move, look, isBoosting);

        // 发给服务器
        CmdSendInput(move, look, isBoosting);

        //发射子弹
        if (input.FirePressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;

            CmdFire(
                firePoint.position,
                look
            );
        }
    }
    // 统一初始化函数
    public void SetSpawnState(Vector3 pos, bool aliveState)
    {
        transform.position = pos;

        syncPos = pos;
        syncRot = Quaternion.identity;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = aliveState;

        isAlive = aliveState;

        currentAngle = 0;

        inputLocked = false;

        foreach (var s in sprites)
            s.enabled = aliveState;
    }

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        inputLocked = true;

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
        if (!isServer) return;

        isAlive = true;
        SetSpawnState(pos, true);
    }

    [TargetRpc]
    public void TargetRespawn(NetworkConnection conn, Vector3 pos)
    {
        SetSpawnState(pos, true);
    }

    void OnAliveChanged(bool oldValue, bool newValue)
    {
        rb.simulated = newValue;

        foreach (var s in sprites)
            s.enabled = newValue;
    }

    [Command]
    public void CmdSurrender()
    {
        Die();
    }

    [Command]
    public void CmdKickPlayer(uint targetNetId)
    {
        if (NetworkServer.spawned.TryGetValue(
            targetNetId,
            out NetworkIdentity id))
        {
            id.connectionToClient?.Disconnect();
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