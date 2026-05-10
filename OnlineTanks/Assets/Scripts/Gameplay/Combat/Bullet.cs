using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float speed = 20;
    public float life = 5f;

    public uint ownerId;
    public uint shotId; // 本次发射编号（同一个owner下唯一）

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(uint owner, uint shot)
    {
        ownerId = owner;
        shotId = shot;
    }

    public void Launch(Vector2 dir)
    {
        rb.linearVelocity = dir.normalized * speed;

        CancelInvoke();
        Invoke(nameof(ReturnPool), life);
    }

    public void ReturnPool()
    {
        rb.linearVelocity = Vector2.zero;
        BulletPool.Instance.ReturnBullet(ownerId, gameObject);
    }
}