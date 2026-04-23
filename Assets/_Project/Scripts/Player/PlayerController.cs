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
    [SerializeField] private Transform  throwPoint;
    [SerializeField] private float throwForce = 20f;
    [SerializeField] private float spawnOffset = 1.2f;

    [Header("Renderer")]
    public Renderer visual;

    [Header("Effects")]
    [SerializeField] private GameObject deathParticlePrefab;

    private bool _hasBoomerang = true;
    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;

    private Vector3 _lastMoveDirection;

    private bool _isDashing;
    private bool _canDash = true;
    private int  _playerIndex;
    private bool _isFrozen = false;
    private bool _isEliminated = false;

    #region Player Input Methods
    public void OnMove(InputValue value)
    {
        if (_isEliminated) return;
         _moveInput = value.Get<Vector2>();
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && _canDash && !_isDashing && !_isFrozen && !_isEliminated)
            StartCoroutine(DashRoutine());
    }

    public void OnThrow(InputValue value)
    {
        if (value.isPressed && _hasBoomerang && !_isDashing && !_isFrozen && !_isEliminated)
            ThrowBoomerang();
    }
#endregion

#region Unity Lifecycle
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.useGravity = false;

        _lastMoveDirection = transform.forward;
    }

    private void FixedUpdate()
    {
        if (_isDashing || _isFrozen) return;
        MovePlayer();
        RotatePlayer();
    }
#endregion

    private void MovePlayer()
    {
        _moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        if (_moveDirection != Vector3.zero)
            _lastMoveDirection = _moveDirection;

        _rb.linearVelocity = _moveDirection * moveSpeed;
    }

    private void RotatePlayer()
    {
        if (_moveDirection == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
        _rb.rotation = Quaternion.RotateTowards(
            _rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    private IEnumerator DashRoutine()
    {
        _canDash  = false;
        _isDashing = true;

        Vector3 dashDir = _moveDirection != Vector3.zero ? _moveDirection : _lastMoveDirection;
        _rb.linearVelocity = dashDir * dashForce;

        yield return YieldCollection.WaitForSeconds(dashDuration);
        _isDashing = false;
        _rb.linearVelocity = Vector3.zero;

        yield return YieldCollection.WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    private IEnumerator FlashRoutine()
    {
        if (visual == null) yield break;

        // Berkedip 5x selama 1.5 detik
        for (int i = 0; i < 10; i++)
        {
            visual.enabled = !visual.enabled;
            yield return YieldCollection.WaitForSeconds(0.05f);
        }
        visual.enabled = true;
    }

    private void ThrowBoomerang()
    {
        _hasBoomerang = false;

        // Selalu spawn di depan arah gerak, bukan di throwPoint yang posisinya fixed
        Vector3 spawnPos = transform.position + _lastMoveDirection.normalized * spawnOffset;
        
        GameObject boomObj    = Instantiate(boomerangPrefab, spawnPos, Quaternion.identity);
        Boomerang  boomScript = boomObj.GetComponent<Boomerang>();

        boomScript.Launch(this.transform, _lastMoveDirection, throwForce);
        boomScript.SetTrailColor(GetComponentInChildren<Renderer>()?.material.color ?? Color.white);
    }


#region Public API Methods
    public void CatchBoomerang()
    {
        _hasBoomerang = true;
    }

    /// <summary>
    /// run whenever player spawned
    /// </summary>
    /// <param name="index"></param>
    public void Init(int index)
    {
        _playerIndex       = index;
        gameObject.name    = $"Player {index + 1}";
        _lastMoveDirection = transform.forward;
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
        _hasBoomerang      = true;
        _isDashing         = false;
        _canDash           = true;
        _lastMoveDirection = transform.forward;
        _isFrozen          = false;
        _isEliminated      = false;
        _rb.linearVelocity = Vector3.zero;
        StartCoroutine(FlashRoutine());
    }

    public void SetFreeze(bool freeze)
    {
        _isFrozen = freeze;
        if (freeze) _rb.linearVelocity = Vector3.zero;
    }

    public void SetEliminated(bool eliminated)
    {
        _isEliminated = eliminated;

        // Kalau eliminated, tidak bisa input apapun
        if (eliminated)
        {
            _isDashing         = false;
            _canDash           = false;
            _hasBoomerang      = false;
            _rb.linearVelocity = Vector3.zero;
        }
    }
#endregion
}