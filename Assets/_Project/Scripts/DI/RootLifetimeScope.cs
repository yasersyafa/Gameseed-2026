using VContainer;
using VContainer.Unity;

/// <summary>
/// Scene root LifetimeScope. Tanpa registration di sini, semua [Inject] di
/// PlayerController/GameManager/dll akan null → bug seperti target group kosong.
/// </summary>
public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<GameManager>();
        builder.RegisterComponentInHierarchy<RoundManager>();
        builder.RegisterComponentInHierarchy<LivesSystem>();
        builder.RegisterComponentInHierarchy<HitEffectManager>();
        builder.RegisterComponentInHierarchy<AudioManager>();
        builder.RegisterComponentInHierarchy<CinemachineCameraManager>();
    }
}
