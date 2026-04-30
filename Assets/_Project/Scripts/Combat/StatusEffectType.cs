/// <summary>
/// Tipe status effect yang bisa di-apply ke player.
/// </summary>
public enum StatusEffectType
{
    None,
    Burning,    // tick damage; di Boomerang Fu = lethal segera
    Frozen,     // lock movement
    Stunned,    // brief lock
}
