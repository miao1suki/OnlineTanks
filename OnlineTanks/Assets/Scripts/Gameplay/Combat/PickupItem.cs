using Mirror;
using UnityEngine;

public class PickupItem : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTypeChanged))]
    public PickupType pickupType;

    public SpriteRenderer sr;

    public Sprite tripleSprite;
    public Sprite burstSprite;
    public Sprite bigSprite;
    public Sprite laserSprite;

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVisual(pickupType); // 保底刷新
    }

    void OnTypeChanged(PickupType oldType, PickupType newType)
    {
        ApplyVisual(newType);
    }

    void ApplyVisual(PickupType type)
    {
        switch (type)
        {
            case PickupType.TripleShot:
                sr.sprite = tripleSprite;
                break;
            case PickupType.BurstShot:
                sr.sprite = burstSprite;
                break;
            case PickupType.BigBullet:
                sr.sprite = bigSprite;
                break;
            case PickupType.Laser:
                sr.sprite = laserSprite;
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer) return;

        PlayerHitBox box =
            other.GetComponent<PlayerHitBox>();

        if (box == null) return;

        PlayerController player =
            MatchManager.Instance.GetPlayer(box.ownerId);

        if (player == null) return;

        switch (pickupType)
        {
            case PickupType.TripleShot:
                player.currentFireMode =
                    FireMode.Triple;
                break;

            case PickupType.BurstShot:
                player.currentFireMode =
                    FireMode.Burst;
                break;

            case PickupType.BigBullet:
                player.currentFireMode =
                    FireMode.BigBullet;
                break;

            case PickupType.Laser:
                player.currentFireMode =
                    FireMode.Laser;
                break;
        }

        // 播放拾取音效（所有客户端都能听见）
        RpcPlayPickupSfx();

        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcPlayPickupSfx()
    {
        AudioEffectManager.Instance?.PlayPickup();
    }
}