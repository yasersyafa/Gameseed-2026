#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor automation for the in-scene global juice listeners
/// (PostFxJuice + HitImpactVfx + WinFlourish). Replaces the deleted
/// <c>JuiceBootstrap</c> runtime spawn — that pattern silently broke after
/// the first scene reload because the DontDestroyOnLoad listeners outlived
/// <c>GameEvents.ClearAll</c> without re-subscribing.
///
/// Idempotent: re-running the menu updates the existing `[Juice]` GO in
/// place rather than spawning duplicates.
/// </summary>
public static class JuiceSetupAutomation
{
    private const string JuiceGoName = "[Juice]";

    public static void RunAll()
    {
        var go = FindInActiveScene(JuiceGoName);
        if (go == null)
        {
            go = new GameObject(JuiceGoName);
            Undo.RegisterCreatedObjectUndo(go, "Create [Juice]");
        }

        EnsureComponent<PostFxJuice>(go);
        EnsureComponent<HitImpactVfx>(go);
        EnsureComponent<WinFlourish>(go);
        EnsureComponent<PickupCollectVfx>(go);

        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorAssetUtils.Refresh();
        Selection.activeGameObject = go;
        Debug.Log("[JuiceSetup] [Juice] GO ready with PostFxJuice + HitImpactVfx + WinFlourish + PickupCollectVfx.");
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static GameObject FindInActiveScene(string name)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }
}
#endif
