#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Helpers untuk editor automation: bikin folder, ScriptableObject,
/// prefab dari primitive, dll. Dipakai oleh BoomerangFuMenu.
/// </summary>
public static class EditorAssetUtils
{
    public const string ProjectRoot = "Assets/_Project";

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts  = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    public static T CreateOrLoadSO<T>(string assetPath) where T : ScriptableObject
    {
        EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));

        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, assetPath);
        return so;
    }

    public static T LoadSO<T>(string assetPath) where T : ScriptableObject
        => AssetDatabase.LoadAssetAtPath<T>(assetPath);

    public static void Save(Object asset)
    {
        EditorUtility.SetDirty(asset);
    }

    public static void Refresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static GameObject CreatePrimitivePrefab(
        string prefabPath,
        PrimitiveType primitive,
        Color color,
        Vector3 scale,
        bool addCollider = true,
        bool addRigidbody = false)
    {
        EnsureFolder(Path.GetDirectoryName(prefabPath).Replace('\\', '/'));

        var go = GameObject.CreatePrimitive(primitive);
        go.transform.localScale = scale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            renderer.sharedMaterial = mat;

            string matPath = prefabPath.Replace(".prefab", "_Mat.mat");
            AssetDatabase.CreateAsset(mat, matPath);
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }

        if (!addCollider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        if (addRigidbody) go.AddComponent<Rigidbody>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>Bikin AudioClip silent (samples 0) sebagai placeholder.</summary>
    public static AudioClip CreateSilentClip(string assetPath, float seconds = 0.1f, int frequency = 22050)
    {
        EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));

        var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (existing != null) return existing;

        // Unity tidak support save AudioClip via CreateAsset (binary).
        // Tulis WAV header minimal silent.
        int   sampleCount = Mathf.RoundToInt(seconds * frequency);
        byte[] wav        = BuildSilentWav(sampleCount, frequency);

        File.WriteAllBytes(assetPath, wav);
        AssetDatabase.ImportAsset(assetPath);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }

    private static byte[] BuildSilentWav(int sampleCount, int sampleRate)
    {
        const int channels       = 1;
        const int bitsPerSample  = 16;
        int       byteRate       = sampleRate * channels * bitsPerSample / 8;
        int       blockAlign     = channels * bitsPerSample / 8;
        int       dataSize       = sampleCount * blockAlign;
        int       fileSize       = 44 + dataSize;

        var buf = new byte[fileSize];
        int o   = 0;

        // RIFF header
        buf[o++] = (byte)'R'; buf[o++] = (byte)'I'; buf[o++] = (byte)'F'; buf[o++] = (byte)'F';
        WriteInt32(buf, ref o, fileSize - 8);
        buf[o++] = (byte)'W'; buf[o++] = (byte)'A'; buf[o++] = (byte)'V'; buf[o++] = (byte)'E';

        // fmt chunk
        buf[o++] = (byte)'f'; buf[o++] = (byte)'m'; buf[o++] = (byte)'t'; buf[o++] = (byte)' ';
        WriteInt32(buf, ref o, 16);
        WriteInt16(buf, ref o, 1);              // PCM
        WriteInt16(buf, ref o, (short)channels);
        WriteInt32(buf, ref o, sampleRate);
        WriteInt32(buf, ref o, byteRate);
        WriteInt16(buf, ref o, (short)blockAlign);
        WriteInt16(buf, ref o, (short)bitsPerSample);

        // data chunk
        buf[o++] = (byte)'d'; buf[o++] = (byte)'a'; buf[o++] = (byte)'t'; buf[o++] = (byte)'a';
        WriteInt32(buf, ref o, dataSize);

        // samples already zero (silent)
        return buf;
    }

    private static void WriteInt32(byte[] buf, ref int o, int v)
    {
        buf[o++] = (byte)(v & 0xff);
        buf[o++] = (byte)((v >> 8)  & 0xff);
        buf[o++] = (byte)((v >> 16) & 0xff);
        buf[o++] = (byte)((v >> 24) & 0xff);
    }

    private static void WriteInt16(byte[] buf, ref int o, short v)
    {
        buf[o++] = (byte)(v & 0xff);
        buf[o++] = (byte)((v >> 8) & 0xff);
    }
}
#endif
