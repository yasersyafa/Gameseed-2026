using UnityEngine;
using VContainer;

/// <summary>
/// Persisted user settings. PlayerPrefs-backed. Other systems read static
/// flags or query instance to gate effects (shake/slowmo/rumble).
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const string KEY_MASTER  = "vol_master";
    private const string KEY_MUSIC   = "vol_music";
    private const string KEY_SFX     = "vol_sfx";
    private const string KEY_UI      = "vol_ui";
    private const string KEY_SHAKE   = "fx_shake";
    private const string KEY_SLOWMO  = "fx_slowmo";
    private const string KEY_RUMBLE  = "fx_rumble";

    public static SettingsManager Instance { get; private set; }

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume  { get; private set; } = 1f;
    public float SfxVolume    { get; private set; } = 1f;
    public float UiVolume     { get; private set; } = 1f;
    public bool  ShakeEnabled { get; private set; } = true;
    public bool  SlowMoEnabled { get; private set; } = true;
    public bool  RumbleEnabled { get; private set; } = true;

    private AudioManager _audio;

    [Inject]
    public void Construct(AudioManager audio)
    {
        _audio = audio;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Load();
    }

    private void Start()
    {
        ApplyToAudio();
    }

    public void Load()
    {
        MasterVolume  = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        MusicVolume   = PlayerPrefs.GetFloat(KEY_MUSIC,  0.7f);
        SfxVolume     = PlayerPrefs.GetFloat(KEY_SFX,    1f);
        UiVolume      = PlayerPrefs.GetFloat(KEY_UI,     1f);
        ShakeEnabled  = PlayerPrefs.GetInt(KEY_SHAKE,   1) == 1;
        SlowMoEnabled = PlayerPrefs.GetInt(KEY_SLOWMO,  1) == 1;
        RumbleEnabled = PlayerPrefs.GetInt(KEY_RUMBLE,  1) == 1;
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, MasterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC,  MusicVolume);
        PlayerPrefs.SetFloat(KEY_SFX,    SfxVolume);
        PlayerPrefs.SetFloat(KEY_UI,     UiVolume);
        PlayerPrefs.SetInt(KEY_SHAKE,    ShakeEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_SLOWMO,   SlowMoEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RUMBLE,   RumbleEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMaster(float v)  { MasterVolume = Mathf.Clamp01(v); ApplyToAudio(); Save(); }
    public void SetMusic(float v)   { MusicVolume  = Mathf.Clamp01(v); ApplyToAudio(); Save(); }
    public void SetSfx(float v)     { SfxVolume    = Mathf.Clamp01(v); ApplyToAudio(); Save(); }
    public void SetUi(float v)      { UiVolume     = Mathf.Clamp01(v); ApplyToAudio(); Save(); }
    public void SetShake(bool b)    { ShakeEnabled  = b; Save(); }
    public void SetSlowMo(bool b)   { SlowMoEnabled = b; Save(); }
    public void SetRumble(bool b)   { RumbleEnabled = b; Save(); }

    private void ApplyToAudio()
    {
        if (_audio == null) return;
        _audio.SetMusicVolume01(MusicVolume * MasterVolume);
        _audio.SetSfxVolume01(SfxVolume   * MasterVolume);
        _audio.SetUiVolume01(UiVolume     * MasterVolume);
    }
}
