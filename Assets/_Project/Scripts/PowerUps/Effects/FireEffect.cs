using UnityEngine;

/// <summary>
/// Boomerang yang dilempar saat fire aktif apply Burning status ke target.
/// Mutex dengan Ice (BFu rule).
/// </summary>
public class FireEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Fire;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.FireIce;
    public float                Duration { get; }

    public FireEffect(float duration = 15f) { Duration = duration; }

    public void OnApply(PlayerController player) { }
    public void OnRemove(PlayerController player) { }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang)
    {
        boomerang.OnHitStatus         = StatusEffectType.Burning;
        boomerang.OnHitStatusDuration = 1f;
    }

    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
}
