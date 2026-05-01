using UnityEngine;

/// <summary>
/// Boomerang apply Frozen status ke target. Mutex dengan Fire.
/// </summary>
public class IceEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Ice;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.FireIce;
    public float                Duration { get; }

    public IceEffect(float duration = 15f) { Duration = duration; }

    public void OnApply(PlayerController player) { }
    public void OnRemove(PlayerController player) { }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang)
    {
        boomerang.OnHitStatus         = StatusEffectType.Frozen;
        boomerang.OnHitStatusDuration = 1.5f;
    }

    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
}
