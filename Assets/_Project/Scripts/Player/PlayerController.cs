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
    [SerializeField] private float      throwForce = 20f;

    private bool _hasBoomerang = true;
    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;

    // FIX BUG 2: Simpan arah terakhir yang valid
    // Di-update hanya saat ada input aktif, sehingga saat player diam
    // dan melempar, tetap menggunakan arah terakhir — bukan Vector3.zero
    private Vector3 _lastMoveDirection;

    private bool _isDashing;
    private bool _canDash = true;
    private int  _playerIndex;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.useGravity = false;

        // FIX BUG 2: Default arah forward supaya throw pertama tidak salah
        _lastMoveDirection = transform.forward;
    }

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

    private void FixedUpdate()
    {
        if (_isDashing) return;
        MovePlayer();
        RotatePlayer();
    }

    public void Init(int index)
    {
        _playerIndex       = index;
        gameObject.name    = $"Player {index + 1}";
        // FIX BUG 2: Sync default arah dengan transform setelah di-spawn
        _lastMoveDirection = transform.forward;
    }

    private void MovePlayer()
    {
        _moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        // FIX BUG 2: Update _lastMoveDirection hanya kalau ada input aktif
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

        GameObject boomObj   = Instantiate(boomerangPrefab, throwPoint.position, Quaternion.identity);
        Boomerang  boomScript = boomObj.GetComponent<Boomerang>();

        // FIX BUG 2: Selalu pakai _lastMoveDirection yang dijamin tidak pernah zero
        boomScript.Launch(this.transform, _lastMoveDirection, throwForce);
    }

    public void CatchBoomerang()
    {
        _hasBoomerang = true;
    }

    public void OnHitByBoomerang()
    {
        if (!gameObject.activeSelf) return;
        LivesSystem.Instance?.PlayerDied(_playerIndex, this);
    }

    public void ResetState()
    {
        _hasBoomerang      = true;
        _isDashing         = false;
        _canDash           = true;
        _lastMoveDirection = transform.forward; // FIX BUG 2: Reset arah juga
        _rb.linearVelocity = Vector3.zero;
    }
}