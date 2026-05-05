using UnityEngine;

/// <summary>
/// Static font registry. Fonts are pushed in via <see cref="Register"/> at
/// runtime — typically by <see cref="GameHUDView"/> reading a serialized
/// <see cref="FontLibrary"/> asset on Awake. <see cref="Display"/> /
/// <see cref="Mono"/> fall back to Unity's built-in legacy face if nothing
/// has been registered yet.
/// </summary>
public static class FontHelper
{
    private static Font _default;
    private static Font _display;
    private static Font _mono;

    public static void Register(Font display, Font mono)
    {
        if (display != null) _display = display;
        if (mono    != null) _mono    = mono;
    }

    public static Font Default()
    {
        if (_default == null)
            _default = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _default;
    }

    /// <summary>Display face — chunky headers, score numbers, ELIM!.</summary>
    public static Font Display() => _display != null ? _display : Default();

    /// <summary>Monospace face — tabular numerics like ROUND counter.</summary>
    public static Font Mono() => _mono != null ? _mono : Default();
}
