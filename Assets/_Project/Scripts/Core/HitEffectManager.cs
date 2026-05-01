using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-trigger time-feel manager. Slow-mo + true hit-stop variants.
/// Subscribes ke: PlayerHit (medium slowmo), PlayerEliminated (heavy slowmo),
/// BoomerangParried (short hit-stop), BoomerangWallBounce (no-op default).
/// </summary>
public class HitEffectManager : MonoBehaviour
{
    public enum SlowMoIntensity { Light, Medium, Heavy }

    [Header("Slow-Mo Profiles")]
    [SerializeField] private float lightTimeScale  = 0.35f;
    [SerializeField] private float lightDuration   = 0.12f;
    [SerializeField] private float mediumTimeScale = 0.15f;
    [SerializeField] private float mediumDuration  = 0.20f;
    [SerializeField] private float heavyTimeScale  = 0.08f;
    [SerializeField] private float heavyDuration   = 0.30f;

    [Header("Recovery")]
    [SerializeField] private float recoverySpeed = 8f;

    [Header("Hit-Stop (true freeze)")]
    [SerializeField] private float parryStopDuration = 0.08f;

    [Header("Win Flourish")]
    [SerializeField] private float winFlourishScale    = 0.08f;
    [SerializeField] private float winFlourishDuration = 1.6f;
    [SerializeField] private float roundEndFreeze      = 0.18f;

    private Coroutine _activeRoutine;

    private void OnEnable()
    {
        GameEvents.OnPlayerHit         += HandlePlayerHit;
        GameEvents.OnPlayerEliminated  += HandlePlayerEliminated;
        GameEvents.OnBoomerangParried  += HandleParry;
        GameEvents.OnRoundEnded        += HandleRoundEnd;
        GameEvents.OnGameOver          += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHit         -= HandlePlayerHit;
        GameEvents.OnPlayerEliminated  -= HandlePlayerEliminated;
        GameEvents.OnBoomerangParried  -= HandleParry;
        GameEvents.OnRoundEnded        -= HandleRoundEnd;
        GameEvents.OnGameOver          -= HandleGameOver;
    }

    private void HandlePlayerHit(int index, PlayerController controller)
        => TriggerSlowMo(SlowMoIntensity.Medium);

    private void HandlePlayerEliminated(int index, PlayerController controller)
        => TriggerSlowMo(SlowMoIntensity.Heavy);

    private void HandleParry(int index)
        => TriggerHitStop(parryStopDuration);

    private void HandleRoundEnd(int winnerIndex)
        => TriggerHitStop(roundEndFreeze);

    private void HandleGameOver(int winnerIndex)
        => TriggerWinFlourish();

    public void TriggerWinFlourish()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(SlowMoRoutine(winFlourishScale, winFlourishDuration));
    }

    /// <summary>Back-compat wrapper — old callsites map to Heavy.</summary>
    public void TriggerKillEffect() => TriggerSlowMo(SlowMoIntensity.Heavy);

    public void TriggerSlowMo(SlowMoIntensity intensity)
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.SlowMoEnabled) return;
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);

        float scale = mediumTimeScale, dur = mediumDuration;
        switch (intensity)
        {
            case SlowMoIntensity.Light:  scale = lightTimeScale;  dur = lightDuration;  break;
            case SlowMoIntensity.Medium: scale = mediumTimeScale; dur = mediumDuration; break;
            case SlowMoIntensity.Heavy:  scale = heavyTimeScale;  dur = heavyDuration;  break;
        }
        _activeRoutine = StartCoroutine(SlowMoRoutine(scale, dur));
    }

    public void TriggerHitStop(float duration)
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.SlowMoEnabled) return;
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator SlowMoRoutine(float targetScale, float holdDuration)
    {
        Time.timeScale      = targetScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(holdDuration);

        while (Time.timeScale < 1f)
        {
            Time.timeScale = Mathf.Min(
                Time.timeScale + recoverySpeed * Time.unscaledDeltaTime,
                1f
            );
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        _activeRoutine      = null;
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale      = 0.001f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        _activeRoutine      = null;
    }
}
