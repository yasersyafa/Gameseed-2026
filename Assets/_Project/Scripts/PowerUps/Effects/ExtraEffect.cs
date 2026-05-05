using UnityEngine;

/// <summary>
/// "Extra Boomerang" — until concurrent-throws refactor lands, this maps to a
/// simpler buff: doubled wall bounces + 30% longer max distance per throw.
/// Players read it as a more durable / longer-reach shot.
/// </summary>
public class ExtraEffect : IPowerUpEffect
{
    private const int   ExtraBounces = 5;
    private const float DistanceMul  = 1.30f;

    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Extra;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public ExtraEffect(float duration = 12f) { Duration = duration; }

    public void OnApply(PlayerController player)  { }
    public void OnRemove(PlayerController player) { }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang)
    {
        if (boomerang == null) return;
        boomerang.SetMaxBounces(ExtraBounces);
        // Stack with charge-throw distance from PlayerController if already set.
        // SetMaxDistance just bumps the override; callers can override again.
        // We multiply against player's max charge distance to feel meaningful.
        boomerang.SetMaxDistance(player.ChargeMaxDistance * DistanceMul);
    }

    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}
