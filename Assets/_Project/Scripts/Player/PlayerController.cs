using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 15f;

    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        // Memastikan physics tidak mengganggu rotasi manual kita
        _rb.freezeRotation = true;
        _rb.useGravity = false; // Biasanya top-down tidak butuh gravity standar
    }

    /// <summary>
    /// Fungsi callback dari Player Input component (Message: OnMove)
    /// </summary>
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        ProcessInputs();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    /// <summary>
    /// Mengonversi input 2D (Vector2) menjadi arah gerak 3D (Vector3)
    /// </summary>
    private void ProcessInputs()
    {
        _moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
    }

    /// <summary>
    /// Menggerakkan Player menggunakan Rigidbody velocity agar responsif
    /// </summary>
    private void MovePlayer()
    {
        // Menggunakan velocity langsung memberikan feel 'snappy' seperti Boomerang Fu
        _rb.linearVelocity = _moveDirection * moveSpeed;
    }

    /// <summary>
    /// Memutar karakter menghadap arah jalan secara halus (lerp)
    /// </summary>
    private void RotatePlayer()
    {
        if (_moveDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }
}