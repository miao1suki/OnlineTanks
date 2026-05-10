using UnityEngine;

public class AudioEffectManager : MonoBehaviour
{
    public static AudioEffectManager Instance;

    [Header("爆炸音效")]
    public AudioClip explosionClip;

    [Header("射击音效")]
    public AudioClip shootClip;

    [Header("最小播放间隔（秒）")]
    public float minInterval = 0.2f;

    AudioSource source;

    float nextExplosionTime;
    float nextShootTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public void PlayExplosion()
    {
        if (explosionClip == null) return;
        if (Time.unscaledTime < nextExplosionTime) return;

        nextExplosionTime = Time.unscaledTime + minInterval;
        source.PlayOneShot(explosionClip);
    }

    public void PlayShoot()
    {
        if (shootClip == null) return;
        if (Time.unscaledTime < nextShootTime) return;

        nextShootTime = Time.unscaledTime + minInterval;
        source.PlayOneShot(shootClip);
    }
}