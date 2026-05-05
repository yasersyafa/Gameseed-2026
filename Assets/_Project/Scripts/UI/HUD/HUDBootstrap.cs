using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Spawns GameHUDView (UI Toolkit, UXML/USS-driven) + SettingsScreen + IrisTransition
/// at runtime. Zero scene wiring. Also ensures an EventSystem with
/// InputSystemUIInputModule exists so UI Toolkit Buttons receive pointer events
/// (project uses the new Input System).
/// </summary>
public static class HUDBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        EnsureEventSystem();

        if (Object.FindFirstObjectByType<GameHUDView>() != null) return;

        var go = new GameObject("[GameHUD]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameHUDView>();
        go.AddComponent<SettingsScreen>();
        go.AddComponent<IrisTransition>();

#if UNITY_EDITOR
        Debug.Log("[HUDBootstrap] GameHUDView + SettingsScreen + IrisTransition spawned");
#endif
    }

    private static void EnsureEventSystem()
    {
        var existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            // Make sure it carries the InputSystem module — legacy
            // StandaloneInputModule is incompatible with new Input System.
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                var legacy = existing.GetComponent<BaseInputModule>();
                if (legacy != null) Object.Destroy(legacy);
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            return;
        }

        var go = new GameObject("[EventSystem]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
#if UNITY_EDITOR
        Debug.Log("[HUDBootstrap] EventSystem + InputSystemUIInputModule spawned");
#endif
    }
}
