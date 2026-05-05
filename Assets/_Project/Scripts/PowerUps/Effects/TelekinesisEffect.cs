using UnityEngine;

/// <summary>
/// Steers the player's active in-flight boomerang toward
/// <see cref="PlayerController.LastMoveDirection"/> every frame. Lets the
/// player curve a thrown shot around walls.
/// </summary>
public class TelekinesisEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Telekinesis;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public TelekinesisEffect(float duration = 10f) { Duration = duration; }

    public void OnApply(PlayerController player)  { }
    public void OnRemove(PlayerController player) { }
    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;

    public void Tick(PlayerController player)
    {
        var b = player.ActiveBoomerang;
        if (b == null || b.State != Boomerang.BoomerangState.Flying) return;
        Vector3 dir = player.LastMoveDirection;
        if (dir.sqrMagnitude < 0.001f) return;
        b.Steer(dir);
    }
}
