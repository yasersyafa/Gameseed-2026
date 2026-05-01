using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Config (override SerializeField if assigned)")]
    [SerializeField] private PlayerStatsSO statsSO;

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
    [SerializeField] private MeleeController melee;

    [Header("Charge Throw")]
    [SerializeField] private float chargeMinForce    = 15f;
    [SerializeField] private float chargeMaxForce    = 40f;
    [SerializeField] private float chargeMaxTime     = 1.0f;
    [SerializeField] private float chargeMinDistance = 8f;
    [SerializeField] private float chargeMaxDistance = 20f;

    [Header("Knockback")]
    [SerializeField] private float hitKnockbackForce   = 12f;
    [SerializeField] private float parryKnockbackForce = 6f;

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
    public bool      IsCharging      { get; set; } = false;
    public Vector2   MoveInput       { get => _moveInput; set => _moveInput = value; }
    public Vector3   MoveDirection   { get; private set; }
    public Vector3   LastMoveDirection => _lastMoveDirection;
    public MeleeController Melee => melee;
    public int PlayerIndex => _playerIndex;
    public float MoveSpeed => moveSpeed;
    public Boomerang ActiveBoomerang { get; set; }
    #endregion

    // Expose settings to state (read-only)
    public float DashForce       => dashForce;
    public float DashDuration    => dashDuration;
    public float DashCooldown    => dashCooldown;
    public float ChargeMinForce  => chargeMinForce;
    public float ChargeMaxForce  => chargeMaxForce;
    public float ChargeMaxTime   => chargeMaxTime;
    public float ThrowForce      => throwForce;
    public float HitKnockback    => hitKnockbackForce;
    public float ParryKnockback  => parryKnockbackForce;

    private PlayerInput                          _playerInput;
    private UnityEngine.InputSystem.InputAction  _throwAction;
    private bool                                 _wasThrowPressedLastFrame;

    // Injected services (via VContainer InjectGameObject di GameManager.OnPlayerJoined)
    private LivesSystem      _lives;
    private HitEffectManager _hits;

    [Inject]
    public void Construct(LivesSystem lives, HitEffectManager hits)
    {
        _lives = lives;
        _hits  = hits;
    }

    #region Unity Lifecycle
    private void Awake()
    {
        ApplyStatsFromSO();

        Rb = GetComponent<Rigidbody>();
        Rb.freezeRotation = true;
        Rb.useGravity     = false;
        _lastMoveDirection = transform.forward;

        EnsureRuntimeComponents();
        ChangeState(new PlayerIdleState());

        // SendMessages mode tidak fire OnThrow di Canceled phase, dan
        // onActionTriggered juga unreliable di mode itu.
        // Solusi: poll InputAction langsung di Update untuk detect release.
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
            _throwAction = _playerInput.actions?.FindAction("Throw");
    }

    private void Update()
    {
        if (_throwAction == null) return;

        bool pressed = _throwAction.IsPressed();
        if (_wasThrowPressedLastFrame && !pressed)
        {
#if UNITY_EDITOR
            Debug.Log($"[Player {_playerIndex}] Throw RELEASE detected via polling");
#endif
            _currentState?.HandleThrowReleased(this);
        }
        _wasThrowPressedLastFrame = pressed;
    }

    private void EnsureRuntimeComponents()
    {
        if (GetComponent<StatusEffectController>() == null)
            gameObject.AddComponent<StatusEffectController>();
        if (GetComponent<HitFlash>() == null && visual != null)
            gameObject.AddComponent<HitFlash>();
        if (GetComponent<PlayerJuice>() == null && visual != null)
            gameObject.AddComponent<PlayerJuice>();
        if (GetComponent<PowerUpController>() == null)
            gameObject.AddComponent<PowerUpController>();
        if (GetComponent<PlayerSmokeEffect>() == null)
            gameObject.AddComponent<PlayerSmokeEffect>();
        if (GetComponent<DashTrail>() == null)
            gameObject.AddComponent<DashTrail>();
        if (GetComponent<DeathSplat>() == null)
            gameObject.AddComponent<DeathSplat>();
    }

    public PowerUpController PowerUps => GetComponent<PowerUpController>();

    public StatusEffectController Status => GetComponent<StatusEffectController>();

    private void ApplyStatsFromSO()
    {
        if (statsSO == null) return;
        moveSpeed     = statsSO.moveSpeed;
        rotationSpeed = statsSO.rotationSpeed;
        dashForce     = statsSO.dashForce;
        dashDuration  = statsSO.dashDuration;
        dashCooldown  = statsSO.dashCooldown;
        throwForce    = statsSO.throwForce;
        spawnOffset   = statsSO.spawnOffset;
        chargeMinForce    = statsSO.chargeMinForce;
        chargeMaxForce    = statsSO.chargeMaxForce;
        chargeMaxTime     = statsSO.chargeMaxTime;
        chargeMinDistance = statsSO.chargeMinDistance;
        chargeMaxDistance = statsSO.chargeMaxDistance;
        hitKnockbackForce   = statsSO.hitKnockbackForce;
        parryKnockbackForce = statsSO.parryKnockbackForce;
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
        // SendMessages mode hanya fire saat Performed (press).
        // Release ditangani onActionTriggered (Canceled phase).
        if (value.isPressed)
            _currentState?.HandleThrow(this);
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            _currentState?.HandleAttack(this);
        }
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
        ThrowBoomerangCharged(0f);
    }

    public void ThrowBoomerangCharged(float charge01)
    {
        HasBoomerang = false;

        float force    = charge01 > 0f ? Mathf.Lerp(chargeMinForce, chargeMaxForce, charge01) : throwForce;
        float distance = Mathf.Lerp(chargeMinDistance, chargeMaxDistance, charge01);

        Vector3 spawnPos  = transform.position + _lastMoveDirection.normalized * spawnOffset;
        GameObject boomObj = Instantiate(boomerangPrefab, spawnPos, Quaternion.identity);
        Boomerang boomScript = boomObj.GetComponent<Boomerang>();

        boomScript.Launch(this.transform, _lastMoveDirection, force);
        boomScript.SetTrailColor(GetComponentInChildren<Renderer>()?.material.color ?? Color.white);
        boomScript.SetThrowerIndex(_playerIndex);
        if (charge01 > 0f) boomScript.SetMaxDistance(distance);
        ActiveBoomerang = boomScript;

        // Power-ups bisa modify boomerang baru ini (Multi, Fire, Ice, Explosive)
        PowerUps?.OnBeforeThrow(boomScript);

#if UNITY_EDITOR
        Debug.Log($"[Player {_playerIndex}] Throw charge={charge01:F2} force={force:F1} dist={distance:F1}");
#endif

        GameEvents.RaiseBoomerangThrown(_playerIndex);
    }

    public void RequestRecall()
    {
        if (ActiveBoomerang != null)
        {
#if UNITY_EDITOR
            Debug.Log($"[Player {_playerIndex}] Recall requested");
#endif
            ActiveBoomerang.ForceRecall();
        }
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
        ActiveBoomerang = null;
#if UNITY_EDITOR
        Debug.Log($"[Player {_playerIndex}] Caught boomerang");
#endif
        GameEvents.RaiseBoomerangCaught(_playerIndex);
    }

    public void OnHitByBoomerang(int killerIndex = -1, Vector3 hitDirection = default)
    {
        if (!gameObject.activeSelf) return;

#if UNITY_EDITOR
        Debug.Log($"[Player {_playerIndex}] Hit by killer={killerIndex} dir={hitDirection}");
#endif

        // Power-up shield bisa absorb hit
        if (PowerUps != null && PowerUps.OnBeforeHit(killerIndex, hitDirection))
        {
            ApplyKnockback(hitDirection, parryKnockbackForce);
#if UNITY_EDITOR
            Debug.Log($"[Player {_playerIndex}] Hit absorbed by power-up");
#endif
            return;
        }

        ApplyKnockback(hitDirection, hitKnockbackForce);

        if (deathParticlePrefab != null)
        {
            GameObject particle = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 2f);
        }

        GameEvents.RaisePlayerHit(_playerIndex, this);

        // _hits?.TriggerKillEffect();   // sudah otomatis via OnPlayerHit subscriber
        _lives?.PlayerDied(_playerIndex, this);
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (Rb == null || force <= 0f || direction.sqrMagnitude < 0.001f) return;
        direction.y = 0f;
        Rb.AddForce(direction.normalized * force, ForceMode.Impulse);
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