using UnityEngine;

/// <summary>
/// Static lookup of pastel tints per <see cref="PowerUpSO.PowerUpKey"/>.
/// Mirrors defaults seeded by <c>PowerUpSetupAutomation</c>; juice listeners
/// (PickupCollectVfx, PowerUpAuraVfx, etc.) read this so they don't need to
/// resolve the live <see cref="PowerUpSO"/> asset for tint info.
/// </summary>
public static class PowerUpVisuals
{
    public static Color GetTint(int key)
        => GetTint((PowerUpSO.PowerUpKey)key);

    public static Color GetTint(PowerUpSO.PowerUpKey key) => key switch
    {
        PowerUpSO.PowerUpKey.Caffeinated      => new Color(1.00f, 0.92f, 0.40f),
        PowerUpSO.PowerUpKey.DashThroughWalls => new Color(0.66f, 0.50f, 0.96f),
        PowerUpSO.PowerUpKey.Teleport         => new Color(0.40f, 0.90f, 1.00f),
        PowerUpSO.PowerUpKey.Explosive        => new Color(1.00f, 0.55f, 0.20f),
        PowerUpSO.PowerUpKey.Multi            => new Color(0.85f, 0.60f, 1.00f),
        PowerUpSO.PowerUpKey.Extra            => new Color(1.00f, 0.84f, 0.40f),
        PowerUpSO.PowerUpKey.Fire             => new Color(1.00f, 0.40f, 0.10f),
        PowerUpSO.PowerUpKey.Ice              => new Color(0.55f, 0.90f, 1.00f),
        PowerUpSO.PowerUpKey.Disguise         => new Color(0.65f, 0.65f, 0.70f),
        PowerUpSO.PowerUpKey.Shield           => new Color(1.00f, 0.95f, 0.40f),
        PowerUpSO.PowerUpKey.Telekinesis      => new Color(0.80f, 0.70f, 0.95f),
        PowerUpSO.PowerUpKey.Decoy            => new Color(0.96f, 0.78f, 0.86f),
        _                                     => Color.white,
    };
}
