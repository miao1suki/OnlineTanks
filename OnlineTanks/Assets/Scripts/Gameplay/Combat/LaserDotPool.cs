using System.Collections.Generic;
using UnityEngine;

public class LaserDotPool : MonoBehaviour
{
    public static LaserDotPool Instance { get; private set; }

    [SerializeField] GameObject laserDotPrefab;
    [SerializeField] int prewarm = 1000;

    readonly Queue<GameObject> pool = new Queue<GameObject>(1024);

    // 用一个list记录活跃点及其死亡时间
    struct ActiveDot
    {
        public GameObject go;
        public float dieTime;
    }

    readonly List<ActiveDot> active = new List<ActiveDot>(4096);

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < prewarm; i++)
        {
            var go = Instantiate(laserDotPrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    void Update()
    {
        // 统一回收：倒序移除
        float now = Time.time;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].dieTime > now) continue;

            var go = active[i].go;
            active.RemoveAt(i);

            if (go == null) continue;
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public void Spawn(Vector2 pos, float ttl, float scale = 1f)
    {
        if (laserDotPrefab == null) return;

        GameObject go = pool.Count > 0 ? pool.Dequeue() : Instantiate(laserDotPrefab, transform);

        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        go.SetActive(true);

        active.Add(new ActiveDot
        {
            go = go,
            dieTime = Time.time + Mathf.Max(0.01f, ttl)
        });
    }
}