using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Pusat playback audio. Subscribe GameEvents otomatis untuk SFX gameplay
/// (throw, catch, hit, dash, death, countdown, round win). Play(id) untuk
/// trigger manual dari sistem lain.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Library")]
    [SerializeField] private AudioCueLibrary library;

    [Header("Mixer")]
    [SerializeField] private AudioMixer       mixer;
    [SerializeField] private AudioMixerGroup  sfxGroup;
    [SerializeField] private AudioMixerGroup  musicGroup;
    [SerializeField] private AudioMixerGroup  uiGroup;

    [Header("Mixer Param Names")]
    [SerializeField] private string sfxVolParam   = "SFXVol";
    [SerializeField] private string musicVolParam = "MusicVol";
    [SerializeField] private string uiVolParam    = "UIVol";

    [Header("SFX Pool")]
    [SerializeField] private int   sfxPoolSize = 16;
    [SerializeField] private float minRetriggerInterval = 0.02f;

    private readonly Queue<AudioSource> _sfxPool = new();
    private AudioSource _musicSource;

    // dedup retrigger spam (mis. wall bounce burst)
    private readonly Dictionary<AudioCueId, float> _lastPlayTime = new();

    // mixer-less volume fallback
    private float _sfxVolume   = 1f;
    private float _musicVolume = 1f;
    private float _uiVolume    = 1f;

    private void Awake()
    {
        BuildPool();
    }

    private void BuildPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SfxVoice_{i}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake     = false;
            src.spatialBlend    = 0f;
            src.outputAudioMixerGroup = sfxGroup;
            _sfxPool.Enqueue(src);
        }

        var musicGo = new GameObject("MusicSource");
        musicGo.transform.SetParent(transform, false);
        _musicSource = musicGo.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop        = true;
        _musicSource.outputAudioMixerGroup = musicGroup;
    }

    private void OnEnable()
    {
        GameEvents.OnBoomerangThrown    += HandleThrow;
        GameEvents.OnBoomerangCaught    += HandleCatch;
        GameEvents.OnBoomerangWallBounce+= HandleWallBounce;
        GameEvents.OnBoomerangParried   += HandleParry;
        GameEvents.OnPlayerDashed       += HandleDash;
        GameEvents.OnPlayerHit          += HandleHit;
        GameEvents.OnPlayerEliminated   += HandleDeath;
        GameEvents.OnCountdownTick      += HandleCountdown;
        GameEvents.OnRoundEnded         += HandleRoundEnd;
        GameEvents.OnGameOver           += HandleGameOver;
        GameEvents.OnPickupSpawned      += HandlePickupSpawned;
        GameEvents.OnPickupCollected    += HandlePickupCollected;
        GameEvents.OnShieldAbsorbed     += HandleShieldAbsorbed;
        GameEvents.OnChargeStarted      += HandleChargeStarted;
        GameEvents.OnChargeReleased     += HandleChargeReleased;
    }

    private void OnDisable()
    {
        GameEvents.OnBoomerangThrown    -= HandleThrow;
        GameEvents.OnBoomerangCaught    -= HandleCatch;
        GameEvents.OnBoomerangWallBounce-= HandleWallBounce;
        GameEvents.OnBoomerangParried   -= HandleParry;
        GameEvents.OnPlayerDashed       -= HandleDash;
        GameEvents.OnPlayerHit          -= HandleHit;
        GameEvents.OnPlayerEliminated   -= HandleDeath;
        GameEvents.OnCountdownTick      -= HandleCountdown;
        GameEvents.OnRoundEnded         -= HandleRoundEnd;
        GameEvents.OnGameOver           -= HandleGameOver;
        GameEvents.OnPickupSpawned      -= HandlePickupSpawned;
        GameEvents.OnPickupCollected    -= HandlePickupCollected;
        GameEvents.OnShieldAbsorbed     -= HandleShieldAbsorbed;
        GameEvents.OnChargeStarted      -= HandleChargeStarted;
        GameEvents.OnChargeReleased     -= HandleChargeReleased;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Play(AudioCueId id)
    {
        if (library == null) return;

        // dedup terlalu cepat
        if (_lastPlayTime.TryGetValue(id, out var t)
            && Time.unscaledTime - t < minRetriggerInterval)
            return;
        _lastPlayTime[id] = Time.unscaledTime;

        var cue = library.Get(id);
        if (cue == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[AudioManager] Cue '{id}' not found in library.");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[AudioManager] Play cue={id} group={cue.group} loop={cue.loop}");
#endif

        if (cue.loop) PlayMusic(cue);
        else          PlayOneShot(cue);
    }

    public void StopMusic()
    {
        if (_musicSource != null) _musicSource.Stop();
    }

    public void SetSfxVolume01(float v)
    {
        _sfxVolume = Mathf.Clamp01(v);
        SetMixer01(sfxVolParam, v);
    }

    public void SetMusicVolume01(float v)
    {
        _musicVolume = Mathf.Clamp01(v);
        if (_musicSource != null) _musicSource.volume = _musicVolume;
        SetMixer01(musicVolParam, v);
    }

    public void SetUiVolume01(float v)
    {
        _uiVolume = Mathf.Clamp01(v);
        SetMixer01(uiVolParam, v);
    }

    public float GetSfxVolume01()   => _sfxVolume;
    public float GetMusicVolume01() => _musicVolume;
    public float GetUiVolume01()    => _uiVolume;

    private void SetMixer01(string param, float v)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;
        // konversi linear 0..1 ke dB (-80 .. 0)
        float db = v <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(v)) * 20f;
        mixer.SetFloat(param, db);
    }

    // ── Playback impl ─────────────────────────────────────────────────────────

    private void PlayOneShot(AudioCue cue)
    {
        var clip = cue.PickClip();
        if (clip == null) return;

        var src = GetVoice();
        src.clip   = clip;
        src.volume = cue.PickVolume() * ResolveGroupVolume(cue.group);
        src.pitch  = cue.PickPitch();
        src.outputAudioMixerGroup = ResolveGroup(cue.group);
        src.loop   = false;
        src.Play();

        _sfxPool.Enqueue(src);
    }

    private float ResolveGroupVolume(AudioCue.Group g) => g switch
    {
        AudioCue.Group.Music => _musicVolume,
        AudioCue.Group.UI    => _uiVolume,
        _                    => _sfxVolume,
    };

    private void PlayMusic(AudioCue cue)
    {
        var clip = cue.PickClip();
        if (clip == null) return;

        _musicSource.clip   = clip;
        _musicSource.volume = cue.PickVolume() * _musicVolume;
        _musicSource.pitch  = cue.PickPitch();
        _musicSource.loop   = true;
        _musicSource.Play();
    }

    private AudioSource GetVoice()
    {
        // round-robin
        var src = _sfxPool.Dequeue();
        if (src.isPlaying) src.Stop();
        return src;
    }

    private AudioMixerGroup ResolveGroup(AudioCue.Group g) => g switch
    {
        AudioCue.Group.Music => musicGroup,
        AudioCue.Group.UI    => uiGroup,
        _                    => sfxGroup,
    };

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleThrow(int playerIndex)        => Play(AudioCueId.BoomerangThrow);
    private void HandleCatch(int playerIndex)        => Play(AudioCueId.BoomerangCatch);
    private void HandleDash(int playerIndex)         => Play(AudioCueId.PlayerDash);
    private void HandleHit(int i, PlayerController c)
    {
        Play(AudioCueId.PlayerHit);
        DuckMusic(0.4f, 0.35f);
    }
    private void HandleDeath(int i, PlayerController c)
    {
        Play(AudioCueId.PlayerDeath);
        DuckMusic(0.25f, 0.6f);
    }
    private void HandleRoundEnd(int winnerIndex)             => Play(AudioCueId.RoundWin);
    private void HandleGameOver(int winnerIndex)             => Play(AudioCueId.MatchWin);
    private void HandleWallBounce()                          => Play(AudioCueId.BoomerangWallBounce);
    private void HandleParry(int playerIndex)                => Play(AudioCueId.BoomerangParry);
    private void HandlePickupSpawned()                       => Play(AudioCueId.PickupSpawn);
    private void HandlePickupCollected(int idx, int key, Vector3 worldPos) => Play(AudioCueId.PickupCollect);
    private void HandleShieldAbsorbed(int idx, Vector3 hitDir)             => Play(AudioCueId.ShieldBreak);
    private void HandleChargeStarted(int idx)                => Play(AudioCueId.ChargeLoop);
    private void HandleChargeReleased(int idx, float c)      { /* loop self-stops on next throw cue */ }

    // ── Music ducking ────────────────────────────────────────────────────────
    private Coroutine _duckRoutine;

    private void DuckMusic(float duckTo01, float duration)
    {
        if (_musicSource == null) return;
        if (_duckRoutine != null) StopCoroutine(_duckRoutine);
        _duckRoutine = StartCoroutine(DuckRoutine(duckTo01, duration));
    }

    private System.Collections.IEnumerator DuckRoutine(float duckTo, float duration)
    {
        float origVol = _musicVolume;
        float duckVol = origVol * duckTo;
        _musicSource.volume = duckVol;
        yield return YieldCollection.WaitForSecondsRealtime(duration);
        // smooth restore
        float t = 0f;
        const float restore = 0.4f;
        while (t < restore)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(duckVol, origVol, t / restore);
            yield return null;
        }
        _musicSource.volume = origVol;
        _duckRoutine = null;
    }

    private void HandleCountdown(int value)
    {
        Play(value > 0 ? AudioCueId.CountdownTick : AudioCueId.CountdownGo);
    }
}
