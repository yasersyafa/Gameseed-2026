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

    private bool _hasBoomerang = true;
    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;

    private Vector3 _lastMoveDirection;

    private bool _isDashing;
    private bool _canDash = true;
    private int  _playerIndex;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.useGravity = false;

        _lastMoveDirection = transform.forward;
    }

#region Player Input Methods
    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

    public void OnDash(InputValue value)
    {
        if (value.isPressed && _canDash && !_isDashing)
            StartCoroutine(DashRoutine());
    }

    public void OnThrow(InputValue value)
    {
        if (value.isPressed && _hasBoomerang && !_isDashing)
            ThrowBoomerang();
    }
#endregion

    private void FixedUpdate()
    {
        if (_isDashing) return;
        MovePlayer();
        RotatePlayer();
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

        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
        _rb.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
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

    public void CatchBoomerang()
    {
        _hasBoomerang = true;
    }

    public void OnHitByBoomerang()
    {
        if (!gameObject.activeSelf) return;
        CinemachineCameraManager.Instance?.ShakeCamera(1.5f);
        LivesSystem.Instance?.PlayerDied(_playerIndex, this);
    }

    public void ResetState()
    {
        _hasBoomerang      = true;
        _isDashing         = false;
        _canDash           = true;
        _lastMoveDirection = transform.forward;
        _rb.linearVelocity = Vector3.zero;
    }
}