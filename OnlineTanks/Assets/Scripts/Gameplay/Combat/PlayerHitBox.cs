using Mirror;
using UnityEngine;

public class PlayerHitBox : NetworkBehaviour
{
    [SyncVar] public uint ownerId;

    PlayerController owner;
    Collider2D col;

    public void Bind(PlayerController player)
    {
        owner = player;
        ownerId = player.netId;

        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!isServer) return;
        if (owner == null) return;

        transform.position = owner.transform.position;
        transform.rotation = owner.transform.rotation;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer) return;

        Bullet bullet = other.GetComponentInParent<Bullet>();
        if (bullet == null) return;

        if (owner == null || !owner.isAlive) return;

        owner.Die();

        // 先记下key（避免 ReturnPool 后取不到）
        uint bidOwner = bullet.ownerId;
        uint bidShot = bullet.shotId;

        bullet.ReturnPool();

        // 通知所有客户端：把这颗显示子弹也回收
        RpcReturnBullet(bidOwner, bidShot);
    }

    [ClientRpc]
    void RpcReturnBullet(uint ownerId, uint shotId)
    {
        // host 的客户端那边也会走，但 host 的显示子弹通常不在 server 分支里生成；
        // 即使调用也没事：找不到就忽略。
        BulletPool.Instance?.ReturnBulletByKey(ownerId, shotId);
    }
}