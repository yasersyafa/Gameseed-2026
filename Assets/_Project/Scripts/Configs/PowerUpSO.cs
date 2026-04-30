using UnityEngine;

/// <summary>
/// Definisi 1 power-up: ID, durasi, stack rule, mutex group, icon, audio cue.
/// Behavior class di-resolve runtime via powerUpKey.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/PowerUp", fileName = "PowerUp_")]
public class PowerUpSO : ScriptableObject
{
    public enum PowerUpKey
    {
        None,
        Caffeinated,
        DashThroughWalls,
        Teleport,
        Explosive,
        Multi,
        Extra,
        Fire,
        Ice,
        Disguise,
        Shield,
        Telekinesis,
        Decoy,
    }

    public enum MutexGroup { None, FireIce }

    [Header("Identity")]
    public PowerUpKey key = PowerUpKey.None;
    public string     displayName;
    public string     description;
    public Sprite     icon;
    public Color      tint = Color.white;

    [Header("Rules")]
    public float      durationSec   = 15f;
    public bool       stackable     = true;
    public MutexGroup mutex         = MutexGroup.None;

    [Header("Audio")]
    public AudioCueId activateCue   = AudioCueId.PowerUpActivate;
    public AudioCueId pickupCue     = AudioCueId.PickupCollect;
}
