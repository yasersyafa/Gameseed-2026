using UnityEngine;

/// <summary>
/// Project font registry. Populated by `Tools > Boomerang Fu > Setup/HUD`,
/// pushed into <see cref="FontHelper"/> at runtime by <see cref="GameHUDView"/>.
/// Replaces the prior <c>Resources.Load&lt;Font&gt;("...")</c> pattern.
/// </summary>
[CreateAssetMenu(menuName = "Boomerang Fu/Font Library", fileName = "FontLibrary")]
public class FontLibrary : ScriptableObject
{
    [Tooltip("Display face — chunky headers, score numbers, ELIM!. Recommended: Archivo Black.")]
    public Font display;

    [Tooltip("Monospace face — tabular numerics like ROUND counter. Recommended: JetBrains Mono Bold.")]
    public Font mono;
}
