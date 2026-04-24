using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float speed = 20;
    public float life = 5f;
    public uint ownerId;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 dir)
    {
        rb.linearVelocity = dir.normalized * speed;

        CancelInvoke();

        Invoke(
          nameof(ReturnPool),
          life
        );
    }
    void OnCollisionEnter2D(Collision2D c)
    {
        //Ç½Ìå×ßPhysicsMaterial·´µ¯
    }

    public void ReturnPool()
    {
        rb.linearVelocity = Vector2.zero;

        BulletPool.Instance.ReturnBullet(
            ownerId,
            gameObject
        );
    }
}