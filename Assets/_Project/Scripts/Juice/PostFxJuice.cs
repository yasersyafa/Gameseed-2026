using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pulse vignette + chromatic aberration di URP Volume saat hit/elim/parry/gameOver.
/// Bikin local Volume sendiri (Priority tinggi) supaya tidak overwrite Volume scene.
/// Pakai unscaledDeltaTime — tetap jalan saat hitstop.
/// </summary>
public class PostFxJuice : MonoBehaviour
{
    [Header("Pulse Profiles")]
    [SerializeField] private float hitVignette        = 0.45f;
    [SerializeField] private float hitChromatic       = 0.6f;
    [SerializeField] private float hitDuration        = 0.25f;

    [SerializeField] private float elimVignette       = 0.55f;
    [SerializeField] private float elimChromatic      = 0.85f;
    [SerializeField] private float elimDuration       = 0.6f;

    [SerializeField] private float parryVignette      = 0.35f;
    [SerializeField] private float parryChromatic     = 0.9f;
    [SerializeField] private float parryDuration      = 0.18f;

    [SerializeField] private float winVignette        = 0.2f;
    [SerializeField] private float winChromatic       = 0.5f;
    [SerializeField] private float winDuration        = 1.4f;

    [SerializeField] private Color vignetteColor      = Color.black;

    private Volume               _volume;
    private VolumeProfile        _profile;
    private Vignette             _vignette;
    private ChromaticAberration  _chromatic;
    private Coroutine            _pulseRoutine;

    private void Awake()
    {
        BuildVolume();
    }

    private void BuildVolume()
    {
        _profile = ScriptableObject.CreateInstance<VolumeProfile>();

        _vignette = _profile.Add<Vignette>(true);
        _vignette.color.Override(vignetteColor);
        _vignette.intensity.Override(0f);
        _vignette.smoothness.Override(0.4f);

        _chromatic = _profile.Add<ChromaticAberration>(true);
        _chromatic.intensity.Override(0f);

        _volume                = gameObject.AddComponent<Volume>();
        _volume.isGlobal       = true;
        _volume.priority       = 100;
        _volume.weight         = 1f;
        _volume.sharedProfile  = _profile;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerHit        += HandleHit;
        GameEvents.OnPlayerEliminated += HandleElim;
        GameEvents.OnBoomerangParried += HandleParry;
        GameEvents.OnGameOver         += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHit        -= HandleHit;
        GameEvents.OnPlayerEliminated -= HandleElim;
        GameEvents.OnBoomerangParried -= HandleParry;
        GameEvents.OnGameOver         -= HandleGameOver;
    }

    private void HandleHit(int i, PlayerController c)   => Pulse(hitVignette, hitChromatic, hitDuration);
    private void HandleElim(int i, PlayerController c)  => Pulse(elimVignette, elimChromatic, elimDuration);
    private void HandleParry(int i)                     => Pulse(parryVignette, parryChromatic, parryDuration);
    private void HandleGameOver(int i)                  => Pulse(winVignette, winChromatic, winDuration);

    private void Pulse(float vIntensity, float cIntensity, float duration)
    {
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine(vIntensity, cIntensity, duration));
    }

    private IEnumerator PulseRoutine(float vIntensity, float cIntensity, float duration)
    {
        const float attack = 0.06f;

        // Attack
        float t = 0f;
        while (t < attack)
        {
            t += Time.unscaledDeltaTime;
            float k = t / attack;
            _vignette.intensity.value  = Mathf.Lerp(0f, vIntensity, k);
            _chromatic.intensity.value = Mathf.Lerp(0f, cIntensity, k);
            yield return null;
        }
        _vignette.intensity.value  = vIntensity;
        _chromatic.intensity.value = cIntensity;

        // Decay
        float decay = Mathf.Max(0.05f, duration - attack);
        t = 0f;
        while (t < decay)
        {
            t += Time.unscaledDeltaTime;
            float k = t / decay;
            _vignette.intensity.value  = Mathf.Lerp(vIntensity, 0f, k);
            _chromatic.intensity.value = Mathf.Lerp(cIntensity, 0f, k);
            yield return null;
        }
        _vignette.intensity.value  = 0f;
        _chromatic.intensity.value = 0f;
        _pulseRoutine = null;
    }
}
