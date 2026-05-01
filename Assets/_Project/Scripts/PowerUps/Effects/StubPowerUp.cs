using UnityEngine;

/// <summary>
/// Placeholder effect untuk power-up yang belum diimplementasi. Log apply/remove
/// supaya pickup chain bisa dites. Logic actual TBD di Phase 3e+.
/// </summary>
public class StubPowerUp : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      { get; }
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public StubPowerUp(PowerUpSO.PowerUpKey key, float duration)
    {
        Key      = key;
        Duration = duration;
    }

    public void OnApply(PlayerController player)
    {
#if UNITY_EDITOR
        Debug.Log($"[PowerUp Stub] {Key} applied (no behavior yet)");
#endif
    }

    public void OnRemove(PlayerController player) { }
    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
}
