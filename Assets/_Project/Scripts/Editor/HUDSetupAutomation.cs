#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Editor automation for the in-scene HUD. Creates HUDPanelSettings asset
/// (if missing), drops `[GameHUD]` GameObject into the active scene with
/// UIDocument + GameHUDView + SettingsScreen + IrisTransition, wires every
/// asset reference (UXML, USS, PanelSettings), and ensures an EventSystem
/// with InputSystemUIInputModule exists for new-Input-System pointer
/// routing. Idempotent — safe to re-run.
///
/// Trigger via Tools > Boomerang Fu > Setup/HUD or as part of Setup All.
/// </summary>
public static class HUDSetupAutomation
{
    private const string HudFolder         = "Assets/_Project/UI/HUD";
    private const string ResourcesFolder   = HudFolder + "/Resources";
    private const string PanelSettingsPath = HudFolder + "/HUDPanelSettings.asset";
    private const string HudViewUxmlPath   = ResourcesFolder + "/HUDView.uxml";
    private const string PlayerRowUxmlPath = ResourcesFolder + "/PlayerRowTemplate.uxml";
    private const string HudUssPath        = ResourcesFolder + "/HUDStyle.uss";
    private const string HudGoName         = "[GameHUD]";
    private const string EventSystemGoName = "[EventSystem]";

    public static void RunAll()
    {
        EditorAssetUtils.EnsureFolder(HudFolder);

        var panelSettings = EnsurePanelSettings();
        var viewUxml      = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudViewUxmlPath);
        var rowUxml       = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PlayerRowUxmlPath);
        var uss           = AssetDatabase.LoadAssetAtPath<StyleSheet>(HudUssPath);

        if (viewUxml == null || rowUxml == null || uss == null)
        {
            Debug.LogError(
                $"[HUDSetup] Missing UXML/USS under {ResourcesFolder}. " +
                "Expected HUDView.uxml, PlayerRowTemplate.uxml, HUDStyle.uss.");
            return;
        }

        var hudGo = EnsureHudGameObject(panelSettings, viewUxml, rowUxml, uss);
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(hudGo.scene);
        EditorAssetUtils.Refresh();
        Selection.activeGameObject = hudGo;
        Debug.Log("[HUDSetup] HUD scene + asset refs ready.");
    }

    private static PanelSettings EnsurePanelSettings()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (ps == null)
        {
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(ps, PanelSettingsPath);
        }
        ps.scaleMode           = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.match               = 0.5f;
        ps.sortingOrder        = 100;
        EditorUtility.SetDirty(ps);
        return ps;
    }

    private static GameObject EnsureHudGameObject(
        PanelSettings ps,
        VisualTreeAsset viewUxml,
        VisualTreeAsset rowUxml,
        StyleSheet uss)
    {
        var existing = FindInActiveScene(HudGoName);
        if (existing == null)
        {
            existing = new GameObject(HudGoName);
            Undo.RegisterCreatedObjectUndo(existing, "Create [GameHUD]");
        }

        var doc = EnsureComponent<UIDocument>(existing);
        doc.panelSettings   = ps;
        doc.visualTreeAsset = viewUxml;

        var view = EnsureComponent<GameHUDView>(existing);
        var so = new SerializedObject(view);
        var rowProp = so.FindProperty("playerRowTemplate");
        var ussProp = so.FindProperty("hudStyleSheet");
        if (rowProp != null) rowProp.objectReferenceValue = rowUxml;
        if (ussProp != null) ussProp.objectReferenceValue = uss;
        so.ApplyModifiedProperties();

        EnsureComponent<SettingsScreen>(existing);
        EnsureComponent<IrisTransition>(existing);

        EditorUtility.SetDirty(existing);
        return existing;
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

    private static void EnsureEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject(EventSystemGoName);
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            es = go.AddComponent<EventSystem>();
        }
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            var legacy = es.GetComponent<BaseInputModule>();
            if (legacy != null && !(legacy is InputSystemUIInputModule))
                Object.DestroyImmediate(legacy);
            Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
        }
        EditorUtility.SetDirty(es.gameObject);
    }
}
#endif
