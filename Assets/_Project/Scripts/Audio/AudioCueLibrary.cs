using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mapping antara AudioCueId ke AudioCue asset. Daftarkan semua cue di sini.
/// AudioManager pakai library ini untuk lookup waktu Play(id).
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Cue Library", fileName = "AudioCueLibrary")]
public class AudioCueLibrary : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public AudioCueId id;
        public AudioCue   cue;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<AudioCueId, AudioCue> _lookup;

    public AudioCue Get(AudioCueId id)
    {
        if (_lookup == null) BuildLookup();
        return _lookup.TryGetValue(id, out var cue) ? cue : null;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<AudioCueId, AudioCue>(entries?.Length ?? 0);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e.id == AudioCueId.None || e.cue == null) continue;
            _lookup[e.id] = e.cue;
        }
    }

    private void OnEnable() => _lookup = null;
}
