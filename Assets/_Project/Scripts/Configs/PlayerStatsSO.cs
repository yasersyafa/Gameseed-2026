using UnityEngine;

/// <summary>
/// Tuning data PlayerController. Bila di-assign ke PlayerController,
/// nilai SO override SerializeField default.
/// </summary>
[CreateAssetMenu(menuName = "BoomerangFu/Configs/Player Stats", fileName = "PlayerStats_")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed     = 10f;
    public float rotationSpeed = 50f;

    [Header("Dash")]
    public float dashForce    = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Throw")]
    public float throwForce  = 20f;
    public float spawnOffset = 1.2f;

    [Header("Charge Throw")]
    public float chargeMinForce      = 15f;
    public float chargeMaxForce      = 40f;
    public float chargeMaxTime       = 1.0f;
    public float chargeMaxDistance   = 20f;
    public float chargeMinDistance   = 8f;

    [Header("Knockback")]
    public float hitKnockbackForce   = 12f;
    public float parryKnockbackForce = 6f;
}
