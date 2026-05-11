using Mirror;
using System.Collections;
using System.Collections.Generic;
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

    public GameObject laserDotPrefab;   // Inspector 拖入
    public float laserDotLife = 0.3f;

    [SyncVar(hook = nameof(OnAliveChanged))]
    public bool isAlive = true;

    [SyncVar(hook = nameof(OnHostChanged))]
    public bool isHostPlayer;

    [SyncVar(hook = nameof(OnFireModeChanged))]
    public FireMode currentFireMode = FireMode.Normal;
    bool isBursting;

    uint shotSeq; // 只在服务器递增即可

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

        // 结算/准备/生成阶段都不允许操作
        if (MatchManager.Instance == null || MatchManager.Instance.currentState != RoomState.Playing)
            return;


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

            //播放射击音频
            AudioEffectManager.Instance?.PlayShootByMode(currentFireMode);

            CmdTryFire(firePoint.position, look);
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

    void OnFireModeChanged(FireMode oldMode, FireMode newMode)
    {
        Debug.Log($"武器模式切换: {newMode}");
    }

    void OnHostChanged(bool oldValue, bool newValue)
    {
        RoomCanvasController.Instance?.RefreshPlayerList(
            GetCurrentPlayers()
        );
    }

    List<PlayerController> GetCurrentPlayers()
    {
        List<PlayerController> list =
            new List<PlayerController>();

        foreach (uint id in MatchManager.Instance.allPlayerIds)
        {
            if (NetworkClient.spawned.TryGetValue(
                id,
                out NetworkIdentity identity))
            {
                PlayerController p =
                    identity.GetComponent<PlayerController>();

                if (p != null)
                    list.Add(p);
            }
        }

        return list;
    }

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        inputLocked = true;

        rb.linearVelocity = Vector2.zero;

        rb.simulated = false;

        currentFireMode = FireMode.Normal;

        foreach (var s in sprites)
        {
            s.enabled = false;
        }

        if (isServer)
        {
            // 所有端震动（死亡震动更大、更久）
            MatchManager.Instance?.ServerShakeAll(0.65f, 0.85f);

            MatchManager.Instance.OnPlayerDead(this);
        }
           
    }

    public void Respawn(Vector3 pos)
    {
        if (!isServer) return;

        // 重置局内武器状态
        currentFireMode = FireMode.Normal;
        isBursting = false;

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

        // 从活着 -> 死亡：播爆炸特效 + 音效
        if (oldValue == true && newValue == false)
        {
            VFXManager.Instance?.PlayExplosion(
                transform.position,
                10f,   // 特效大小
                2f    // 播放时间
            );

            AudioEffectManager.Instance?.PlayExplosion();
        }
    }

    [Command]
    public void CmdSurrender()
    {
        Die();
    }


    [Command]
    public void CmdKickPlayer(uint targetNetId)
    {
        if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity id))
            return;

        NetworkConnectionToClient targetConn = id.connectionToClient;

        if (targetConn == null)
            return;

        StartCoroutine(KickFlow(targetConn));
    }

    IEnumerator KickFlow(NetworkConnectionToClient conn)
    {
        // 等一帧，确保RPC发送出去
        yield return null;

        TargetKicked(conn, "您被移出房间");

        // 保证客户端收到
        yield return new WaitForSeconds(0.1f);

        conn.Disconnect();
    }

    [TargetRpc]
    void TargetKicked(
     NetworkConnection conn,
     string msg
 )
    {
        KickToastUI.Instance?.Show(msg, UIContext.Lobby);
    }
    public override void OnStopClient()
    {
        base.OnStopClient();

        MatchManager.Instance?.UnregisterPlayer(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        MatchManager.Instance?.UnregisterPlayer(this);
    }

    [Command]
    void CmdTryFire(Vector2 pos, Vector2 dir)
    {
        switch (currentFireMode)
        {
            case FireMode.Normal:
                ServerFireSingle(pos, dir, false);
                break;

            case FireMode.Triple:
                FireTriple(pos, dir);
                break;

            case FireMode.Burst:
                if (!isBursting)
                    StartCoroutine(BurstRoutine(pos, dir));
                break;

            case FireMode.BigBullet:
                ServerFireSingle(pos, dir, true);
                break;

            case FireMode.Laser:
                FireLaser(pos, dir);
                break;
        }
    }

    void ServerFireSingle(Vector2 pos, Vector2 dir, bool big)
    {
        uint shotId = ++shotSeq;

        GameObject bullet = BulletPool.Instance.GetBullet(netId);

        bullet.transform.position = pos;
        bullet.transform.rotation =
            Quaternion.LookRotation(Vector3.forward, dir);

        bullet.SetActive(true);

        Bullet b = bullet.GetComponent<Bullet>();

        b.Init(netId, shotId);

        // 巨型子弹免疫自伤
        b.isBigBullet = big;
        b.ignoreSelfHit = big;


        BulletPool.Instance.RegisterActive(b);

        b.Launch(dir);

        // 让所有客户端播放“这次发射的武器音效”
        RpcPlayShootSfx(big ? FireMode.BigBullet : currentFireMode);

        RpcSpawnBullet(
            pos,
            dir,
            netId,
            shotId,
            big
        );

        if (big)
        {
            MatchManager.Instance?.ServerShakeAll(0.5f, 0.42f);
        }
    }

    void FireTriple(Vector2 pos, Vector2 dir)
    {
        Vector2 leftDir =
            Quaternion.Euler(0, 0, 15) * dir;

        Vector2 rightDir =
            Quaternion.Euler(0, 0, -15) * dir;

        ServerFireSingle(pos, dir, false);

        ServerFireSingle(
            pos + leftDir * 0.3f,
            leftDir,
            false
        );

        ServerFireSingle(
            pos + rightDir * 0.3f,
            rightDir,
            false
        );
    }


    IEnumerator BurstRoutine(Vector2 pos, Vector2 dir)
    {
        isBursting = true;

        float timer = 1f;

        while (timer > 0)
        {
            ServerFireSingle(
                firePoint.position,
                dir,
                false
            );

            yield return new WaitForSeconds(0.05f);

            timer -= 0.05f;
        }

        isBursting = false;
    }

    void FireLaser(Vector2 pos, Vector2 dir)
    {
        // 激光也广播音效
        RpcPlayShootSfx(FireMode.Laser);

        GameObject bullet =
            BulletPool.Instance.GetLaser(netId);

        bullet.transform.position = pos;

        bullet.SetActive(true);

        LaserBullet laser =
            bullet.GetComponent<LaserBullet>();

        laser.Fire(dir ,netId ,this);
    }

    [ClientRpc]
    public void RpcRenderLaser(Vector2[] points)
    {
        if (points == null) return;

        foreach (var p in points)
            LaserDotPool.Instance?.Spawn(p, 0.3f);
    }


    [ClientRpc]
    void RpcSpawnBullet(
     Vector2 pos,
     Vector2 dir,
     uint ownerId,
     uint shotId,
     bool big)
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

        Bullet b = bullet.GetComponent<Bullet>();

        b.Init(ownerId, shotId);

        b.isBigBullet = big;

        BulletPool.Instance.RegisterActive(b);

        b.Launch(dir);
    }

    [ClientRpc]
    void RpcPlayShootSfx(FireMode mode)
    {
        // 本地玩家已经在Update里播过一次了，避免重复
        if (isLocalPlayer) return;

        AudioEffectManager.Instance?.PlayShootByMode(mode);
    }

    Vector2 CalculateLookDirection()
    {
#if UNITY_ANDROID || UNITY_IOS
    // 用坦克朝向当作射击方向（不需要鼠标）
    float ang = -currentAngle; // 你 rotation 用的是 -currentAngle
    float rad = ang * Mathf.Deg2Rad;
    return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)).normalized;
#else
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return (worldPos - (Vector2)transform.position).normalized;
#endif
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