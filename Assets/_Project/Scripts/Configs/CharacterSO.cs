using UnityEngine;

/// <summary>
/// Definisi 1 character (food). Mesh / material / voice / splat VFX.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/Character", fileName = "Character_")]
public class CharacterSO : ScriptableObject
{
    [Header("Identity")]
    public string   characterId;
    public string   displayName;
    public Sprite   portrait;

    [Header("Visual (placeholder = capsule)")]
    public Mesh     mesh;
    public Material material;
    public Color    primaryColor = Color.white;
    public Vector3  meshScale    = Vector3.one;

    [Header("Audio (optional)")]
    public AudioCueId hurtCue = AudioCueId.PlayerHit;
    public AudioCueId dieCue  = AudioCueId.PlayerDeath;

    [Header("Death VFX")]
    public GameObject splatPrefab;
}
