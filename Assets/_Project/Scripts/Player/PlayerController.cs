using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed     = 10f;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce    = 25f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Boomerang Settings")]
    [SerializeField] private GameObject boomerangPrefab;
    [SerializeField] private float throwForce  = 20f;
    [SerializeField] private float spawnOffset = 1.2f;

    [Header("Renderer")]
    public Renderer visual;

    [Header("Effects")]
    [SerializeField] private GameObject deathParticlePrefab;

    /// <summary>
    /// State Machine of PlayerController
    /// </summary>
    private IPlayerState _currentState;

    #region Internal Data
    private int     _playerIndex;
    private Vector2 _moveInput;
    private Vector3 _lastMoveDirection;
    #endregion

    #region Properties
    public Rigidbody Rb              { get; private set; }
    public bool      HasBoomerang    { get; set; } = true;
    public bool      CanDash         { get; set; } = true;
    public Vector2   MoveInput       { get => _moveInput; set => _moveInput = value; }
    public Vector3   MoveDirection   { get; private set; }
    public Vector3   LastMoveDirection => _lastMoveDirection;
    #endregion

    // Expose settings to state (read-only)
    public float DashForce    => dashForce;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;

    #region Unity Lifecycle
    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Rb.freezeRotation = true;
        Rb.useGravity     = false;
        _lastMoveDirection = transform.forward;

        ChangeState(new PlayerIdleState());
    }

    private void FixedUpdate()
    {
        _currentState?.FixedUpdate(this);
    }
    #endregion

    #region Player Input Methods
    public void OnMove(InputValue value)
    {
        _currentState?.HandleMove(this, value.Get<Vector2>());
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed)
            _currentState?.HandleDash(this);
    }

    public void OnThrow(InputValue value)
    {
        if (value.isPressed)
            _currentState?.HandleThrow(this);
    }
    #endregion

    #region State Machine
    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;
        _currentState.Enter(this);
    }
    #endregion

    #region Movement Helpers
    public void ApplyMovement()
    {
        MoveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        if (MoveDirection != Vector3.zero)
            _lastMoveDirection = MoveDirection;

        Rb.linearVelocity = MoveDirection * moveSpeed;
    }

    public void ApplyRotation()
    {
        if (MoveDirection == Vector3.zero) return;

        Quaternion target = Quaternion.LookRotation(MoveDirection);
        Rb.rotation = Quaternion.RotateTowards(
            Rb.rotation, target, rotationSpeed * Time.fixedDeltaTime
        );
    }

    public void ThrowBoomerang()
    {
        HasBoomerang = false;

        Vector3 spawnPos  = transform.position + _lastMoveDirection.normalized * spawnOffset;
        GameObject boomObj = Instantiate(boomerangPrefab, spawnPos, Quaternion.identity);
        Boomerang boomScript = boomObj.GetComponent<Boomerang>();

        boomScript.Launch(this.transform, _lastMoveDirection, throwForce);
        boomScript.SetTrailColor(GetComponentInChildren<Renderer>()?.material.color ?? Color.white);
    }
    #endregion

    #region Public API
    public void Init(int index)
    {
        _playerIndex       = index;
        gameObject.name    = $"Player {index + 1}";
        _lastMoveDirection = transform.forward;
    }

    public void CatchBoomerang()
    {
        HasBoomerang = true;
    }

    public void OnHitByBoomerang()
    {
        if (!gameObject.activeSelf) return;

        if (deathParticlePrefab != null)
        {
            GameObject particle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 2f);
        }

        HitEffectManager.Instance?.TriggerKillEffect();
        LivesSystem.Instance?.PlayerDied(_playerIndex, this);
    }

    public void ResetState()
    {
        _lastMoveDirection = transform.forward;
        ChangeState(new PlayerIdleState());
        StartCoroutine(FlashRoutine());
    }

    /// <summary>Called RoundManager when countdown / round end.</summary>
    public void SetFreeze(bool freeze)
    {
        if (freeze)
            ChangeState(new PlayerFrozenState());
        else
            ChangeState(new PlayerIdleState());
    }

    /// <summary>Called LivesSystem when player die / respawn.</summary>
    public void SetEliminated(bool eliminated)
    {
        if (eliminated)
            ChangeState(new PlayerEliminatedState());
        else
            ChangeState(new PlayerIdleState());
    }

    public bool IsEliminated => _currentState is PlayerEliminatedState;
    #endregion

    #region Private Helpers
    private IEnumerator FlashRoutine()
    {
        if (visual == null) yield break;

        for (int i = 0; i < 10; i++)
        {
            visual.enabled = !visual.enabled;
            yield return YieldCollection.WaitForSeconds(0.05f);
        }
        visual.enabled = true;
    }
    #endregion
}