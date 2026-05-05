using UnityEngine;

/// <summary>
/// Absorb 1 hit. Setelah absorb, effect di-consume otomatis (return true di
/// OnBeforeHit → PowerUpController remove).
/// </summary>
public class ShieldEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Shield;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public ShieldEffect(float duration = 30f) { Duration = duration; }

    public void OnApply(PlayerController player) { }
    public void OnRemove(PlayerController player) { }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }

    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir)
    {
        // Fire-and-forget visual / audio cue. Controller consumes the effect
        // after this returns true; PowerUpController also raises
        // OnPowerUpRemoved so the player aura updates.
        if (player != null)
            GameEvents.RaiseShieldAbsorbed(player.PlayerIndex, hitDir);
        return true;
    }
}
