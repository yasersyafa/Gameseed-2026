using UnityEngine;

/// <summary>
/// Cached UI font lookups. Resources.GetBuiltinResource + Resources.Load aren't
/// free; cache once. TTFs live under Assets/_Project/Fonts/Resources so they can
/// be Resources.Load'd from runtime / DontDestroyOnLoad bootstraps.
/// </summary>
public static class FontHelper
{
    private static Font _default;
    private static Font _display;
    private static Font _mono;

    public static Font Default()
    {
        if (_default == null)
            _default = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _default;
    }

    /// <summary>Archivo Black — chunky display face for HUD headers / counters.</summary>
    public static Font Display()
    {
        if (_display == null)
            _display = Resources.Load<Font>("ArchivoBlack-Regular") ?? Default();
        return _display;
    }

    /// <summary>JetBrains Mono Bold — monospace for numeric / tabular HUD readouts.</summary>
    public static Font Mono()
    {
        if (_mono == null)
            _mono = Resources.Load<Font>("JetBrainsMono-Bold") ?? Default();
        return _mono;
    }
}
