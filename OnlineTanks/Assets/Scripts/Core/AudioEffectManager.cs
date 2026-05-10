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

    // ===== 新增：拾取音效 =====
    [Header("拾取音效")]
    public AudioClip pickupClip;

    // ===== 不同武器射击音效（可选，不配就回落到 shootClip）=====
    [Header("武器射击音效")]
    public AudioClip shootTripleClip;
    public AudioClip shootBurstClip;
    public AudioClip shootBigClip;
    public AudioClip shootLaserClip;

    AudioSource source;

    float nextExplosionTime;
    float nextShootTime;

    // ===== 拾取的节流 =====
    float nextPickupTime;

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

    // ===== 拾取播放 =====
    public void PlayPickup()
    {
        if (pickupClip == null) return;
        if (Time.unscaledTime < nextPickupTime) return;

        nextPickupTime = Time.unscaledTime + minInterval;
        source.PlayOneShot(pickupClip);
    }

    // ===== 按武器模式播放射击 =====
    public void PlayShootByMode(FireMode mode)
    {
        if (Time.unscaledTime < nextShootTime) return;

        AudioClip clip = shootClip;

        switch (mode)
        {
            case FireMode.Triple:
                if (shootTripleClip != null) clip = shootTripleClip;
                break;

            case FireMode.Burst:
                if (shootBurstClip != null) clip = shootBurstClip;
                break;

            case FireMode.BigBullet:
                if (shootBigClip != null) clip = shootBigClip;
                break;

            case FireMode.Laser:
                if (shootLaserClip != null) clip = shootLaserClip;
                break;
        }

        if (clip == null) return;

        nextShootTime = Time.unscaledTime + minInterval;
        source.PlayOneShot(clip);
    }
}