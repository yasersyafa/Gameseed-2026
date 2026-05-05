using UnityEngine;

/// <summary>
/// Boomerang explodes on hit — AoE knockback + cascade kill via
/// <c>Boomerang.ApplyExplosion</c> (already wired in OnCollisionEnter).
/// </summary>
public class ExplosiveEffect : IPowerUpEffect
{
    private const float Radius = 3.5f;

    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Explosive;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public ExplosiveEffect(float duration = 12f) { Duration = duration; }

    public void OnApply(PlayerController player)  { }
    public void OnRemove(PlayerController player) { }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang)
    {
        boomerang.ExplodeOnHit = true;
        boomerang.ExplodeRadius = Radius;
    }

    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}
