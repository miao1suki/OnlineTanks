using System.Collections.Generic;
using UnityEngine;

public class WallVisibilityController : MonoBehaviour
{
    public static WallVisibilityController Instance;

    [Header("所有预放墙的父物体")]
    public Transform wallRoot;

    [Header("显示方式（二选一）")]
    [Range(0f, 1f)] public float showRatio = 0.35f;
    public int showCount = -1; // >=0 则使用固定数量，否则用 showRatio

    [Header("是否在Awake自动收集墙")]
    public bool autoCollectOnAwake = true;

    private readonly List<GameObject> walls = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (autoCollectOnAwake)
            CollectWalls();
    }

    public void CollectWalls()
    {
        walls.Clear();

        if (wallRoot == null)
        {
            Debug.LogError("WallVisibilityController: wallRoot 没有设置");
            return;
        }

        for (int i = 0; i < wallRoot.childCount; i++)
        {
            var go = wallRoot.GetChild(i).gameObject;
            walls.Add(go);
        }

        // 统一先隐藏一次，避免初始状态不一致
        HideAll();
    }

    public void HideAll()
    {
        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i] != null)
                walls[i].SetActive(false);
        }
    }

    public void ApplySeed(long seed)
    {
        // 1) 确保列表存在
        if (walls.Count == 0)
            CollectWalls();

        // 2) 先隐藏全部
        HideAll();

        // 3) 算出要显示哪些
        int count = walls.Count;
        List<int> indices = (showCount >= 0)
            ? SeedWallSelector.SelectIndices((ulong)seed, count, showCount)
            : SeedWallSelector.SelectIndicesByRatio((ulong)seed, count, showRatio);

        // 4) 显示对应墙
        for (int i = 0; i < indices.Count; i++)
        {
            int idx = indices[i];
            if (idx >= 0 && idx < walls.Count && walls[idx] != null)
                walls[idx].SetActive(true);
        }

        Debug.Log($"WallVisibilityController: seed={seed}, total={count}, show={indices.Count}");
    }
}