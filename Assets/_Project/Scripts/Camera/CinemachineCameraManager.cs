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

    public void AddTarget(Transform target)
    {
        targetGroup.AddMember(target, targetWeight, targetRadius);
    }

    public void RemoveTarget(Transform target)
    {
        targetGroup.RemoveMember(target);
    }

    // force kecil = 0.5f (boomerang catch), besar = 1.5f (player mati)
    public void ShakeCamera(float force = 1f)
    {
        impulseSource.GenerateImpulse(force);
    }

    public void AddTargetIfNotExists(Transform target)
    {
        // Cek dulu apakah sudah ada di group
        for (int i = 0; i < targetGroup.Targets.Count; i++)
        {
            if (targetGroup.Targets[i].Object == target) return;
        }
        targetGroup.AddMember(target, targetWeight, targetRadius);
    }
}