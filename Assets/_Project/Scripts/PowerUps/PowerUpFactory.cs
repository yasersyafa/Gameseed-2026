/// <summary>
/// Build IPowerUpEffect instance dari PowerUpKey. Tempat sentral mapping
/// key → effect class. Stub untuk yang belum diimplementasi.
/// </summary>
public static class PowerUpFactory
{
    public static IPowerUpEffect Create(PowerUpSO so)
    {
        if (so == null) return null;
        return Create(so.key, so.durationSec);
    }

    public static IPowerUpEffect Create(PowerUpSO.PowerUpKey key, float duration)
    {
        return key switch
        {
            PowerUpSO.PowerUpKey.Fire             => new FireEffect(duration),
            PowerUpSO.PowerUpKey.Ice              => new IceEffect(duration),
            PowerUpSO.PowerUpKey.Multi            => new MultiEffect(duration),
            PowerUpSO.PowerUpKey.Shield           => new ShieldEffect(duration),
            PowerUpSO.PowerUpKey.Caffeinated      => new CaffeinatedEffect(duration),
            PowerUpSO.PowerUpKey.DashThroughWalls => new DashThroughWallsEffect(duration),
            PowerUpSO.PowerUpKey.Teleport         => new TeleportEffect(duration),
            PowerUpSO.PowerUpKey.Explosive        => new ExplosiveEffect(duration),
            PowerUpSO.PowerUpKey.Extra            => new ExtraEffect(duration),
            PowerUpSO.PowerUpKey.Disguise         => new DisguiseEffect(duration),
            PowerUpSO.PowerUpKey.Telekinesis      => new TelekinesisEffect(duration),
            PowerUpSO.PowerUpKey.Decoy            => new DecoyEffect(duration),

            _ => null,
        };
    }
}
