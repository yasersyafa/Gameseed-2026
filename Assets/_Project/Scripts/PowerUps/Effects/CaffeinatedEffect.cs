using UnityEngine;

/// <summary>
/// Move + dash speed boost. Multiplies <see cref="PlayerController.MoveSpeedMultiplier"/>
/// and divides <see cref="PlayerController.DashCooldownMultiplier"/> while active.
/// Stack-compounds; cleanly restores on remove.
/// </summary>
public class CaffeinatedEffect : IPowerUpEffect
{
    private const float SpeedMul    = 1.30f;
    private const float DashCdMul   = 0.60f;

    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Caffeinated;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public CaffeinatedEffect(float duration = 10f) { Duration = duration; }

    public void OnApply(PlayerController player)
    {
        player.MoveSpeedMultiplier    *= SpeedMul;
        player.DashCooldownMultiplier *= DashCdMul;
    }

    public void OnRemove(PlayerController player)
    {
        player.MoveSpeedMultiplier    /= SpeedMul;
        player.DashCooldownMultiplier /= DashCdMul;
    }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}
