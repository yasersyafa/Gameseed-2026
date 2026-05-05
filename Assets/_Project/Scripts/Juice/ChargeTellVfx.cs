using UnityEngine;

/// <summary>
/// Telegraph windup ring di kaki player saat charge throw. Spawn pooled sphere
/// flat di ground, scale up linear sampai ChargeMaxTime, lalu pop+despawn di
/// release. Subscribe per-player ke OnChargeStarted / OnChargeReleased.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class ChargeTellVfx : MonoBehaviour
{
    [SerializeField] private float minScale       = 0.4f;
    [SerializeField] private float maxScale       = 2.4f;
    [SerializeField] private float popScale       = 3.2f;
    [SerializeField] private float popLifetime    = 0.18f;
    [SerializeField] private float ringHeight     = 0.05f;
    [SerializeField] private float alpha          = 0.55f;

    private static Material _sharedMat;
    private static MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private PlayerController _player;
    private GameObject       _ring;
    private float            _chargeStartTime;
    private bool             _active;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        GameEvents.OnChargeStarted  += HandleChargeStarted;
        GameEvents.OnChargeReleased += HandleChargeReleased;
    }

    private void OnDisable()
    {
        GameEvents.OnChargeStarted  -= HandleChargeStarted;
        GameEvents.OnChargeReleased -= HandleChargeReleased;
        ReleaseRing();
    }

    private bool IsMine(int idx) => _player != null && _player.PlayerIndex == idx;

    private void HandleChargeStarted(int idx)
    {
        if (!IsMine(idx)) return;
        SpawnRing();
        _chargeStartTime = Time.time;
        _active = true;
    }

    private void HandleChargeReleased(int idx, float charge01)
    {
        if (!IsMine(idx)) return;
        _active = false;
        if (_ring == null) return;

        // Pop: enlarge briefly + return to pool
        float scale = Mathf.Lerp(minScale, popScale, charge01);
        _ring.transform.localScale = new Vector3(scale, ringHeight, scale);
        PrimitivePool.ReleaseAfter(_ring, popLifetime);
        _ring = null;
    }

    private void Update()
    {
        if (!_active || _ring == null || _player == null) return;

        float t = (Time.time - _chargeStartTime) / Mathf.Max(0.01f, _player.ChargeMaxTime);
        t = Mathf.Clamp01(t);
        float scale = Mathf.Lerp(minScale, maxScale, t);

        Vector3 pos = _player.transform.position;
        pos.y = ringHeight * 0.5f;
        _ring.transform.position   = pos;
        _ring.transform.localScale = new Vector3(scale, ringHeight, scale);
    }

    private void SpawnRing()
    {
        if (_ring != null) ReleaseRing();
        _ring = PrimitivePool.AcquireSphere();

        Color c = (_player.visual != null && _player.visual.sharedMaterial != null)
            ? _player.visual.sharedMaterial.color
            : Color.white;
        c.a = alpha;

        if (_sharedMat == null) _sharedMat = new Material(ShaderHelper.GetUnlit());
        if (_mpb == null)       _mpb       = new MaterialPropertyBlock();

        var rend = _ring.GetComponent<Renderer>();
        rend.sharedMaterial = _sharedMat;
        _mpb.Clear();
        _mpb.SetColor(BaseColorId, c);
        _mpb.SetColor(ColorId,     c);
        rend.SetPropertyBlock(_mpb);

        if (_ring.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        Vector3 pos = _player.transform.position;
        pos.y = ringHeight * 0.5f;
        _ring.transform.position   = pos;
        _ring.transform.localScale = new Vector3(minScale, ringHeight, minScale);
    }

    private void ReleaseRing()
    {
        if (_ring == null) return;
        PrimitivePool.Release(_ring);
        _ring = null;
    }
}
