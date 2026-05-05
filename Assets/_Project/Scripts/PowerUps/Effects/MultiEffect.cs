using UnityEngine;

/// <summary>
/// Throw spawn 3 boomerang dengan angle spread. Effect ini di-trigger di
/// PlayerController.ThrowBoomerangCharged setelah primary spawn → spawn
/// dua tambahan secara manual.
/// </summary>
public class MultiEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Multi;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    private const float SpreadAngleDeg = 18f;

    public MultiEffect(float duration = 15f) { Duration = duration; }

    public void OnApply(PlayerController player) { }
    public void OnRemove(PlayerController player) { }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang)
    {
        // boomerang utama sudah di-spawn. Tambah 2 lagi di kiri+kanan.
        SpawnExtra(player, boomerang, +SpreadAngleDeg);
        SpawnExtra(player, boomerang, -SpreadAngleDeg);
    }

    private void SpawnExtra(PlayerController player, Boomerang primary, float angleDeg)
    {
        var prefab = primary != null ? primary.gameObject : null;
        if (prefab == null) return;

        Vector3 dir = Quaternion.AngleAxis(angleDeg, Vector3.up) * primary.transform.forward;
        Vector3 spawnPos = primary.transform.position;

        var clone = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        var boom  = clone.GetComponent<Boomerang>();
        boom.Launch(player.transform, dir, primary.CurrentSpeed);
        boom.SetThrowerIndex(player.PlayerIndex);
        boom.OnHitStatus         = primary.OnHitStatus;
        boom.OnHitStatusDuration = primary.OnHitStatusDuration;
    }

    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}
