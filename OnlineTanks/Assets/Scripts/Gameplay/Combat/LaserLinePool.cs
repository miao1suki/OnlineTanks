using System.Collections.Generic;
using UnityEngine;

public class LaserLinePool : MonoBehaviour
{
    public static LaserLinePool Instance { get; private set; }

    [SerializeField] LineRenderer prefab;
    [SerializeField] int prewarm = 32;

    readonly Queue<LineRenderer> pool = new();
    readonly List<(LineRenderer lr, float dieTime)> active = new();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < prewarm; i++)
        {
            var lr = Instantiate(prefab, transform);
            lr.gameObject.SetActive(false);
            pool.Enqueue(lr);
        }
    }

    void Update()
    {
        float now = Time.time;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].dieTime > now) continue;

            var lr = active[i].lr;
            active.RemoveAt(i);

            if (lr == null) continue;
            lr.gameObject.SetActive(false);
            pool.Enqueue(lr);
        }
    }

    public void Draw(Vector2[] corners, float ttl, float width)
    {
        if (corners == null || corners.Length < 2) return;

        var lr = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        lr.gameObject.SetActive(true);

        lr.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
            lr.SetPosition(i, new Vector3(corners[i].x, corners[i].y, 0f));

        lr.startWidth = width;
        lr.endWidth = width;

        active.Add((lr, Time.time + Mathf.Max(0.01f, ttl)));
    }
}