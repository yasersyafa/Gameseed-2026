using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraManager : MonoBehaviour
{
    public enum ShakeIntensity { Small, Medium, Large }

    [SerializeField] private CinemachineTargetGroup  targetGroup;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float targetRadius = 2f;

    [Header("Shake Profiles")]
    [SerializeField] private float smallShake  = 0.5f;
    [SerializeField] private float mediumShake = 1.5f;
    [SerializeField] private float largeShake  = 3.0f;

    private void OnEnable()
    {
        GameEvents.OnPlayerEliminated += HandlePlayerEliminated;
        GameEvents.OnPlayerRespawned  += HandlePlayerRespawned;
        GameEvents.OnPlayerHit        += HandlePlayerHit;
        GameEvents.OnBoomerangParried += HandleParry;
        GameEvents.OnBoomerangCaught  += HandleCatch;
        GameEvents.OnBoomerangWallBounce += HandleWallBounce;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerEliminated -= HandlePlayerEliminated;
        GameEvents.OnPlayerRespawned  -= HandlePlayerRespawned;
        GameEvents.OnPlayerHit        -= HandlePlayerHit;
        GameEvents.OnBoomerangParried -= HandleParry;
        GameEvents.OnBoomerangCaught  -= HandleCatch;
        GameEvents.OnBoomerangWallBounce -= HandleWallBounce;
    }

    private void HandlePlayerEliminated(int index, PlayerController controller)
    {
        Shake(ShakeIntensity.Large);
        RemoveTarget(controller.transform);
    }

    private void HandlePlayerRespawned(int index, PlayerController controller)
        => AddTargetIfNotExists(controller.transform);

    private void HandlePlayerHit(int index, PlayerController controller)
        => Shake(ShakeIntensity.Medium);

    private void HandleParry(int index)         => Shake(ShakeIntensity.Medium);
    private void HandleCatch(int index)         => Shake(ShakeIntensity.Small);
    private void HandleWallBounce()             => Shake(ShakeIntensity.Small);

    public void AddTarget(Transform target)
        => targetGroup.AddMember(target, targetWeight, targetRadius);

    public void RemoveTarget(Transform target)
        => targetGroup.RemoveMember(target);

    public void Shake(ShakeIntensity intensity)
    {
        if (impulseSource == null) return;
        if (SettingsManager.Instance != null && !SettingsManager.Instance.ShakeEnabled) return;
        float force = intensity switch
        {
            ShakeIntensity.Small  => smallShake,
            ShakeIntensity.Medium => mediumShake,
            ShakeIntensity.Large  => largeShake,
            _ => mediumShake,
        };
        impulseSource.GenerateImpulse(force);
    }

    public void ShakeCamera(float force = 1f)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(force);
    }

    public void AddTargetIfNotExists(Transform target)
    {
        for (int i = 0; i < targetGroup.Targets.Count; i++)
            if (targetGroup.Targets[i].Object == target) return;

        targetGroup.AddMember(target, targetWeight, targetRadius);
    }
}
