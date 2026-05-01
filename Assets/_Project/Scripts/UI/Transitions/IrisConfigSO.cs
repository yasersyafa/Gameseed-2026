using UnityEngine;

/// <summary>
/// Tunable iris-wipe timings. Place asset at
/// Assets/_Project/Resources/IrisConfig.asset — IrisTransition loads it
/// automatically. If missing, falls back to component defaults.
/// </summary>
[CreateAssetMenu(menuName = "Config/Iris Config", fileName = "IrisConfig")]
public class IrisConfigSO : ScriptableObject
{
    [Header("Timing (seconds)")]
    [Range(0f, 2f)]  public float closeDelay    = 0.4f;
    [Range(0.05f, 3f)] public float closeDuration = 0.7f;
    [Range(0f, 3f)]  public float holdAtClosed  = 0.35f;
    [Range(0.05f, 3f)] public float openDuration  = 0.5f;

    [Header("Radii")]
    [Range(0.5f, 3f)] public float fullRadius   = 1.6f;
    [Range(0f, 0.3f)] public float closedRadius = 0f;

    [Header("Game Over close multiplier")]
    [Range(0.5f, 4f)] public float gameOverDurationMul = 1.5f;
}
