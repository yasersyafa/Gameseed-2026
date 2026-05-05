using UnityEngine;

/// <summary>
/// One-shot dash replacement: warps the player along their last move direction.
/// Consumed on first dash press; <see cref="OnBeforeDash"/> returns true so the
/// regular Dash state never runs.
/// </summary>
public class TeleportEffect : IPowerUpEffect
{
    private const float Range = 5f;

    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Teleport;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    public TeleportEffect(float duration = 5f) { Duration = duration; }

    public void OnApply(PlayerController player)  { }
    public void OnRemove(PlayerController player) { }
    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;

    public bool OnBeforeDash(PlayerController player)
    {
        Vector3 dir = player.LastMoveDirection;
        if (dir.sqrMagnitude < 0.001f) dir = player.transform.forward;
        Vector3 target = player.transform.position + dir.normalized * Range;
        if (player.Rb != null)
            player.Rb.position = target;
        else
            player.transform.position = target;
        return true; // consume effect
    }

    public void Tick(PlayerController player) { }
}
