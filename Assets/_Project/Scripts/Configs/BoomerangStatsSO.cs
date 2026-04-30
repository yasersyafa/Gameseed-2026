using UnityEngine;

/// <summary>
/// Tuning data Boomerang. Override SerializeField default kalau di-assign.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/Boomerang Stats", fileName = "BoomerangStats_")]
public class BoomerangStatsSO : ScriptableObject
{
    [Header("Flight")]
    public float maxDistance         = 10f;
    public float returnSpeed         = 18f;
    public float catchRadius         = 1.0f;
    public int   maxBounces          = 3;

    [Header("Spin")]
    public float spinSpeed           = 720f;
    public float lateralCurveStrength = 3f;

    [Header("Spin Scaling (speed-based)")]
    public float spinSpeedAtMin      = 360f;
    public float spinSpeedAtMax      = 1080f;
    public float speedForMaxSpin     = 25f;
}
