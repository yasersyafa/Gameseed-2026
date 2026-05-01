using UnityEngine;

/// <summary>
/// Spawns GameHUDOverlay + SettingsScreen at runtime. Zero scene wiring.
/// </summary>
public static class HUDBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (Object.FindFirstObjectByType<GameHUDOverlay>() != null) return;

        var go = new GameObject("[GameHUD]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameHUDOverlay>();
        go.AddComponent<SettingsScreen>();

#if UNITY_EDITOR
        Debug.Log("[HUDBootstrap] GameHUDOverlay + SettingsScreen spawned");
#endif
    }
}
