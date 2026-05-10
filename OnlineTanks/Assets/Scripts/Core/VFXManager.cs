using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("爆炸特效预制体")]
    public GameObject explosionPrefab;

    [Header("初始对象池数量")]
    public int poolCount = 10;

    readonly Queue<GameObject> pool = new Queue<GameObject>();
    bool poolReady;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        SceneManager.activeSceneChanged -= OnSceneChanged;
        StopAllCoroutines();
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // 只在 Game 场景建池
        if (newScene.name == "Game")
        {
            EnsurePool();
        }
        else
        {
            // 可选：离开 Game 就清空（不清也行）
            // ClearPool();
            poolReady = false;
        }
    }

    void EnsurePool()
    {
        if (poolReady) return;

        ClearPool(); // 防止重复
        CreatePool();

        poolReady = true;
    }

    void CreatePool()
    {
        if (explosionPrefab == null) return;

        for (int i = 0; i < poolCount; i++)
        {
            var obj = Instantiate(explosionPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    void ClearPool()
    {
        while (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            if (obj != null) Destroy(obj);
        }
    }

    GameObject GetEffect()
    {
        if (!poolReady) return null;
        if (explosionPrefab == null) return null;

        while (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            if (obj != null) return obj;
        }

        var extra = Instantiate(explosionPrefab);
        extra.SetActive(false);
        return extra;
    }

    public void PlayExplosion(Vector3 position, float scale, float lifeTime)
    {
        if (Application.isBatchMode) return;
        // Game 场景才允许播放
        if (!poolReady) return;

        StartCoroutine(PlayRoutine(position, scale, lifeTime));
    }

    IEnumerator PlayRoutine(Vector3 position, float scale, float lifeTime)
    {
        GameObject obj = GetEffect();
        if (obj == null) yield break;

        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * scale;

        obj.SetActive(true);

        var systems = obj.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null) continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }

        if (lifeTime > 0f)
            yield return new WaitForSecondsRealtime(lifeTime);

        if (obj == null) yield break;

        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}