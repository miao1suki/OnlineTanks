using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class LaserBullet : Bullet
{
    [Header("Laser Settings")]
    [SerializeField] float laserSpeed = 80f;
    [SerializeField] float laserLife = 0.3f;

    [Header("Hit Detection")]
    [SerializeField] float hitRadius = 0.3f;      // 命中半径
    [SerializeField] LayerMask hitMask = ~0;      // 建议在Inspector指定只打PlayerHitBox所在层
    [SerializeField] bool recordPath = true;

    public float dotSpacing = 0.12f;
    public List<Vector2> path = new();

    PlayerController ownerPlayer;
    HashSet<PlayerHitBox> hits = new HashSet<PlayerHitBox>();

    Vector2 lastPos;
    bool hasLast;

    public GameObject laserDotPrefab;

    public void Fire(Vector2 dir, uint owner, PlayerController player)
    {
        path.Clear();
        hits.Clear();

        ownerId = owner;
        ownerPlayer = player;

        speed = laserSpeed;
        life = laserLife;

        GetComponent<SpriteRenderer>().enabled = false;

        hasLast = false;
        Launch(dir);
    }

    void FixedUpdate()
    {
        Vector2 now = transform.position;

        if (recordPath)
            path.Add(now);

        if (!hasLast)
        {
            lastPos = now;
            hasLast = true;
            return;
        }

        Vector2 delta = now - lastPos;
        float dist = delta.magnitude;

        // 连续检测：上一帧到这一帧整段扫过去
        if (dist > 0.0001f)
        {
            Vector2 dir = delta / dist;

            RaycastHit2D[] hs = Physics2D.CircleCastAll(
                lastPos,
                hitRadius,
                dir,
                dist,
                hitMask
            );

            foreach (var h in hs)
            {
                if (h.collider == null) continue;

                // HitBox是挂在boxPrefab上（不是玩家本体），这里直接取
                PlayerHitBox box = h.collider.GetComponent<PlayerHitBox>();
                if (box != null)
                    hits.Add(box);
            }
        }

        lastPos = now;
    }

    public override void ReturnPool()
    {
        foreach (var h in hits)
        {
            if (h != null)
                h.GetComponent<PlayerController>()?.Die();
        }

        Vector2[] dense = Densify(path, dotSpacing);

        ownerPlayer?.RpcRenderLaser(dense);
        RenderLaserDense(dense);

        hits.Clear();
        path.Clear();

        base.ReturnPool();
    }

    void RenderLaserDense(Vector2[] points)
    {
        if (points == null) return;
        foreach (var p in points)
            LaserDotPool.Instance?.Spawn(p, 0.3f);
    }

    static Vector2[] Densify(List<Vector2> raw, float spacing)
    {
        if (raw == null || raw.Count < 2)
            return raw == null ? new Vector2[0] : raw.ToArray();

        if (spacing <= 0.0001f) spacing = 0.0001f;

        List<Vector2> result = new List<Vector2>(raw.Count * 4);
        result.Add(raw[0]);

        for (int i = 1; i < raw.Count; i++)
        {
            Vector2 a = raw[i - 1];
            Vector2 b = raw[i];
            float dist = Vector2.Distance(a, b);
            if (dist < 0.0001f) continue;

            int steps = Mathf.CeilToInt(dist / spacing);
            for (int s = 1; s <= steps; s++)
            {
                float t = (float)s / steps;
                result.Add(Vector2.Lerp(a, b, t));
            }
        }
        return result.ToArray();
    }
}