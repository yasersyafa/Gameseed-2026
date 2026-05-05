using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Scene root LifetimeScope. Tanpa registration di sini, semua [Inject] di
/// PlayerController/GameManager/dll akan null → bug seperti target group kosong.
/// Also wires GameEvents.ClearAll on scene unload to prevent stale subscribers.
/// </summary>
public class RootLifetimeScope : LifetimeScope
{
    private static bool _sceneUnloadHooked;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<GameManager>();
        builder.RegisterComponentInHierarchy<RoundManager>();
        builder.RegisterComponentInHierarchy<LivesSystem>();
        builder.RegisterComponentInHierarchy<HitEffectManager>();
        builder.RegisterComponentInHierarchy<AudioManager>();
        builder.RegisterComponentInHierarchy<CinemachineCameraManager>();
        builder.RegisterComponentInHierarchy<SettingsManager>();
        builder.RegisterComponentInHierarchy<RumbleManager>();
        builder.RegisterComponentInHierarchy<GameHUDView>();

        if (!_sceneUnloadHooked)
        {
            SceneManager.sceneUnloaded += _ => GameEvents.ClearAll();
            _sceneUnloadHooked = true;
        }
    }
}
