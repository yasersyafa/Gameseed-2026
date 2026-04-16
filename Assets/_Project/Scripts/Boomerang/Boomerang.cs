using UnityEngine;

public class Boomerang : MonoBehaviour
{
    // ─── State ───────────────────────────────────────────────────────────────
    private enum BoomerangState { Flying, Returning }
    private BoomerangState _state = BoomerangState.Flying;

    // ─── Runtime refs ────────────────────────────────────────────────────────
    private Transform _owner;
    private PlayerController _ownerController;
    private Vector3 _velocity;          // arah + kecepatan saat ini
    private float _speed;               // initial throw force dari PlayerController

    // ─── Tuning (bisa override dari inspector kalau mau eksperimen) ──────────
    [Header("Flight")]
    [SerializeField] private float maxDistance   = 10f;   // jarak sebelum balik
    [SerializeField] private float returnSpeed   = 18f;   // kecepatan saat kembali
    [SerializeField] private float catchRadius   = 1.0f;  // jarak "ditangkap"
    [SerializeField] private int   maxBounces    = 3;     // max mantul dari wall

    [Header("Spin")]
    [SerializeField] private float spinSpeed     = 720f;  // derajat/detik

    [Header("Kill")]
    [SerializeField] private string playerTag    = "Player";

    // ─── Internal tracking ───────────────────────────────────────────────────
    private Vector3 _spawnPosition;
    private int _bounceCount = 0;
    private bool _canKillOwner = false; // owner baru bisa terkena setelah 1 bounce

    // ─────────────────────────────────────────────────────────────────────────
    // Dipanggil oleh PlayerController.ThrowBoomerang()
    // ─────────────────────────────────────────────────────────────────────────

    // Tambah field ini
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 0f;       // drag = 0, tidak ada perlambatan
        _rb.angularDamping = 0f;
        _rb.interpolation = RigidbodyInterpolation.Interpolate; // fix blink
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // fix miss collision
        _rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Launch(Transform owner, Vector3 direction, float force)
    {
        _owner           = owner;
        _ownerController = owner.GetComponent<PlayerController>();
        _velocity        = direction.normalized * force;
        _speed           = force;
        _spawnPosition   = transform.position;
        _state           = BoomerangState.Flying;
        _bounceCount     = 0;
        _canKillOwner    = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        SpinSelf();

        switch (_state)
        {
            case BoomerangState.Flying:    HandleFlying();    break;
            case BoomerangState.Returning: HandleReturning(); break;
        }
    }

    // ─── Flying ──────────────────────────────────────────────────────────────
    private void HandleFlying()
    {
        // Pakai MovePosition bukan transform.position +=
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);

        float distFromSpawn = Vector3.Distance(_rb.position, _spawnPosition);
        if (distFromSpawn >= maxDistance)
        {
            _state = BoomerangState.Returning;
            _canKillOwner = true;
        }
    }

    private void HandleReturning()
    {
        if (_owner == null) { Destroy(gameObject); return; }

        Vector3 toOwner = (_owner.position - _rb.position);
        float dist      = toOwner.magnitude;

        if (dist <= catchRadius)
        {
            _ownerController.CatchBoomerang();
            Destroy(gameObject);
            return;
        }

        float dynamicSpeed = Mathf.Lerp(returnSpeed * 0.7f, returnSpeed * 1.3f,
                                        1f - Mathf.Clamp01(dist / maxDistance));
        _velocity          = toOwner.normalized * dynamicSpeed;
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    // ─── Spin visual ─────────────────────────────────────────────────────────
    private void SpinSelf()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COLLISION — pakai OnCollisionEnter (bukan trigger) supaya dapat normal
    // ─────────────────────────────────────────────────────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        // ── Cek owner dulu — selalu prioritas catch, bukan kill ──────────────
        if (collision.transform == _owner)
        {
            // Owner hanya bisa terkena kalau sudah returning DAN canKillOwner
            // tapi kalau dia yang nyamperin (collision), treat sebagai catch
            if (_state == BoomerangState.Returning || !_canKillOwner)
            {
                _ownerController.CatchBoomerang();
                Destroy(gameObject);
                return;
            }
            // Edge case: owner kena boomerang saat masih Flying setelah bounce
            // (misal owner lari ke arah boomerang setelah mantul) → kill
            if (_canKillOwner)
            {
                _ownerController.OnHitByBoomerang();
                Destroy(gameObject);
            }
            return;
        }

        // ── Kill player lain ─────────────────────────────────────────────────
        if (collision.gameObject.CompareTag(playerTag))
        {
            collision.gameObject.GetComponent<PlayerController>()?.OnHitByBoomerang();
            Destroy(gameObject);
            return;
        }

        // ── Wall bounce ───────────────────────────────────────────────────────
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (_bounceCount >= maxBounces)
            {
                _state = BoomerangState.Returning;
                return;
            }

            Vector3 normal = collision.contacts[0].normal;
            normal.y       = 0f;
            _velocity      = Vector3.Reflect(_velocity, normal.normalized);
            _bounceCount++;
            _canKillOwner  = true;
        }
    }
}