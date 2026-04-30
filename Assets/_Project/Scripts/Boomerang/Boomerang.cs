using UnityEngine;

public class Boomerang : MonoBehaviour
{
    public enum BoomerangState { Flying, Returning }
    private BoomerangState _state = BoomerangState.Flying;

    private Transform _owner;
    private PlayerController _ownerController;
    private Vector3 _velocity;
    private float _speed;
    private bool _hasHit = false;

    [Header("Config (override SerializeField if assigned)")]
    [SerializeField] private BoomerangStatsSO statsSO;

    [Header("Flight")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float returnSpeed = 18f;
    [SerializeField] private float catchRadius = 1.0f;
    [SerializeField] private int   maxBounces  = 3;

    [Header("Spin")]
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float lateralCurveStrength = 3f;

    [Header("Kill")]
    [SerializeField] private string playerTag = "Player";

    [Header("Visual Effect")]
    [SerializeField] private TrailRenderer trail;

    private Vector3 _spawnPosition;
    private int     _bounceCount   = 0;
    private int     _throwerIndex  = -1;
    private float   _maxDistanceOverride = -1f;

    private Rigidbody _rb;

    public int ThrowerIndex => _throwerIndex;
    public BoomerangState State => _state;
    public Transform Owner => _owner;
    public float CurrentSpeed => _velocity.magnitude;

    private void Awake()
    {
        ApplyStatsFromSO();

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity      = false;
        _rb.linearDamping   = 0f;
        _rb.angularDamping  = 0f;
        _rb.interpolation   = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    private void ApplyStatsFromSO()
    {
        if (statsSO == null) return;
        maxDistance          = statsSO.maxDistance;
        returnSpeed          = statsSO.returnSpeed;
        catchRadius          = statsSO.catchRadius;
        maxBounces           = statsSO.maxBounces;
        spinSpeed            = statsSO.spinSpeed;
        lateralCurveStrength = statsSO.lateralCurveStrength;
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
        _hasHit          = false;
#if UNITY_EDITOR
        Debug.Log($"[Boomerang] Launch owner={owner.name} dir={direction} force={force:F1}");
#endif
    }

    public void SetThrowerIndex(int index)
    {
        _throwerIndex = index;
    }

    public void SetMaxDistance(float distance)
    {
        _maxDistanceOverride = distance;
    }

    /// <summary>Manual recall — paksa balik ke owner walau belum max distance.</summary>
    public void ForceRecall()
    {
        if (_state == BoomerangState.Returning) return;
#if UNITY_EDITOR
        Debug.Log($"[Boomerang] ForceRecall thrower={_throwerIndex}");
#endif
        StartReturning();
    }

    /// <summary>Reflect velocity (untuk parry).</summary>
    public void Reflect(Vector3 newDirection, float speedScale = 1f, Transform newOwner = null)
    {
        _velocity      = newDirection.normalized * (_speed * speedScale);
        _spawnPosition = _rb.position;
        _state         = BoomerangState.Flying;
        _bounceCount   = 0;
        _hasHit        = false;

        if (newOwner != null)
        {
            _owner           = newOwner;
            _ownerController = newOwner.GetComponent<PlayerController>();
        }
#if UNITY_EDITOR
        Debug.Log($"[Boomerang] Reflect newOwner={(newOwner != null ? newOwner.name : "same")} scale={speedScale:F2}");
#endif
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

        float effectiveMax = _maxDistanceOverride > 0f ? _maxDistanceOverride : maxDistance;
        if (Vector3.Distance(_rb.position, _spawnPosition) >= effectiveMax)
            StartReturning();
    }

    private void StartReturning()
    {
        _state = BoomerangState.Returning;

        Vector3 lateral = Vector3.Cross(
            (_owner.position - _rb.position).normalized,
            Vector3.up
        ).normalized;

        _velocity += lateral * lateralCurveStrength;
    }

    private void HandleReturning()
    {
        if (_owner == null) { Destroy(gameObject); return; }

        Vector3 toOwner = _owner.position - _rb.position;
        float   dist    = toOwner.magnitude;

        if (dist <= catchRadius)
        {
            _ownerController.CatchBoomerang();
            Destroy(gameObject);
            return;
        }

        Vector3 desiredVelocity = toOwner.normalized * returnSpeed;
        float steerStrength     = Mathf.Lerp(1f, 8f, 1f - Mathf.Clamp01(dist / maxDistance));

        _velocity = Vector3.MoveTowards(
            _velocity,
            desiredVelocity,
            steerStrength * returnSpeed * Time.fixedDeltaTime
        );

        _velocity = _velocity.normalized * returnSpeed;

        // float dynamicSpeed = Mathf.Lerp(
        //     returnSpeed * 0.7f,
        //     returnSpeed * 1.3f,
        //     1f - Mathf.Clamp01(dist / maxDistance)
        // );
        // _velocity = toOwner.normalized * dynamicSpeed;

        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    public void SetTrailColor(Color color)
    {
        if (trail == null) return;

        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new(color, 0f), new(color, 1f) },
            new GradientAlphaKey[]  { new(1f, 0f),    new(0f, 1f)   }
        );
        trail.colorGradient = gradient;
    }

    private void SpinSelf()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;

        if (collision.transform == _owner)
        {
            _ownerController.CatchBoomerang();
            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.CompareTag(playerTag))
        {
            _hasHit = true;
            collision.gameObject.GetComponent<PlayerController>()?.OnHitByBoomerang(_throwerIndex, _velocity.normalized);
            Destroy(gameObject);
            if (_ownerController != null) _ownerController.CatchBoomerang();
            return;
        }

        // Wall bounce
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (_bounceCount >= maxBounces)
            {
#if UNITY_EDITOR
                Debug.Log($"[Boomerang] Max bounces reached ({maxBounces}) — recalling");
#endif
                StartReturning();
                return;
            }

            Vector3 normal = collision.contacts[0].normal;
            normal.y  = 0f;
            _velocity = Vector3.Reflect(_velocity, normal.normalized);
            _bounceCount++;
#if UNITY_EDITOR
            Debug.Log($"[Boomerang] Wall bounce #{_bounceCount}");
#endif
            GameEvents.RaiseBoomerangWallBounce();
        }
    }
}