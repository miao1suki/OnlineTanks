using System;
using System.Collections.Generic;
using UnityEngine;

public static class SeedWallSelector
{
    /// <summary>
    /// 通过 seed 在 [0, wallCount) 中确定性选出 showCount 个索引（不重复）
    /// </summary>
    public static List<int> SelectIndices(ulong seed, int wallCount, int showCount)
    {
        var result = new List<int>(Mathf.Clamp(showCount, 0, wallCount));
        if (wallCount <= 0) return result;

        showCount = Mathf.Clamp(showCount, 0, wallCount);

        // Floyd's algorithm：O(k) 选 k 个不重复数字，确定性，省内存
        var rng = new DeterministicRng(seed);
        var chosen = new HashSet<int>();

        // Floyd: for j in [n-k .. n-1], pick t in [0..j], add t if not exist else add j
        for (int j = wallCount - showCount; j < wallCount; j++)
        {
            int t = rng.NextInt(0, j + 1);
            if (!chosen.Add(t))
                chosen.Add(j);
        }

        result.AddRange(chosen);
        return result;
    }

    /// <summary>
    /// 通过 seed + 比例选墙
    /// </summary>
    public static List<int> SelectIndicesByRatio(ulong seed, int wallCount, float showRatio)
    {
        int showCount = Mathf.RoundToInt(wallCount * Mathf.Clamp01(showRatio));
        return SelectIndices(seed, wallCount, showCount);
    }

    // ===== 确定性 RNG：SplitMix64 =====
    private struct DeterministicRng
    {
        private ulong state;

        public DeterministicRng(ulong seed)
        {
            state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        private ulong NextU64()
        {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            ulong r = NextU64();
            int range = maxExclusive - minInclusive;
            return minInclusive + (int)(r % (ulong)range);
        }
    }
}