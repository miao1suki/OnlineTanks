using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserDotPool : MonoBehaviour
{
    public static LaserDotPool Instance { get; private set; }

    [SerializeField] GameObject laserDotPrefab;
    [SerializeField] int prewarm = 1000;

    readonly Queue<GameObject> pool = new Queue<GameObject>(1024);

    void Awake()
    {
        Instance = this;

        // 只在Game场景预热
        for (int i = 0; i < prewarm; i++)
        {
            var go = Instantiate(laserDotPrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public void Spawn(Vector2 pos, float ttl)
    {
        if (laserDotPrefab == null) return;

        GameObject go = pool.Count > 0 ? pool.Dequeue() : Instantiate(laserDotPrefab, transform);

        go.transform.position = pos;
        go.SetActive(true);

        // 用协程回收（不会Destroy）
        StartCoroutine(ReturnAfter(go, ttl));
    }

    IEnumerator ReturnAfter(GameObject go, float ttl)
    {
        yield return new WaitForSeconds(ttl);

        if (go == null) yield break;

        go.SetActive(false);
        pool.Enqueue(go);
    }
}