using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 25f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Boomerang Settings")]
    [SerializeField] private GameObject boomerangPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 20f;

    private bool _hasBoomerang = true;

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;
    
    private bool _isDashing;
    private bool _canDash = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.useGravity = false;
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

    public void OnDash(InputValue value)
    {
        if (value.isPressed && _canDash && !_isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    public void OnThrow(InputValue value)
    {
        if (value.isPressed && _hasBoomerang && !_isDashing)
        {
            ThrowBoomerang();
        }
    }

    private void FixedUpdate()
    {
        if (_isDashing) return;

        MovePlayer();
        RotatePlayer();
    }

    private void MovePlayer()
    {
        _moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        _rb.linearVelocity = _moveDirection * moveSpeed;
    }

    private void RotatePlayer()
    {
        if (_moveDirection == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);

        // Pakai RotateTowards supaya lebih predictable — derajat/detik yang konsisten
        _rb.rotation = Quaternion.RotateTowards(
            _rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    /// <summary>
    /// Logika utama Dash menggunakan Coroutine untuk kontrol durasi
    /// </summary>
    private IEnumerator DashRoutine()
    {
        _canDash = false;
        _isDashing = true;

        Vector3 dashDir = _moveDirection != Vector3.zero ? _moveDirection : transform.forward;

        _rb.linearVelocity = dashDir * dashForce;

        yield return new WaitForSeconds(dashDuration);

        _isDashing = false;
        _rb.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    /// <summary>
    /// Spawns the boomerang and sets its initial direction
    /// </summary>
    private void ThrowBoomerang()
    {
        _hasBoomerang = false;
        GameObject boomObj = Instantiate(boomerangPrefab, throwPoint.position, Quaternion.identity);
        Boomerang boomScript = boomObj.GetComponent<Boomerang>();

        // Gunakan movement direction kalau ada input, fallback ke transform.forward
        Vector3 throwDir = _moveDirection != Vector3.zero ? _moveDirection : transform.forward;

        boomScript.Launch(this.transform, throwDir, throwForce);
    }

    /// <summary>
    /// Called by the Boomerang script when it returns to the player
    /// </summary>
    public void CatchBoomerang()
    {
        _hasBoomerang = true;
        // Add visual/audio feedback here
    }

    // Tambahkan ke PlayerController.cs
    public void OnHitByBoomerang()
    {
        // Untuk sekarang: langsung mati / disable
        Debug.Log($"{gameObject.name} terkena boomerang!");
        gameObject.SetActive(false);

        // Nanti di Phase 3 ini akan diganti dengan sistem lives
    }
}