using UnityEngine;

/// <summary>
/// Cached shader lookups. Replaces repeated Shader.Find calls scattered across
/// runtime visual scripts (DashTrail, Boomerang trail, AimIndicator, etc.).
/// Shader.Find is not free; cache once.
/// </summary>
public static class ShaderHelper
{
    private static Shader _unlit;
    private static Shader _lit;
    private static Shader _spriteDefault;

    public static Shader GetUnlit()
    {
        if (_unlit == null)
            _unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        return _unlit;
    }

    public static Shader GetLit()
    {
        if (_lit == null)
            _lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return _lit;
    }

    public static Shader GetSpriteDefault()
    {
        if (_spriteDefault == null)
            _spriteDefault = Shader.Find("Sprites/Default") ?? GetUnlit();
        return _spriteDefault;
    }

    private static Material _sharedUnlit;

    /// <summary>
    /// One shared unlit Material reused across runtime LineRenderer / TrailRenderer
    /// instances whose color is driven via Gradient (not material color). Avoids
    /// `new Material(...)` per spawn — those leak materials and bloat VRAM.
    /// </summary>
    public static Material SharedUnlit()
    {
        if (_sharedUnlit == null)
        {
            _sharedUnlit = new Material(GetUnlit());
            _sharedUnlit.name = "[SharedUnlit]";
        }
        return _sharedUnlit;
    }
}
