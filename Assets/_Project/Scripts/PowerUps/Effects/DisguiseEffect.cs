using UnityEngine;

/// <summary>
/// Tints the player visual to a neutral wall color so opponents can't pick
/// them out at a glance. Cosmetic only for v1; full "boomerang ignore until
/// next throw" behaviour is deferred until a tag swap is wired.
/// </summary>
public class DisguiseEffect : IPowerUpEffect
{
    private static readonly Color DisguiseColor = new(0.65f, 0.65f, 0.70f);

    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Disguise;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    private Color _originalColor;
    private bool  _captured;

    public DisguiseEffect(float duration = 8f) { Duration = duration; }

    public void OnApply(PlayerController player)
    {
        var rend = player.visual;
        if (rend == null || rend.sharedMaterial == null) return;

        _originalColor = rend.sharedMaterial.color;
        _captured = true;

        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", DisguiseColor);
        mpb.SetColor("_Color",     DisguiseColor);
        rend.SetPropertyBlock(mpb);
    }

    public void OnRemove(PlayerController player)
    {
        var rend = player.visual;
        if (rend == null || !_captured) return;

        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", _originalColor);
        mpb.SetColor("_Color",     _originalColor);
        rend.SetPropertyBlock(mpb);
    }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}
