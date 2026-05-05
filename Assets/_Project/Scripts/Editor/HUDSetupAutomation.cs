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
    private const string HudFolder              = "Assets/_Project/UI/HUD";
    private const string ConfigsFolder          = "Assets/_Project/ScriptableObjects/Configs";
    private const string FontsFolder            = "Assets/_Project/Fonts";
    private const string PanelSettingsPath      = HudFolder + "/HUDPanelSettings.asset";
    private const string GameOverPanelSettingsPath = HudFolder + "/GameOverPanelSettings.asset";
    private const string HudViewUxmlPath        = HudFolder + "/HUDView.uxml";
    private const string PlayerRowUxmlPath      = HudFolder + "/PlayerRowTemplate.uxml";
    private const string GameOverViewUxmlPath   = HudFolder + "/GameOverView.uxml";
    private const string HudUssPath             = HudFolder + "/HUDStyle.uss";
    private const string FontLibraryPath        = HudFolder + "/FontLibrary.asset";
    private const string DisplayFontPath        = FontsFolder + "/ArchivoBlack-Regular.ttf";
    private const string MonoFontPath           = FontsFolder + "/JetBrainsMono-Bold.ttf";
    private const string IrisConfigPath         = ConfigsFolder + "/IrisConfig.asset";
    private const string HudGoName              = "[GameHUD]";
    private const string GameOverGoName         = "[GameOverOverlay]";
    private const string EventSystemGoName      = "[EventSystem]";

    // sortingOrder layering: HUD (100) → iris Canvas (200) → game-over (300).
    // Keeps round-time HUD behind the iris wipe; game-over panel reveals on
    // top of the fully-closed iris.
    private const int HudSortingOrder       = 100;
    private const int GameOverSortingOrder  = 300;

    public static void RunAll()
    {
        EditorAssetUtils.EnsureFolder(HudFolder);

        var hudPanelSettings      = EnsurePanelSettings(PanelSettingsPath, HudSortingOrder);
        var gameOverPanelSettings = EnsurePanelSettings(GameOverPanelSettingsPath, GameOverSortingOrder);

        var viewUxml      = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudViewUxmlPath);
        var rowUxml       = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PlayerRowUxmlPath);
        var gameOverUxml  = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameOverViewUxmlPath);
        var uss           = AssetDatabase.LoadAssetAtPath<StyleSheet>(HudUssPath);

        if (viewUxml == null || rowUxml == null || gameOverUxml == null || uss == null)
        {
            Debug.LogError(
                $"[HUDSetup] Missing UXML/USS under {HudFolder}. Expected " +
                "HUDView.uxml, PlayerRowTemplate.uxml, GameOverView.uxml, HUDStyle.uss.");
            return;
        }

        var fontLibrary = EnsureFontLibrary();
        var hudGo       = EnsureHudGameObject(hudPanelSettings, viewUxml, rowUxml, uss, fontLibrary);
        var overlayGo   = EnsureGameOverGameObject(gameOverPanelSettings, gameOverUxml, uss, fontLibrary);
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(hudGo.scene);
        EditorAssetUtils.Refresh();
        Selection.activeGameObject = hudGo;
        Debug.Log("[HUDSetup] HUD + GameOver overlay + asset refs ready.");
    }

    private static PanelSettings EnsurePanelSettings(string path, int sortingOrder)
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (ps == null)
        {
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(ps, path);
        }
        ps.scaleMode           = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.match               = 0.5f;
        ps.sortingOrder        = sortingOrder;
        EditorUtility.SetDirty(ps);
        return ps;
    }

    private static FontLibrary EnsureFontLibrary()
    {
        var lib = AssetDatabase.LoadAssetAtPath<FontLibrary>(FontLibraryPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<FontLibrary>();
            AssetDatabase.CreateAsset(lib, FontLibraryPath);
        }
        var display = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);
        var mono    = AssetDatabase.LoadAssetAtPath<Font>(MonoFontPath);
        if (display != null) lib.display = display;
        if (mono    != null) lib.mono    = mono;
        EditorUtility.SetDirty(lib);
        return lib;
    }

    private static GameObject EnsureHudGameObject(
        PanelSettings ps,
        VisualTreeAsset viewUxml,
        VisualTreeAsset rowUxml,
        StyleSheet uss,
        FontLibrary fontLibrary)
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
        var rowProp  = so.FindProperty("playerRowTemplate");
        var ussProp  = so.FindProperty("hudStyleSheet");
        var fontProp = so.FindProperty("fontLibrary");
        if (rowProp  != null) rowProp.objectReferenceValue  = rowUxml;
        if (ussProp  != null) ussProp.objectReferenceValue  = uss;
        if (fontProp != null) fontProp.objectReferenceValue = fontLibrary;
        so.ApplyModifiedProperties();

        EnsureComponent<SettingsScreen>(existing);
        var iris = EnsureComponent<IrisTransition>(existing);
        WireIrisConfig(iris);

        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static GameObject EnsureGameOverGameObject(
        PanelSettings ps,
        VisualTreeAsset uxml,
        StyleSheet uss,
        FontLibrary fontLibrary)
    {
        var existing = FindInActiveScene(GameOverGoName);
        if (existing == null)
        {
            existing = new GameObject(GameOverGoName);
            Undo.RegisterCreatedObjectUndo(existing, "Create [GameOverOverlay]");
        }

        var doc = EnsureComponent<UIDocument>(existing);
        doc.panelSettings   = ps;
        doc.visualTreeAsset = uxml;

        var view = EnsureComponent<GameOverView>(existing);
        var so = new SerializedObject(view);
        var ussProp  = so.FindProperty("hudStyleSheet");
        var fontProp = so.FindProperty("fontLibrary");
        if (ussProp  != null) ussProp.objectReferenceValue  = uss;
        if (fontProp != null) fontProp.objectReferenceValue = fontLibrary;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static void WireIrisConfig(IrisTransition iris)
    {
        var cfg = AssetDatabase.LoadAssetAtPath<IrisConfigSO>(IrisConfigPath);
        if (cfg == null)
        {
            Debug.LogWarning($"[HUDSetup] IrisConfig.asset not found at {IrisConfigPath}; iris keeps SerializeField defaults.");
            return;
        }
        var so = new SerializedObject(iris);
        var prop = so.FindProperty("configSO");
        if (prop != null) prop.objectReferenceValue = cfg;
        so.ApplyModifiedProperties();
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
