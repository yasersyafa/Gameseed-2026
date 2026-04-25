using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraManager : MonoBehaviour
{
    public static CinemachineCameraManager Instance { get; private set; }

    [SerializeField] private CinemachineTargetGroup  targetGroup;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float targetRadius = 2f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerEliminated += HandlePlayerEliminated;
        GameEvents.OnPlayerRespawned  += HandlePlayerRespawned;
        GameEvents.OnPlayerHit        += HandlePlayerHit;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerEliminated -= HandlePlayerEliminated;
        GameEvents.OnPlayerRespawned  -= HandlePlayerRespawned;
        GameEvents.OnPlayerHit        -= HandlePlayerHit;
    }

    private void HandlePlayerEliminated(int index, PlayerController controller)
        => RemoveTarget(controller.transform);

    private void HandlePlayerRespawned(int index, PlayerController controller)
        => AddTargetIfNotExists(controller.transform);

    private void HandlePlayerHit(int index, PlayerController controller)
        => ShakeCamera(1.5f);

    // ── Public API ────────────────────────────────────────────────────────────

    public void AddTarget(Transform target)
        => targetGroup.AddMember(target, targetWeight, targetRadius);

    public void RemoveTarget(Transform target)
        => targetGroup.RemoveMember(target);

    public void ShakeCamera(float force = 1f)
        => impulseSource.GenerateImpulse(force);

    public void AddTargetIfNotExists(Transform target)
    {
        for (int i = 0; i < targetGroup.Targets.Count; i++)
            if (targetGroup.Targets[i].Object == target) return;

        targetGroup.AddMember(target, targetWeight, targetRadius);
    }
}