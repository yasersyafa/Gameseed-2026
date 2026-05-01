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
            PowerUpSO.PowerUpKey.Fire   => new FireEffect(duration),
            PowerUpSO.PowerUpKey.Ice    => new IceEffect(duration),
            PowerUpSO.PowerUpKey.Multi  => new MultiEffect(duration),
            PowerUpSO.PowerUpKey.Shield => new ShieldEffect(duration),

            // Stubs (Phase 3d)
            PowerUpSO.PowerUpKey.Caffeinated      => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.DashThroughWalls => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.Teleport         => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.Explosive        => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.Extra            => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.Disguise         => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.Telekinesis      => new StubPowerUp(key, duration),
            PowerUpSO.PowerUpKey.Decoy            => new StubPowerUp(key, duration),

            _ => null,
        };
    }
}
