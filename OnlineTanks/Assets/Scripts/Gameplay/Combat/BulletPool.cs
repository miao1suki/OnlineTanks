using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [Header("普通子弹预制体")]
    public GameObject normalBulletPrefab;

    [Header("激光子弹预制体")]
    public GameObject laserBulletPrefab;

    public int bulletsPerPlayer = 20;

    // 普通子弹池
    Dictionary<uint, Queue<GameObject>> bulletPools = new();

    // 激光池
    Dictionary<uint, Queue<GameObject>> laserPools = new();

    // 每个玩家一个父节点
    Dictionary<uint, Transform> poolParents = new();

    // 活跃子弹
    Dictionary<ulong, Bullet> active = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    static ulong Key(uint ownerId, uint shotId)
    {
        return ((ulong)ownerId << 32) | shotId;
    }

    //====================================================
    // 普通子弹
    //====================================================

    public GameObject GetBullet(uint ownerId)
    {
        if (!bulletPools.ContainsKey(ownerId))
            CreateBulletPool(ownerId);

        Queue<GameObject> pool = bulletPools[ownerId];

        if (pool.Count > 0)
            return pool.Dequeue();

        GameObject extra =
            Instantiate(
                normalBulletPrefab,
                poolParents[ownerId]
            );

        extra.SetActive(false);

        extra.GetComponent<Bullet>().ownerId = ownerId;

        return extra;
    }

    void CreateBulletPool(uint ownerId)
    {
        Queue<GameObject> pool = new();

        GameObject parentObj =
            new GameObject($"PlayerBullets_{ownerId}");

        parentObj.transform.SetParent(transform);

        poolParents.Add(ownerId, parentObj.transform);

        for (int i = 0; i < bulletsPerPlayer; i++)
        {
            GameObject bullet =
                Instantiate(
                    normalBulletPrefab,
                    parentObj.transform
                );

            bullet.SetActive(false);

            bullet.GetComponent<Bullet>().ownerId = ownerId;

            pool.Enqueue(bullet);
        }

        bulletPools.Add(ownerId, pool);
    }

    //====================================================
    // 激光池
    //====================================================

    public GameObject GetLaser(uint ownerId)
    {
        if (!laserPools.ContainsKey(ownerId))
            CreateLaserPool(ownerId);

        Queue<GameObject> pool = laserPools[ownerId];

        if (pool.Count > 0)
            return pool.Dequeue();

        GameObject extra =
            Instantiate(
                laserBulletPrefab,
                transform
            );

        extra.SetActive(false);

        return extra;
    }

    void CreateLaserPool(uint ownerId)
    {
        Queue<GameObject> pool = new();

        for (int i = 0; i < 5; i++)
        {
            GameObject laser =
                Instantiate(
                    laserBulletPrefab,
                    transform
                );

            laser.SetActive(false);

            pool.Enqueue(laser);
        }

        laserPools.Add(ownerId, pool);
    }

    //====================================================
    // 活跃登记
    //====================================================

    public void RegisterActive(Bullet b)
    {
        if (b == null)
            return;

        active[Key(b.ownerId, b.shotId)] = b;
    }

    public void ReturnBulletByKey(
        uint ownerId,
        uint shotId)
    {
        ulong k = Key(ownerId, shotId);

        if (active.TryGetValue(k, out Bullet b))
        {
            active.Remove(k);

            if (b != null)
                b.ReturnPool();
        }
    }

    //====================================================
    // 回收
    //====================================================

    public void ReturnBullet(
        uint ownerId,
        GameObject bullet)
    {
        if (bullet == null)
            return;

        Bullet b = bullet.GetComponent<Bullet>();

        if (b != null)
        {
            active.Remove(
                Key(b.ownerId, b.shotId)
            );
        }

        bullet.SetActive(false);

        // 激光
        if (bullet.GetComponent<LaserBullet>() != null)
        {
            if (!laserPools.ContainsKey(ownerId))
            {
                Destroy(bullet);
                return;
            }

            laserPools[ownerId].Enqueue(bullet);

            return;
        }

        // 普通子弹
        if (!bulletPools.ContainsKey(ownerId))
        {
            Destroy(bullet);
            return;
        }

        bulletPools[ownerId].Enqueue(bullet);
    }
}