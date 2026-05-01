using UnityEngine;

/// <summary>
/// Kontrak 1 power-up effect aktif di player. PowerUpController panggil hook
/// ini sesuai event gameplay. Lifetime di-track via Duration; OnApply/OnRemove
/// dipakai untuk setup/cleanup state di player atau boomerang spawn.
/// </summary>
public interface IPowerUpEffect
{
    PowerUpSO.PowerUpKey   Key      { get; }
    PowerUpSO.MutexGroup   Mutex    { get; }
    float                  Duration { get; }

    void OnApply(PlayerController player);
    void OnRemove(PlayerController player);

    /// <summary>Modifikasi boomerang yang baru di-spawn (Multi, Fire, Ice, Explosive).</summary>
    void OnBeforeThrow(PlayerController player, Boomerang boomerang);

    /// <summary>Return true untuk absorb hit (Shield). False = lanjutkan ke OnHitByBoomerang.</summary>
    bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir);
}
