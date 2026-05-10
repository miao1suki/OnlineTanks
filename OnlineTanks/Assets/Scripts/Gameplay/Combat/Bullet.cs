using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float speed = 20;
    public float life = 5f;

    public uint ownerId;
    public uint shotId; // 本次发射编号（同一个owner下唯一）

    [HideInInspector]
    public bool isBigBullet;

    public bool ignoreSelfHit = false;

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
        rb.linearVelocity =
            dir.normalized * speed;

        if (isBigBullet)
        {
            transform.localScale = Vector3.one * 10f;
        }
        else
        {
            transform.localScale = Vector3.one * 3f;
        }

        CancelInvoke();

        Invoke(nameof(ReturnPool), life);
    }

    public virtual void ReturnPool()
    {
        CancelInvoke();

        rb.linearVelocity = Vector2.zero;

        isBigBullet = false;

        transform.localScale = Vector3.one * 3f;

        BulletPool.Instance.ReturnBullet(
            ownerId,
            gameObject
        );
    }
}