using UnityEngine;

public class Boomerang : MonoBehaviour
{
    private enum BoomerangState { Flying, Returning }
    private BoomerangState _state = BoomerangState.Flying;

    private Transform _owner;
    private PlayerController _ownerController;
    private Collider _ownerCollider;   // ← tambah ini
    private Collider _myCollider;      // ← tambah ini
    private Vector3 _velocity;
    private float _speed;

    [Header("Flight")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float returnSpeed = 18f;
    [SerializeField] private float catchRadius = 1.0f;
    [SerializeField] private int   maxBounces  = 3;

    [Header("Spin")]
    [SerializeField] private float spinSpeed = 720f;

    [Header("Kill")]
    [SerializeField] private string playerTag = "Player";

    private Vector3 _spawnPosition;
    private int _bounceCount = 0;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _myCollider = GetComponent<Collider>();   // ← tambah ini
        _rb.useGravity = false;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Launch(Transform owner, Vector3 direction, float force)
    {
        _owner           = owner;
        _ownerController = owner.GetComponent<PlayerController>();
        _ownerCollider   = owner.GetComponent<Collider>();   // ← tambah ini
        _velocity        = direction.normalized * force;
        _speed           = force;
        _spawnPosition   = transform.position;
        _state           = BoomerangState.Flying;
        _bounceCount     = 0;

        // FIX: Matikan collision dengan owner sejak awal.
        // Ini mencegah boomerang langsung ke-catch karena overlap di frame pertama,
        // sekaligus memastikan owner tidak bisa terkena boomerangnya sendiri saat Flying.
        if (_ownerCollider != null && _myCollider != null)
            Physics.IgnoreCollision(_myCollider, _ownerCollider, true);
    }

    private void FixedUpdate()
    {
        SpinSelf();

        switch (_state)
        {
            case BoomerangState.Flying:    HandleFlying();    break;
            case BoomerangState.Returning: HandleReturning(); break;
        }
    }

    private void HandleFlying()
    {
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);

        float distFromSpawn = Vector3.Distance(_rb.position, _spawnPosition);
        if (distFromSpawn >= maxDistance)
            StartReturning();
    }

    private void StartReturning()
    {
        _state = BoomerangState.Returning;

        // FIX: Aktifkan kembali collision dengan owner saat boomerang mulai balik,
        // supaya catch via OnCollisionEnter tetap bekerja normal.
        if (_ownerCollider != null && _myCollider != null)
            Physics.IgnoreCollision(_myCollider, _ownerCollider, false);
    }

    private void HandleReturning()
    {
        if (_owner == null) { Destroy(gameObject); return; }

        Vector3 toOwner = _owner.position - _rb.position;
        float dist      = toOwner.magnitude;

        if (dist <= catchRadius)
        {
            _ownerController.CatchBoomerang();
            Destroy(gameObject);
            return;
        }

        float dynamicSpeed = Mathf.Lerp(returnSpeed * 0.7f, returnSpeed * 1.3f, 1f - Mathf.Clamp01(dist / maxDistance));
        _velocity = toOwner.normalized * dynamicSpeed;
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    private void SpinSelf()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Owner — saat Returning, collision sudah diaktifkan kembali → catch
        // Saat Flying, collision masih di-ignore → blok ini tidak akan terpanggil untuk owner
        if (collision.transform == _owner)
        {
            _ownerController.CatchBoomerang();
            Destroy(gameObject);
            return;
        }

        // Kill player lain
        if (collision.gameObject.CompareTag(playerTag))
        {
            collision.gameObject.GetComponent<PlayerController>()?.OnHitByBoomerang();
            Destroy(gameObject);
            return;
        }

        // Wall bounce
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (_bounceCount >= maxBounces)
            {
                StartReturning();
                return;
            }

            Vector3 normal = collision.contacts[0].normal;
            normal.y  = 0f;
            _velocity = Vector3.Reflect(_velocity, normal.normalized);
            _bounceCount++;
        }
    }
}