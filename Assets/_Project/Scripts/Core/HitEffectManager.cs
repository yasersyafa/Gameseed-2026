// Buat script baru: HitEffectManager.cs
using System.Collections;
using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance { get; private set; }

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowTimeScale    = 0.15f;  // seberapa lambat
    [SerializeField] private float slowDuration     = 0.2f;   // berapa lama lambat
    [SerializeField] private float recoverySpeed    = 8f;     // seberapa cepat balik normal

    private Coroutine _activeRoutine;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerKillEffect()
    {
        // Kalau ada efek yang sedang jalan, stop dulu
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(SlowMotionRoutine());
    }

    private IEnumerator SlowMotionRoutine()
    {
        // Langsung snap ke slow
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Tahan selama slowDuration (pakai unscaled time
        // supaya tidak ikut melambat)
        yield return new WaitForSecondsRealtime(slowDuration);

        // Balik ke normal secara smooth
        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.Min(
                Time.timeScale + recoverySpeed * Time.unscaledDeltaTime,
                1f
            );
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        // Pastikan benar-benar reset ke normal
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        _activeRoutine      = null;
    }
}