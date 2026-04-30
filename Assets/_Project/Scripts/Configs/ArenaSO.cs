using UnityEngine;

/// <summary>
/// Definisi 1 arena: scene reference, preview, spawn count, hazard list,
/// ambient music cue.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/Arena", fileName = "Arena_")]
public class ArenaSO : ScriptableObject
{
    [Header("Identity")]
    public string  arenaId;
    public string  displayName;
    public Sprite  preview;

    [Header("Scene")]
    public string  sceneName;            // pakai SceneAttribute kalau perlu picker
    public int     maxPlayers   = 6;

    [Header("Audio")]
    public AudioCueId ambientMusic = AudioCueId.MusicGame;

    [Header("Tags")]
    public bool  hasWaterHazard;
    public bool  hasSlidingWalls;
    public bool  hasMovingPlatforms;
}
