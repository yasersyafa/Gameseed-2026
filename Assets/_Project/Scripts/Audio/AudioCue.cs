using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Satu cue audio: 1 atau lebih clip (random pick), volume + pitch range,
/// dan target mixer group. Reusable lewat AudioCueLibrary.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "AudioCue_")]
public class AudioCue : ScriptableObject
{
    public enum Group { SFX, Music, UI }

    [Header("Clips (random pick)")]
    public AudioClip[] clips;

    [Header("Routing")]
    public Group group = Group.SFX;

    [Header("Volume")]
    [Range(0f, 1f)] public float volumeMin = 0.9f;
    [Range(0f, 1f)] public float volumeMax = 1.0f;

    [Header("Pitch")]
    [Range(0.1f, 3f)] public float pitchMin = 1f;
    [Range(0.1f, 3f)] public float pitchMax = 1f;

    [Header("Loop (for music)")]
    public bool loop = false;

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public float PickVolume() => Random.Range(volumeMin, volumeMax);
    public float PickPitch()  => Random.Range(pitchMin, pitchMax);
}
