using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    public GameObject bulletPrefab;
    public int bulletsPerPlayer = 20;

    Dictionary<uint, Queue<GameObject>> pools = new();
    Dictionary<uint, Transform> poolParents = new();

    // 活跃子弹索引（用于精准回收）
    Dictionary<ulong, Bullet> active = new(); // key = (ownerId<<32) | shotId

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    static ulong Key(uint ownerId, uint shotId) => ((ulong)ownerId << 32) | shotId;

    public GameObject GetBullet(uint ownerId)
    {
        if (!pools.ContainsKey(ownerId))
            CreatePool(ownerId);

        Queue<GameObject> pool = pools[ownerId];

        if (pool.Count > 0)
            return pool.Dequeue();

        GameObject extra = Instantiate(bulletPrefab, poolParents[ownerId]);
        extra.SetActive(false);
        extra.GetComponent<Bullet>().ownerId = ownerId;
        return extra;
    }

    void CreatePool(uint ownerId)
    {
        Queue<GameObject> pool = new();

        //每个玩家的子弹有独立父物体
        GameObject parentObj = new GameObject($"PlayerBullets_{ownerId}");
        parentObj.transform.SetParent(transform);
        poolParents.Add(ownerId, parentObj.transform);

        for (int i = 0; i < bulletsPerPlayer; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, parentObj.transform);
            bullet.SetActive(false);
            bullet.GetComponent<Bullet>().ownerId = ownerId;
            pool.Enqueue(bullet);
        }

        pools.Add(ownerId, pool);
    }

    // 登记活跃子弹
    public void RegisterActive(Bullet b)
    {
        if (b == null) return;
        active[Key(b.ownerId, b.shotId)] = b;
    }

    // 按 key 回收（客户端命中回收用）
    public void ReturnBulletByKey(uint ownerId, uint shotId)
    {
        ulong k = Key(ownerId, shotId);

        if (active.TryGetValue(k, out Bullet b) && b != null)
        {
            active.Remove(k);
            b.ReturnPool();
        }
    }

    public void ReturnBullet(uint ownerId, GameObject bullet)
    {
        if (bullet == null) return;

        // 回收时把 active 里也清掉
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
            active.Remove(Key(b.ownerId, b.shotId));

        bullet.SetActive(false);

        if (!pools.ContainsKey(ownerId))
        {
            Destroy(bullet);
            return;
        }

        pools[ownerId].Enqueue(bullet);
    }
}