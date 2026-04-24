using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    public GameObject bulletPrefab;

    public int bulletsPerPlayer = 20;

    Dictionary<uint, Queue<GameObject>> pools =
        new Dictionary<uint, Queue<GameObject>>();

    Dictionary<uint, Transform> poolParents =
        new Dictionary<uint, Transform>();


    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    public GameObject GetBullet(uint ownerId)
    {
        if (!pools.ContainsKey(ownerId))
        {
            CreatePool(ownerId);
        }

        Queue<GameObject> pool = pools[ownerId];

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // 池空扩容
        GameObject extra =
            Instantiate(
                bulletPrefab,
                poolParents[ownerId]
            );

        extra.SetActive(false);

        extra.GetComponent<Bullet>().ownerId =
            ownerId;

        return extra;
    }


    void CreatePool(uint ownerId)
    {
        Queue<GameObject> pool =
            new Queue<GameObject>();


        // 每个玩家自己的父物体
        GameObject parentObj =
            new GameObject(
                $"PlayerBullets_{ownerId}"
            );

        parentObj.transform.SetParent(
            transform
        );

        poolParents.Add(
            ownerId,
            parentObj.transform
        );


        for (int i = 0; i < bulletsPerPlayer; i++)
        {
            GameObject bullet =
                Instantiate(
                    bulletPrefab,
                    parentObj.transform
                );

            bullet.SetActive(false);

            bullet.GetComponent<Bullet>().ownerId =
                ownerId;

            pool.Enqueue(bullet);
        }

        pools.Add(ownerId, pool);
    }


    public void ReturnBullet(
        uint ownerId,
        GameObject bullet
    )
    {
        bullet.SetActive(false);

        if (!pools.ContainsKey(ownerId))
        {
            Destroy(bullet);
            return;
        }

        pools[ownerId].Enqueue(bullet);
    }
}