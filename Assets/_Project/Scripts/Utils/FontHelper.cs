using UnityEngine;

/// <summary>
/// Cached default UI font. Resources.GetBuiltinResource isn't free; cache once.
/// </summary>
public static class FontHelper
{
    private static Font _default;

    public static Font Default()
    {
        if (_default == null)
            _default = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _default;
    }
}
