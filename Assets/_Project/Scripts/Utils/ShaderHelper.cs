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
}
