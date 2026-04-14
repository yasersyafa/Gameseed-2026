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
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
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

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }
}