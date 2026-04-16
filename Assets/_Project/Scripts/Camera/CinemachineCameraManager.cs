using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraManager : MonoBehaviour
{
    public static CinemachineCameraManager Instance { get; private set; }

    [SerializeField] private CinemachineTargetGroup targetGroup;

    // Tuning bobot dan radius tiap target
    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float targetRadius = 2f;

    private void Awake()
    {
        Instance = this;
    }

    public void AddTarget(Transform target)
    {
        // Cinemachine 3.x pakai CinemachineTargetGroup.Target struct
        targetGroup.AddMember(target, targetWeight, targetRadius);
    }

    public void RemoveTarget(Transform target)
    {
        targetGroup.RemoveMember(target);
    }
}