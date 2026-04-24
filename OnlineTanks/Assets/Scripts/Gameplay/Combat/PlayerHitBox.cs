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
        if (bullet.ownerId == owner.netId) return;

        owner.Die();
        bullet.ReturnPool();
    }
}