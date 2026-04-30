using UnityEngine;

/// <summary>
/// Definisi 1 game mode. Behavior class di-resolve runtime via modeKey.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/Mode", fileName = "Mode_")]
public class ModeSO : ScriptableObject
{
    public enum ModeKey { FreeForAll, TeamUp, GoldenBoomerang, HideAndSeek }

    [Header("Identity")]
    public ModeKey  key = ModeKey.FreeForAll;
    public string   displayName;
    public string   description;
    public Sprite   icon;

    [Header("Rules")]
    public bool     teamBased;
    public int      teamCount        = 2;
    public bool     allowAi          = true;
    public int      defaultPointsToWin = 5;

    [Header("Mode-specific")]
    public float    goldenBoomerangHoldTime = 30f;
    public float    goldenBoomerangSlow      = 0.6f;
    public float    hideSeekRotateSeconds    = 30f;
}
