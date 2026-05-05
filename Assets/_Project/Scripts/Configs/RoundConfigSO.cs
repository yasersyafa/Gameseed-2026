using UnityEngine;

/// <summary>
/// Aturan match-level: poin menang, durasi countdown, delay antar-round.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/Round Config", fileName = "RoundConfig_")]
public class RoundConfigSO : ScriptableObject
{
    [Header("Match")]
    public int   pointsToWin       = 5;
    public int   livesPerPlayer    = 1;

    [Header("Timing")]
    public float countdownDuration = 3f;
    public float roundEndDelay     = 2f;
    public float respawnDelay      = 2f;

    [Header("Mode")]
    public bool  suddenDeath       = false;  // overrides livesPerPlayer to 1 when true
    public bool  midRoundRespawn   = false;  // Boomerang Fu = false (eliminasi sampai round end)
    public bool  friendlyFire      = true;
    public float matchTimeLimit    = 0f;     // 0 = no limit
}
