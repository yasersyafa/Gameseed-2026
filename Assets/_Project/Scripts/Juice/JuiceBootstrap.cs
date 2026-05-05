using UnityEngine;

/// <summary>
/// Spawn global juice listeners (PostFxJuice, HitImpactVfx, WinFlourish) di
/// runtime. Mirror style HUDBootstrap — DontDestroyOnLoad, no scene wiring.
/// </summary>
public static class JuiceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (Object.FindFirstObjectByType<PostFxJuice>() != null) return;

        var go = new GameObject("[Juice]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<PostFxJuice>();
        go.AddComponent<HitImpactVfx>();
        go.AddComponent<WinFlourish>();

#if UNITY_EDITOR
        Debug.Log("[JuiceBootstrap] PostFxJuice + HitImpactVfx + WinFlourish spawned");
#endif
    }
}
