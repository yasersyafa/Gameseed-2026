#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-generate semua audio asset placeholder:
/// - silent AudioClip (.wav) per cue
/// - AudioCue SO per cue
/// - AudioCueLibrary populated dengan semua entry
/// User tinggal drop file SFX nyata ke folder, library tetap reference clip yg sama nama-nya.
/// </summary>
public static class AudioSetupAutomation
{
    private const string AudioRoot = EditorAssetUtils.ProjectRoot + "/Audio";
    private const string CuesPath  = AudioRoot + "/Cues";
    private const string SfxPath   = AudioRoot + "/SFX";
    private const string MusicPath = AudioRoot + "/Soundtrack";
    private const string UiPath    = AudioRoot + "/UI";
    private const string LibPath   = AudioRoot + "/AudioCueLibrary.asset";

    public static void RunAll()
    {
        EditorAssetUtils.EnsureFolder(CuesPath);
        EditorAssetUtils.EnsureFolder(SfxPath);
        EditorAssetUtils.EnsureFolder(MusicPath);
        EditorAssetUtils.EnsureFolder(UiPath);

        var lib = EditorAssetUtils.CreateOrLoadSO<AudioCueLibrary>(LibPath);
        var entries = new List<AudioCueLibrary.Entry>();

        foreach (AudioCueId id in Enum.GetValues(typeof(AudioCueId)))
        {
            if (id == AudioCueId.None) continue;

            var cue  = BuildCueForId(id);
            entries.Add(new AudioCueLibrary.Entry { id = id, cue = cue });
        }

        var so = new SerializedObject(lib);
        var arr = so.FindProperty("entries");
        arr.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            var elem = arr.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("id").enumValueIndex  = (int)entries[i].id;
            elem.FindPropertyRelative("cue").objectReferenceValue = entries[i].cue;
        }
        so.ApplyModifiedProperties();
        EditorAssetUtils.Save(lib);

        EditorAssetUtils.Refresh();
        Debug.Log($"[BoomerangFu] AudioCueLibrary populated with {entries.Count} cues at {LibPath}");
    }

    private static AudioCue BuildCueForId(AudioCueId id)
    {
        var meta     = ResolveMeta(id);
        var cuePath  = $"{CuesPath}/Cue_{id}.asset";
        var clipPath = $"{meta.folder}/{id}.wav";

        var clip = EditorAssetUtils.CreateSilentClip(clipPath, meta.clipSeconds);

        var cue  = EditorAssetUtils.CreateOrLoadSO<AudioCue>(cuePath);
        cue.clips     = new[] { clip };
        cue.group     = meta.group;
        cue.loop      = meta.loop;
        cue.volumeMin = meta.volMin;
        cue.volumeMax = meta.volMax;
        cue.pitchMin  = meta.pitchMin;
        cue.pitchMax  = meta.pitchMax;
        EditorAssetUtils.Save(cue);
        return cue;
    }

    private struct CueMeta
    {
        public AudioCue.Group group;
        public string folder;
        public bool   loop;
        public float  clipSeconds;
        public float  volMin, volMax;
        public float  pitchMin, pitchMax;
    }

    private static CueMeta ResolveMeta(AudioCueId id)
    {
        var meta = new CueMeta {
            group = AudioCue.Group.SFX,
            folder = SfxPath,
            loop = false,
            clipSeconds = 0.1f,
            volMin = 0.9f, volMax = 1.0f,
            pitchMin = 1.0f, pitchMax = 1.0f,
        };

        switch (id)
        {
            case AudioCueId.MusicMenu:
            case AudioCueId.MusicGame:
                meta.group       = AudioCue.Group.Music;
                meta.folder      = MusicPath;
                meta.loop        = true;
                meta.clipSeconds = 1.0f;
                meta.volMin      = meta.volMax = 0.7f;
                break;

            case AudioCueId.UiHover:
            case AudioCueId.UiSelect:
            case AudioCueId.UiBack:
                meta.group  = AudioCue.Group.UI;
                meta.folder = UiPath;
                meta.volMin = meta.volMax = 0.8f;
                break;

            case AudioCueId.BoomerangThrow:
            case AudioCueId.BoomerangCatch:
            case AudioCueId.BoomerangWallBounce:
                meta.pitchMin = 0.95f; meta.pitchMax = 1.1f;
                break;

            case AudioCueId.BoomerangParry:
                meta.pitchMin = 1.05f; meta.pitchMax = 1.25f;
                meta.volMin   = 0.95f; meta.volMax   = 1.0f;
                break;

            case AudioCueId.ChargeLoop:
                meta.pitchMin = 0.9f; meta.pitchMax = 1.0f;
                meta.volMin   = 0.5f; meta.volMax   = 0.7f;
                break;

            case AudioCueId.PlayerHit:
            case AudioCueId.PlayerDeath:
                meta.pitchMin = 0.9f; meta.pitchMax = 1.05f;
                break;

            case AudioCueId.CountdownTick:
                meta.pitchMin = meta.pitchMax = 1.0f;
                break;

            case AudioCueId.CountdownGo:
                meta.pitchMin = meta.pitchMax = 1.2f;
                break;
        }

        return meta;
    }
}
#endif
